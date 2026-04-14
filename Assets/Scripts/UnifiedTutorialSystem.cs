using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Unified tutorial system that handles both welcome sequence and mode-specific help
/// Replaces both WelcomeTutorial and ContextualTutorialPanel
/// </summary>
public class UnifiedTutorialSystem : MonoBehaviour
{
    [Header("References")]
    public TextMeshPro tutorialText;
    public GameObject tutorialPanel;
    public Transform panelTransform;
    public OVRGraphController graphController; // Reference to get current mode

    [Header("Settings")]
    public bool showWelcomeOnStart = true;
    public bool showModeHelp = true;
    public float stepDuration = 6f;
    public float autoHideDelay = 10f;
    public Vector3 panelOffset = new Vector3(0, 0f, 0.5f); // Centered at eye level, 0.5m in front
    public bool followView = true;
    public float followSpeed = 3f;

    [Header("Welcome Tutorial Settings")]
    public int actionsBeforeAutoHide = 3; // Hide mode help after N actions

    // State
    private bool hasShownWelcome = false;
    private bool isShowingTutorial = false;
    private TutorialMode currentTutorialMode = TutorialMode.None;

    // Welcome sequence
    private int currentWelcomeStep = 0;
    private bool triggerHeldLastFrame = false;

    // Mode help tracking
    private OVRGraphController.Mode currentMode;
    private Dictionary<OVRGraphController.Mode, bool> hasShownForMode = new Dictionary<OVRGraphController.Mode, bool>();
    private Dictionary<OVRGraphController.Mode, int> actionCountPerMode = new Dictionary<OVRGraphController.Mode, int>();
    private float showTimer = 0f;

    private enum TutorialMode
    {
        None,
        WelcomeSequence,
        ModeHelp
    }

    // Welcome tutorial steps
    private static readonly string[] welcomeSteps = new string[]
    {
        "<b>Welcome to Interactive Structures XR!</b>\n\nThis app lets you build and analyze 3D truss structures in VR.\n\nPress TRIGGER to continue...",

        "<b>Basic Controls</b>\n\n<color=#88FF88>Right Thumbstick Left/Right:</color> Switch modes\n<color=#88FF88>Right Trigger:</color> Primary action\n<color=#88FF88>A Button:</color> Detect table surface\n\nPress TRIGGER to continue...",

        "<b>Building Structures</b>\n\n1. <color=#88FF88>AddNode:</color> Place structural joints (nodes)\n2. <color=#88FF88>AddEdge:</color> Connect nodes with members\n3. <color=#88FF88>AddLoad:</color> Apply forces to nodes\n4. <color=#88FF88>ToggleSupport:</color> Fix nodes in place\n\nPress TRIGGER to continue...",

        "<b>Editing Structures</b>\n\n5. <color=#AA88FF>Move:</color> Drag individual elements\n6. <color=#FF8888>Delete:</color> Remove elements\n7. <color=#88DDFF>Grab:</color> Move entire structures\n8. <color=#FFFFFF>Analyze:</color> Run structural analysis\n\nPress TRIGGER to continue...",

        "<b>Analysis Mode</b>\n\nAfter building, use <color=#FFFFFF>Analyze</color> mode to:\n• Calculate forces in each member\n• Visualize displacements (exaggerated)\n• See support reactions\n\n<color=#FF8888>Red = Tension</color> | <color=#8888FF>Blue = Compression</color>\n\nPress TRIGGER to continue...",

        "<b>Grid System</b>\n\nUse <color=#AAAAAA>Grid</color> mode to:\n• Adjust grid spacing and size\n• Align grid to surfaces\n• Toggle grid visibility (Left stick press)\n\nNodes snap to grid for precision!\n\nPress TRIGGER to start building!"
    };

    void Start()
    {
        // Initialize mode tracking
        foreach (OVRGraphController.Mode mode in System.Enum.GetValues(typeof(OVRGraphController.Mode)))
        {
            hasShownForMode[mode] = false;
            actionCountPerMode[mode] = 0;
        }

        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(false);
        }
        else
        {
            Debug.LogError("[UnifiedTutorial] Tutorial panel is NOT assigned!");
        }

        // Show welcome sequence on start
        if (showWelcomeOnStart && !hasShownWelcome)
        {
            StartCoroutine(ShowWelcomeSequence());
        }
    }

    void Update()
    {
        // Toggle tutorial with X button (left controller)
        //if (OVRInput.GetDown(OVRInput.Button.Three, OVRInput.Controller.LTouch))
        if (OVRInput.GetDown(OVRInput.Button.Three))
        {
            Debug.Log("[UnifiedTutorial] Toggling tutorial panel");
            ToggleTutorial();
        }

        // Update panel position to follow view
        if (isShowingTutorial && followView && panelTransform != null && Camera.main != null)
        {
            UpdatePanelPosition();
        }

        // Auto-hide timer for mode help
        if (isShowingTutorial && currentTutorialMode == TutorialMode.ModeHelp)
        {
            showTimer += Time.deltaTime;
            if (showTimer > autoHideDelay)
            {
                HidePanel();
            }
        }

        // Handle welcome sequence trigger input
        if (currentTutorialMode == TutorialMode.WelcomeSequence)
        {
            HandleWelcomeInput();
        }
    }

    #region Welcome Sequence

    IEnumerator ShowWelcomeSequence()
    {
        hasShownWelcome = true;
        isShowingTutorial = true;
        currentTutorialMode = TutorialMode.WelcomeSequence;
        currentWelcomeStep = 0;

        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(true);
        }

        // Show each step
        while (currentWelcomeStep < welcomeSteps.Length)
        {
            ShowText(welcomeSteps[currentWelcomeStep]);

            // Wait for trigger press or timeout
            float timer = 0f;
            bool triggered = false;

            while (timer < stepDuration * 3f) // Max timeout
            {
                if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
                {
                    triggered = true;
                    HapticFeedback.Trigger(HapticFeedback.HapticType.Light);
                    break;
                }
                timer += Time.deltaTime;
                yield return null;
            }

            currentWelcomeStep++;
            yield return new WaitForSeconds(0.3f);
        }

        // Welcome complete
        HidePanel();
        HapticFeedback.Trigger(HapticFeedback.HapticType.Success);
    }

    void HandleWelcomeInput()
    {
        bool triggerPressed = OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch);
        triggerHeldLastFrame = triggerPressed;
    }

    public void ReplayWelcome()
    {
        hasShownWelcome = false;
        StartCoroutine(ShowWelcomeSequence());
    }

    #endregion

    #region Mode Help

    public void ShowForMode(OVRGraphController.Mode mode)
    {
        if (!showModeHelp) return;

        currentMode = mode;

        // If tutorial is already visible, just update the text for the new mode
        if (isShowingTutorial && currentTutorialMode == TutorialMode.ModeHelp)
        {
            ShowText(GetModeHelpText(mode));
            showTimer = 0f; // Reset timer
            return;
        }

        // Don't show if already shown for this mode (and not currently visible)
        if (hasShownForMode.ContainsKey(mode) && hasShownForMode[mode])
        {
            return;
        }

        // Show mode help
        isShowingTutorial = true;
        currentTutorialMode = TutorialMode.ModeHelp;
        showTimer = 0f;

        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(true);
        }

        ShowText(GetModeHelpText(mode));
        hasShownForMode[mode] = true;
    }

    public void OnActionPerformed(OVRGraphController.Mode mode)
    {
        if (!actionCountPerMode.ContainsKey(mode))
        {
            actionCountPerMode[mode] = 0;
        }

        actionCountPerMode[mode]++;
        showTimer = 0f; // Reset timer

        // Auto-hide after enough actions
        if (actionCountPerMode[mode] >= actionsBeforeAutoHide && mode == currentMode)
        {
            HidePanel();
        }
    }

    string GetModeHelpText(OVRGraphController.Mode mode)
    {
        switch (mode)
        {
            case OVRGraphController.Mode.Setup:
                return @"<b><color=#FFEE44>SETUP</color></b>

Right stick ↕ = Switch tool
<color=#FFFF88>TRIGGER</color> = Run selected tool

Tools: World · Grid · Network · Import
Left stick press = Toggle grid snap

<size=80%><i>Press X to hide</i></size>";

            case OVRGraphController.Mode.Build:
                return @"<b><color=#88FF88>BUILD</color></b>

Right stick ↕ = Switch tool
<color=#FFFF88>TRIGGER</color> = Place / connect
<color=#FFFF88>GRIP</color>    = Drag node / edge / structure
<color=#FFFF88>B</color>       = Cancel or clear selection

Tools: Node · Edge · Load · Support · Delete · Select

<size=80%><i>Press X to hide</i></size>";

            case OVRGraphController.Mode.Optimize:
                return @"<b><color=#FFD700>OPTIMIZE</color></b>

<color=#FFFF88>GRIP</color>         = Drag nodes (follow arrows!)
Left stick ↕    = Browse history
<color=#FFFF88>TRIGGER</color>      = Rollback to cursor position
<color=#FFFF88>B</color>           = Jump to latest snapshot
Right stick ↕   = Adjust displacement scale

Arrows show direction to improve compliance

<size=80%><i>Press X to hide</i></size>";

            default:
                return "Tutorial";
        }
    }

    #endregion

    #region Panel Control

    void ShowText(string text)
    {
        if (tutorialText != null)
        {
            tutorialText.text = text;
        }
    }

    void HidePanel()
    {
        isShowingTutorial = false;
        currentTutorialMode = TutorialMode.None;

        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(false);
        }
    }

    void ToggleTutorial()
    {
        if (isShowingTutorial)
        {
            HidePanel();
        }
        else
        {
            // Show appropriate tutorial
            if (currentTutorialMode == TutorialMode.WelcomeSequence || !hasShownWelcome)
            {
                ReplayWelcome();
            }
            else
            {
                // Get current mode from OVRGraphController to ensure we show the right tutorial
                if (graphController != null)
                {
                    ShowForMode(graphController.currentMode);
                }
                else
                {
                    ShowForMode(currentMode); // Fallback to stored mode
                }
            }
        }
    }

    void UpdatePanelPosition()
    {
        Vector3 cameraPos = Camera.main.transform.position;
        Vector3 cameraForward = Camera.main.transform.forward;

        // Keep forward direction horizontal
        cameraForward.y = 0;
        cameraForward.Normalize();

        // Calculate target position
        Vector3 targetPos = cameraPos
            + Camera.main.transform.right * panelOffset.x
            + Vector3.up * panelOffset.y
            + cameraForward * panelOffset.z;

        // Smoothly move panel
        panelTransform.position = Vector3.Lerp(panelTransform.position, targetPos, Time.deltaTime * followSpeed);

        // Face camera
        Vector3 lookDirection = cameraPos - panelTransform.position;
        lookDirection.y = 0;

        if (lookDirection.magnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(-lookDirection);
            panelTransform.rotation = Quaternion.Slerp(panelTransform.rotation, targetRotation, Time.deltaTime * followSpeed);
        }
    }

    #endregion

    #region Public API

    public bool IsTutorialActive()
    {
        return isShowingTutorial;
    }

    public void ResetAllTutorials()
    {
        hasShownWelcome = false;
        foreach (var key in hasShownForMode.Keys)
        {
            hasShownForMode[key] = false;
        }
        foreach (var key in actionCountPerMode.Keys)
        {
            actionCountPerMode[key] = 0;
        }
    }

    public void DisableTutorials()
    {
        showModeHelp = false;
        HidePanel();
    }

    public void EnableTutorials()
    {
        showModeHelp = true;
    }

    #endregion
}
