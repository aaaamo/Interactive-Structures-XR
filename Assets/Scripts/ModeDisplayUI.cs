using UnityEngine;
using TMPro;

/// <summary>
/// Enhanced mode display with icons, colors, and hints
/// </summary>
public class ModeDisplayUI : MonoBehaviour
{
    [Header("References")]
    public TextMeshPro modeNameText;
    public TextMeshPro hintText;
    public TextMeshPro iconText; // Using TextMeshPro with unicode symbols as icons

    [Header("Mode Colors")]
    public Color networkColor = new Color(0.5f, 0.5f, 0.5f);
    public Color setupWorldColor = new Color(1f, 0.9f, 0.3f);
    public Color addNodeColor = new Color(0.2f, 1f, 0.2f);
    public Color addEdgeColor = new Color(0.2f, 0.6f, 1f);
    public Color addLoadColor = new Color(1f, 0.6f, 0.2f);
    public Color toggleSupportColor = new Color(0.8f, 0.8f, 0.2f);
    public Color moveColor = new Color(0.6f, 0.3f, 1f);
    public Color deleteColor = new Color(1f, 0.2f, 0.2f);
    public Color grabColor = new Color(0.3f, 0.9f, 1f);
    public Color analyzeColor = new Color(1f, 1f, 1f);
    public Color gridColor = new Color(0.7f, 0.7f, 0.7f);
    public Color importColor = new Color(0.5f, 0.9f, 1f);

    private static readonly string[] modeIcons = {
        "\u26A1",  // Network - Lightning bolt
        "\u2316",  // SetupWorld - Crosshair/position
        "\u25CF",  // AddNode - Filled circle
        "\u2015",  // AddEdge - Horizontal line
        "\u2193",  // AddLoad - Down arrow
        "\u25A0",  // ToggleSupport - Square
        "\u21C4",  // Move - Left-right arrow
        "\u2716",  // Delete - X
        "\u270B",  // Grab - Hand
        "\u2699",  // Analyze - Gear
        "\u2637",  // Grid - Grid symbol
        "\u2913"   // Import - Down arrow to bar (download)
    };

    private static readonly string[] modeHints = {
        "Connect to network to start",
        "Set origin, then direction point",
        "Point and TRIGGER to place node",
        "Select first node, then second node",
        "Select node, then point direction and TRIGGER",
        "Point at node and TRIGGER to toggle",
        "Point and hold TRIGGER to drag",
        "Point and TRIGGER to delete",
        "Point and TRIGGER to move structure",
        "TRIGGER to analyze | GRIP to cancel",
        "TRIGGER to set grid anchors",
        "TRIGGER to open file browser"
    };

    /// <summary>
    /// Update display for current mode
    /// </summary>
    public void UpdateModeDisplay(OVRGraphController.Mode mode, OVRGraphController.GridAxis gridAxis = OVRGraphController.GridAxis.X, float gridSpacing = 0.1f)
    {
        int modeIndex = (int)mode;
        Color modeColor = GetColorForMode(mode);

        // Update mode name
        if (modeNameText != null)
        {
            modeNameText.text = mode.ToString();
            modeNameText.color = modeColor;

            // Add grid info if in grid mode
            if (mode == OVRGraphController.Mode.Grid)
            {
                modeNameText.text += $"\n{gridAxis}";
                if (gridAxis == OVRGraphController.GridAxis.Spacing)
                {
                    modeNameText.text += $": {gridSpacing:F3}m";
                }
            }
        }

        // Update icon
        if (iconText != null && modeIndex < modeIcons.Length)
        {
            iconText.text = modeIcons[modeIndex];
            iconText.color = modeColor;
            iconText.fontSize = 0.15f;
        }

        // Update hint
        if (hintText != null && modeIndex < modeHints.Length)
        {
            hintText.text = modeHints[modeIndex];
            hintText.color = new Color(0.8f, 0.8f, 0.8f);
            hintText.fontSize = 0.04f;
        }
    }

    /// <summary>
    /// Show custom message (e.g., for errors or warnings)
    /// </summary>
    public void ShowMessage(string message, Color color)
    {
        if (hintText != null)
        {
            hintText.text = message;
            hintText.color = color;
        }
    }

    /// <summary>
    /// Clear custom message and restore hint
    /// </summary>
    public void ClearMessage(OVRGraphController.Mode mode)
    {
        int modeIndex = (int)mode;
        if (hintText != null && modeIndex < modeHints.Length)
        {
            hintText.text = modeHints[modeIndex];
            hintText.color = new Color(0.8f, 0.8f, 0.8f);
        }
    }

    private Color GetColorForMode(OVRGraphController.Mode mode)
    {
        switch (mode)
        {
            case OVRGraphController.Mode.Network: return networkColor;
            case OVRGraphController.Mode.SetupWorld: return setupWorldColor;
            case OVRGraphController.Mode.AddNode: return addNodeColor;
            case OVRGraphController.Mode.AddEdge: return addEdgeColor;
            case OVRGraphController.Mode.AddLoad: return addLoadColor;
            case OVRGraphController.Mode.ToggleSupport: return toggleSupportColor;
            case OVRGraphController.Mode.Move: return moveColor;
            case OVRGraphController.Mode.Delete: return deleteColor;
            case OVRGraphController.Mode.Grab: return grabColor;
            case OVRGraphController.Mode.Analyze: return analyzeColor;
            case OVRGraphController.Mode.Grid: return gridColor;
            case OVRGraphController.Mode.Import: return importColor;
            default: return Color.white;
        }
    }
}
