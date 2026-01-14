using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Manages first-time tutorial tooltips that appear near the controller
/// </summary>
public class TutorialTooltipSystem : MonoBehaviour
{
    [Header("References")]
    public Transform tooltipParent; // Usually attached to controller
    public TextMeshPro tooltipText;
    public GameObject tooltipBackground;

    [Header("Settings")]
    public float displayDuration = 5f;
    public bool enableTutorials = true;

    private Dictionary<string, bool> shownTips = new Dictionary<string, bool>();
    private float currentDisplayTimer = 0f;
    private bool isDisplaying = false;

    // Tooltip messages for each mode (first time usage)
    private static readonly Dictionary<OVRGraphController.Mode, string> firstTimeTips = new Dictionary<OVRGraphController.Mode, string>
    {
        { OVRGraphController.Mode.AddNode, "Welcome! Place nodes by pointing and pressing TRIGGER.\nNodes snap to the grid." },
        { OVRGraphController.Mode.AddEdge, "Connect two nodes to create structural members.\nFirst TRIGGER: Select start node\nSecond TRIGGER: Select end node" },
        { OVRGraphController.Mode.AddLoad, "Apply forces to nodes.\nFirst TRIGGER: Select node\nSecond TRIGGER: Set force direction & magnitude" },
        { OVRGraphController.Mode.ToggleSupport, "Toggle nodes between free and fixed supports.\nFixed supports (constraints) prevent movement." },
        { OVRGraphController.Mode.Move, "Drag individual elements.\nPoint at node/edge/load and hold TRIGGER to move." },
        { OVRGraphController.Mode.Delete, "Delete elements by pointing and pressing TRIGGER.\nDeleting nodes also removes connected edges and loads." },
        { OVRGraphController.Mode.Grab, "Move entire connected structures.\nPoint at any part and hold TRIGGER to drag the whole structure." },
        { OVRGraphController.Mode.Analyze, "Run structural analysis to see forces and displacements.\nTRIGGER: Confirm analysis\nGRIP: Cancel\nLeft stick: Adjust displacement scale" },
        { OVRGraphController.Mode.Grid, "Customize the grid alignment.\nTRIGGER: Set anchor points\nLeft stick: Adjust parameters\nLeft stick press: Toggle grid visibility" }
    };

    void Update()
    {
        if (isDisplaying)
        {
            currentDisplayTimer -= Time.deltaTime;
            if (currentDisplayTimer <= 0f)
            {
                HideTooltip();
            }
        }
    }

    /// <summary>
    /// Show tooltip for a mode (only if first time)
    /// </summary>
    public void ShowModeTooltip(OVRGraphController.Mode mode)
    {
        if (!enableTutorials) return;

        string tipKey = "mode_" + mode.ToString();

        // Check if already shown
        if (shownTips.ContainsKey(tipKey) && shownTips[tipKey])
            return;

        // Mark as shown
        shownTips[tipKey] = true;

        // Get tooltip text
        if (firstTimeTips.ContainsKey(mode))
        {
            ShowTooltip(firstTimeTips[mode]);
        }
    }

    /// <summary>
    /// Show a custom tooltip message
    /// </summary>
    public void ShowTooltip(string message, float duration = -1f)
    {
        if (!enableTutorials) return;

        if (tooltipText != null)
        {
            tooltipText.text = message;
        }

        if (tooltipBackground != null)
        {
            tooltipBackground.SetActive(true);
        }

        isDisplaying = true;
        currentDisplayTimer = duration > 0 ? duration : displayDuration;
    }

    /// <summary>
    /// Hide the tooltip
    /// </summary>
    public void HideTooltip()
    {
        if (tooltipBackground != null)
        {
            tooltipBackground.SetActive(false);
        }

        isDisplaying = false;
    }

    /// <summary>
    /// Reset all tutorials (for testing or user request)
    /// </summary>
    public void ResetTutorials()
    {
        shownTips.Clear();
        HideTooltip();
    }

    /// <summary>
    /// Disable all tutorials
    /// </summary>
    public void DisableTutorials()
    {
        enableTutorials = false;
        HideTooltip();
    }

    /// <summary>
    /// Enable tutorials
    /// </summary>
    public void EnableTutorials()
    {
        enableTutorials = true;
    }
}
