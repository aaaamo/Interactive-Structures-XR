using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Welcome tutorial that shows at app start with step-by-step instructions
/// </summary>
public class WelcomeTutorial : MonoBehaviour
{
    [Header("References")]
    public TextMeshPro tutorialText;
    public GameObject tutorialPanel;
    public Transform panelTransform; // Position in front of user

    [Header("Settings")]
    public float stepDuration = 6f;
    public bool showOnStart = true;
    public Vector3 panelOffset = new Vector3(0, 1.6f, 2.0f); // Offset from camera (x=left/right, y=up/down, z=forward)
    public bool followView = true; // Panel follows your view
    public float followSpeed = 3f; // How fast panel follows

    private bool hasShownTutorial = false;
    private int currentStep = 0;
    private bool isShowingTutorial = false;

    // Tutorial steps
    private static readonly string[] tutorialSteps = new string[]
    {
        "<b>Welcome to Interactive Structures XR!</b>\n\nThis app lets you build and analyze 3D truss structures in VR.\n\nPress TRIGGER to continue...",

        "<b>Basic Controls</b>\n\n<color=#88FF88>Right Thumbstick Left/Right:</color> Switch modes\n<color=#88FF88>Right Trigger:</color> Primary action\n<color=#88FF88>A Button:</color> Detect table surface\n\nPress TRIGGER to continue...",

        "<b>Building Structures</b>\n\n1. <color=#88FF88>AddNode:</color> Place structural joints (nodes)\n2. <color=#88FF88>AddEdge:</color> Connect nodes with members\n3. <color=#88FF88>AddLoad:</color> Apply forces to nodes\n4. <color=#88FF88>ToggleSupport:</color> Fix nodes in place\n\nPress TRIGGER to continue...",

        "<b>Editing Structures</b>\n\n5. <color=#AA88FF>Move:</color> Drag individual elements\n6. <color=#FF8888>Delete:</color> Remove elements\n7. <color=#88DDFF>Grab:</color> Move entire structures\n8. <color=#FFFFFF>Analyze:</color> Run structural analysis\n\nPress TRIGGER to continue...",

        "<b>Analysis Mode</b>\n\nAfter building, use <color=#FFFFFF>Analyze</color> mode to:\n\u2022 Calculate forces in each member\n\u2022 Visualize displacements (exaggerated)\n\u2022 See support reactions\n\n<color=#FF8888>Red = Tension</color> | <color=#8888FF>Blue = Compression</color>\n\nPress TRIGGER to continue...",

        "<b>Grid System</b>\n\nUse <color=#AAAAAA>Grid</color> mode to:\n\u2022 Adjust grid spacing and size\n\u2022 Align grid to surfaces\n\u2022 Toggle grid visibility (Left stick press)\n\nNodes snap to grid for precision!\n\nPress TRIGGER to start building!"
    };

    void Start()
    {
        if (showOnStart && !hasShownTutorial)
        {
            StartCoroutine(ShowTutorialSequence());
        }
        else if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(false);
        }
    }

    void Update()
    {
        // Toggle tutorial with X button (Button.Three on left controller)
        if (OVRInput.GetDown(OVRInput.Button.Three, OVRInput.Controller.LTouch))
        {
            if (isShowingTutorial)
            {
                SkipTutorial();
            }
            else
            {
                ReplayTutorial();
            }
        }

        // Update panel position to follow view
        if (isShowingTutorial && followView && panelTransform != null && Camera.main != null)
        {
            UpdatePanelPosition();
        }
    }

    /// <summary>
    /// Update panel position to follow camera view
    /// </summary>
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

        // Smoothly move panel to target position
        panelTransform.position = Vector3.Lerp(panelTransform.position, targetPos, Time.deltaTime * followSpeed);

        // Make panel face the camera
        Vector3 lookDirection = cameraPos - panelTransform.position;
        lookDirection.y = 0; // Keep rotation horizontal

        if (lookDirection.magnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(-lookDirection);
            panelTransform.rotation = Quaternion.Slerp(panelTransform.rotation, targetRotation, Time.deltaTime * followSpeed);
        }
    }

    /// <summary>
    /// Show the tutorial sequence
    /// </summary>
    public IEnumerator ShowTutorialSequence()
    {
        hasShownTutorial = true;
        isShowingTutorial = true;
        currentStep = 0;

        // Position panel in front of user
        if (panelTransform != null && Camera.main != null)
        {
            Vector3 cameraPos = Camera.main.transform.position;
            Vector3 cameraForward = Camera.main.transform.forward;
            cameraForward.y = 0; // Keep panel at eye level
            cameraForward.Normalize();

            panelTransform.position = cameraPos + cameraForward * panelOffset.z + Vector3.up * panelOffset.y;
            panelTransform.LookAt(Camera.main.transform);
            panelTransform.Rotate(0, 180, 0); // Face user
        }

        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(true);
        }

        // Show each step
        while (currentStep < tutorialSteps.Length)
        {
            ShowStep(currentStep);

            // Wait for trigger press or timeout
            float timer = 0f;
            bool triggered = false;

            while (timer < stepDuration * 3f) // Max 3x duration to prevent infinite wait
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

            currentStep++;

            // Small delay between steps
            yield return new WaitForSeconds(0.3f);
        }

        // Tutorial complete
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(false);
        }

        isShowingTutorial = false;
        HapticFeedback.Trigger(HapticFeedback.HapticType.Success);
    }

    /// <summary>
    /// Show a specific tutorial step
    /// </summary>
    void ShowStep(int stepIndex)
    {
        if (tutorialText != null && stepIndex < tutorialSteps.Length)
        {
            tutorialText.text = tutorialSteps[stepIndex];
        }
    }

    /// <summary>
    /// Skip tutorial
    /// </summary>
    public void SkipTutorial()
    {
        StopAllCoroutines();
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(false);
        }
        isShowingTutorial = false;
    }

    /// <summary>
    /// Replay tutorial (for user request)
    /// </summary>
    public void ReplayTutorial()
    {
        hasShownTutorial = false;
        StartCoroutine(ShowTutorialSequence());
    }

    /// <summary>
    /// Check if tutorial is currently showing
    /// </summary>
    public bool IsTutorialActive()
    {
        return isShowingTutorial;
    }
}
