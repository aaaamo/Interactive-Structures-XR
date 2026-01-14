using UnityEngine;
using TMPro;

/// <summary>
/// Smart tutorial panel that shows contextual help only when needed
/// Auto-hides after user demonstrates understanding
/// </summary>
public class ContextualTutorialPanel : MonoBehaviour
{
    [Header("References")]
    public TextMeshPro tutorialText;
    public GameObject panelObject;
    public Transform panelTransform;

    [Header("Settings")]
    public bool showTutorials = true;
    public float autoHideDelay = 10f; // Hide after 10 seconds if no action
    public Vector3 panelOffset = new Vector3(0.6f, 0.2f, 0.8f); // Offset from camera (x=right, y=up, z=forward)
    public bool followView = true; // Panel follows your view
    public float followSpeed = 3f; // How fast panel follows

    private OVRGraphController.Mode currentMode;
    private bool isPanelVisible = false;
    private float showTimer = 0f;
    private System.Collections.Generic.Dictionary<OVRGraphController.Mode, bool> hasShownForMode = new System.Collections.Generic.Dictionary<OVRGraphController.Mode, bool>();
    private System.Collections.Generic.Dictionary<OVRGraphController.Mode, int> actionCountPerMode = new System.Collections.Generic.Dictionary<OVRGraphController.Mode, int>();

    // Track when to hide tutorial
    private const int ACTIONS_BEFORE_HIDE = 3; // Hide after 3 successful actions in this mode

    // Remove "position once" tracking since we now follow continuously
    // private bool hasPositionedOnce = false; // REMOVED

    void Start()
    {
        Debug.Log("[ContextualTutorial] Start() called");

        if (panelObject != null)
        {
            panelObject.SetActive(false);
            Debug.Log("[ContextualTutorial] Panel object found and hidden initially");
        }
        else
        {
            Debug.LogError("[ContextualTutorial] Panel object is NOT assigned in Inspector!");
        }

        // Initialize tracking dictionaries
        foreach (OVRGraphController.Mode mode in System.Enum.GetValues(typeof(OVRGraphController.Mode)))
        {
            hasShownForMode[mode] = false;
            actionCountPerMode[mode] = 0;
        }

        Debug.Log($"[ContextualTutorial] Initialized tracking for {hasShownForMode.Count} modes");
    }

    void Update()
    {
        if (!showTutorials) return;

        // Position panel if visible and following view
        if (isPanelVisible && followView && panelTransform != null && Camera.main != null)
        {
            PositionPanel();
        }

        // Auto-hide timer
        if (isPanelVisible)
        {
            showTimer += Time.deltaTime;
            if (showTimer > autoHideDelay)
            {
                HidePanel();
            }
        }

        // Toggle with Y button (Button.Four)
        if (OVRInput.GetDown(OVRInput.Button.Four, OVRInput.Controller.RTouch))
        {
            if (isPanelVisible)
            {
                HidePanel();
            }
            else
            {
                ShowForMode(currentMode);
            }
        }
    }

    /// <summary>
    /// Show tutorial for a specific mode (called when mode changes)
    /// </summary>
    public void ShowForMode(OVRGraphController.Mode mode)
    {
        currentMode = mode;

        Debug.Log($"[ContextualTutorial] ShowForMode called for: {mode}");

        // Don't show if user has already learned this mode
        if (hasShownForMode.ContainsKey(mode) && hasShownForMode[mode])
        {
            Debug.Log($"[ContextualTutorial] Already shown for {mode}, skipping");
            return; // User already knows this mode
        }

        // Show the tutorial
        isPanelVisible = true;
        showTimer = 0f;

        Debug.Log($"[ContextualTutorial] Showing tutorial for {mode}");

        if (panelObject != null)
        {
            panelObject.SetActive(true);
            Debug.Log("[ContextualTutorial] Panel object activated");
        }
        else
        {
            Debug.LogWarning("[ContextualTutorial] Panel object is NULL!");
        }

        UpdateTutorialText(mode);
        hasShownForMode[mode] = true; // Mark as shown
    }

    /// <summary>
    /// Call this when user performs a successful action
    /// After N actions, hide the tutorial for this mode permanently
    /// </summary>
    public void OnActionPerformed(OVRGraphController.Mode mode)
    {
        if (!actionCountPerMode.ContainsKey(mode))
        {
            actionCountPerMode[mode] = 0;
        }

        actionCountPerMode[mode]++;

        // Reset timer (user is actively using it)
        showTimer = 0f;

        // After enough actions, assume user understands
        if (actionCountPerMode[mode] >= ACTIONS_BEFORE_HIDE && mode == currentMode)
        {
            HidePanel();
        }
    }

    /// <summary>
    /// Hide the panel
    /// </summary>
    public void HidePanel()
    {
        isPanelVisible = false;

        if (panelObject != null)
        {
            panelObject.SetActive(false);
        }
    }

    /// <summary>
    /// Force show panel for current mode (B button)
    /// </summary>
    public void ForceShow()
    {
        ShowForMode(currentMode);
    }

    /// <summary>
    /// Update tutorial text based on mode
    /// </summary>
    void UpdateTutorialText(OVRGraphController.Mode mode)
    {
        if (tutorialText == null) return;

        string text = GetTutorialForMode(mode);
        tutorialText.text = text;
    }

    /// <summary>
    /// Get concise tutorial text for each mode
    /// </summary>
    string GetTutorialForMode(OVRGraphController.Mode mode)
    {
        switch (mode)
        {
            case OVRGraphController.Mode.AddNode:
                return @"<size=0.1><size=0.1><b><color=#88FF88>ADD NODE</color></b></size>

<size=0.08><color=#FFFF88>TRIGGER</color> to place nodes
Nodes snap to grid automatically</size>

<size=0.05><i>Press Y to hide</i></size>";

            case OVRGraphController.Mode.AddEdge:
                return @"<size=0.1><b><color=#88DDFF>ADD EDGE</color></b>

1. <color=#FFFF88>TRIGGER</color> on first node
2. <color=#FFFF88>TRIGGER</color> on second node

Green line = valid
Red line = duplicate

</size>

<size=0.05><i>Press Y to hide</i></size>";

            case OVRGraphController.Mode.AddLoad:
                return @"<size=0.1><b><color=#FFAA88>ADD LOAD</color></b>

1. <color=#FFFF88>TRIGGER</color> to select node
2. Point direction
3. <color=#FFFF88>TRIGGER</color> to confirm

Arrow length = force

</size>

<size=0.05><i>Press Y to hide</i></size>";

            case OVRGraphController.Mode.ToggleSupport:
                return @"<size=0.1><b><color=#FFFF88>TOGGLE SUPPORT</color></b></size>

<size=0.08><color=#FFFF88>TRIGGER</color> on node to fix/unfix

Fixed nodes = anchors
Need supports for stability

</size>

<size=0.05><i>Press Y to hide</i></size>";

            case OVRGraphController.Mode.Move:
                return @"<size=0.1><b><color=#AA88FF>MOVE</color></b>

Point at element
Hold <color=#FFFF88>TRIGGER</color> to drag
Release to drop

Works on nodes, edges, loads

</size>

<size=0.05><i>Press Y to hide</i></size>";

            case OVRGraphController.Mode.Delete:
                return @"<size=0.1><b><color=#FF8888>DELETE</color></b></size>

<size=0.08><color=#FFFF88>TRIGGER</color> to delete element

⚠️ Red highlight = warning
⚠️ No undo!

</size>

<size=0.05><i>Press Y to hide</i></size>";

            case OVRGraphController.Mode.Grab:
                return @"<size=0.1><b><color=#88DDFF>GRAB</color></b>

Point at any part
Cyan = whole structure
Hold <color=#FFFF88>TRIGGER</color> to move all

</size>

<size=0.05><i>Press Y to hide</i></size>";

            case OVRGraphController.Mode.Analyze:
                return @"<size=0.1><b><color=#FFFFFF>ANALYZE</color></b></size>

<size=0.08><color=#FFFF88>TRIGGER</color> = Run analysis
<color=#FFFF88>GRIP</color> = Cancel

<color=#FF8888>Red</color> = Tension
<color=#8888FF>Blue</color> = Compression

Left thumbstick = scale

</size>

<size=0.05><i>Press Y to hide</i></size>";

            case OVRGraphController.Mode.Grid:
                return @"<size=0.1><b><color=#AAAAAA>GRID</color></b></size>

<size=0.08><color=#FFFF88>TRIGGER</color> = Set anchors
Left stick ↕ = Adjust values
Left stick ↔ = Change axis
Left stick press = Toggle visibility

</size>

<size=0.05><i>Press Y to hide</i></size>";

            default:
                return "Tutorial";
        }
    }

    /// <summary>
    /// Position panel to right side of view
    /// </summary>
    void PositionPanel()
    {
        Vector3 cameraPos = Camera.main.transform.position;
        Vector3 cameraForward = Camera.main.transform.forward;
        Vector3 cameraRight = Camera.main.transform.right;

        // Keep forward horizontal
        cameraForward.y = 0;
        cameraForward.Normalize();

        cameraRight.y = 0;
        cameraRight.Normalize();

        // Position to the right side
        Vector3 targetPos = cameraPos
            + cameraRight * panelOffset.x
            + Vector3.up * panelOffset.y
            + cameraForward * panelOffset.z;

        panelTransform.position = Vector3.Lerp(panelTransform.position, targetPos, Time.deltaTime * 5f);

        // Face the camera
        Vector3 lookDir = cameraPos - panelTransform.position;
        lookDir.y = 0;
        if (lookDir.magnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(-lookDir);
            panelTransform.rotation = Quaternion.Slerp(panelTransform.rotation, targetRotation, Time.deltaTime * 5f);
        }
    }

    /// <summary>
    /// Reset all tutorials (for testing or user preference)
    /// </summary>
    public void ResetAllTutorials()
    {
        foreach (var key in hasShownForMode.Keys)
        {
            hasShownForMode[key] = false;
        }
        foreach (var key in actionCountPerMode.Keys)
        {
            actionCountPerMode[key] = 0;
        }
    }

    /// <summary>
    /// Disable tutorials
    /// </summary>
    public void DisableTutorials()
    {
        showTutorials = false;
        HidePanel();
    }
}
