using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro;

/// <summary>
/// Single-panel HUD. Layout (top to bottom):
///
///   [Y]    ⚙ Setup  ● Build  ⭐ Optimize      ← modeNameText
///   [R←→]  Node  Edge  Load  Support  …       ← toolText  (grayed while selecting mode)
///          Current action hint                 ← hintText
///          C = 0.0234                          ← complianceText  (Optimize only)
///          ▁▂▃▅▇|▅                             ← sparklineText   (Optimize only)
/// </summary>
public class ModeDisplayUI : MonoBehaviour
{
    [Header("Text References")]
    public TextMeshPro modeNameText;
    public TextMeshPro toolText;
    public TextMeshPro hintText;
    public TextMeshPro complianceText;
    public TextMeshPro sparklineText;   // kept for legacy wiring; hidden at runtime

    [Header("Graph")]
    public ComplianceGraphUI complianceGraph;

    [Header("Colors")]
    public Color activeColor   = Color.white;
    public Color cursorColor   = new Color(1f, 0.85f, 0f);   // gold — mode cursor
    public Color dimColor      = new Color(0.45f, 0.45f, 0.45f);
    public Color hintColor     = new Color(0.75f, 0.75f, 0.75f);
    public Color optimizeColor = new Color(1f, 0.85f, 0f);
    public Color setupColor    = new Color(1f, 0.9f, 0.3f);
    public Color buildColor    = new Color(0.2f, 1f, 0.2f);

    // ------------------------------------------------------------------ //
    // Static content tables
    // ------------------------------------------------------------------ //

    private static readonly string[] ModeIcons  = { "\u2316", "\u25CF", "\u2B50" }; // ⌖ ● ⭐
    private static readonly string[] ModeNames  = { "Setup", "Build", "Optimize" };

    private static readonly string[] BuildToolNames =
        { "Node", "Edge", "Load", "Support", "Delete", "Select", "Analyze" };

    private static readonly string[] SetupToolNames =
        { "World", "Grid", "Network", "Import" };

    private static readonly string[] OptimizeToolNames =
        { "Move", "Rollback" };

    // Hint per Build tool
    private const string BuildGripHint = "GRIP drag  ·  BOTH GRIP grab all";

    private static readonly string[] BuildHints =
    {
        "TRIGGER to place node",
        "TRIGGER 1st node → TRIGGER 2nd node",
        "TRIGGER node → point → TRIGGER to set load",
        "TRIGGER node to toggle support",
        "TRIGGER to delete node / edge / load",
        "TRIGGER to select · GRIP to move selected",
        "TRIGGER to run analysis · L↕ exaggeration · blue=tension red=compression"
    };

    // Hint per Setup tool
    private static readonly string[] SetupHints =
    {
        "TRIGGER to set world coordinate origin",
        "TRIGGER to anchor grid · L←→ axis · L↕ size",
        "Use the network panel to host or join session",
        "TRIGGER to load a saved structure"
    };

    // Hint per Optimize tool
    private static readonly string[] OptimizeHints =
    {
        "GRIP drag nodes · BOTH GRIP grab all · follow arrows",
        "L←→ browse history · TRIGGER to rollback · B to jump to latest"
    };

    private const string SelectingModeHint =
        "[R←→] to choose mode · TRIGGER or A to confirm · B to cancel";

    // ------------------------------------------------------------------ //
    // Internal
    // ------------------------------------------------------------------ //

    private Coroutine _flashCoroutine;

    // ------------------------------------------------------------------ //
    // Main update — called every frame from OVRGraphController.UpdateModeText()
    // ------------------------------------------------------------------ //

    public void UpdateModeDisplay(
        OVRGraphController.Mode         mode,
        OVRGraphController.BuildTool    buildTool,
        OVRGraphController.SetupTool    setupTool,
        OVRGraphController.GridAxis     gridAxis,
        float                           gridSpacing,
        OVRGraphController.OptimizeTool optimizeTool  = OVRGraphController.OptimizeTool.Move,
        bool                            selectingMode = false,
        int                             modeCursor    = -1)
    {
        // ── Mode name row ──────────────────────────────────────────────
        if (modeNameText != null)
        {
            modeNameText.text = BuildModeRow(mode, selectingMode, modeCursor);
        }

        // ── Tool row ───────────────────────────────────────────────────
        if (toolText != null)
        {
            if (selectingMode)
            {
                // Gray out entire tool row while selecting mode
                toolText.text  = DimText(BuildToolRowPlain(mode, buildTool, setupTool, optimizeTool, gridAxis, gridSpacing));
                toolText.color = dimColor;
            }
            else
            {
                toolText.text  = BuildToolRow(mode, buildTool, setupTool, optimizeTool, gridAxis, gridSpacing);
                toolText.color = Color.white;
            }
        }

        // ── Hint ───────────────────────────────────────────────────────
        if (hintText != null && _flashCoroutine == null)
        {
            hintText.color = hintColor;
            if (selectingMode)
            {
                hintText.text = SelectingModeHint;
            }
            else
            {
                hintText.text = GetHint(mode, buildTool, setupTool, optimizeTool);
            }
        }

        // ── Compliance + graph (Optimize only) ────────────────────────
        bool inOptimize = mode == OVRGraphController.Mode.Optimize && !selectingMode;
        if (complianceText != null) { complianceText.gameObject.SetActive(inOptimize); }
        if (sparklineText  != null) { sparklineText.gameObject.SetActive(false); }  // replaced by graph
        complianceGraph?.SetVisible(inOptimize);
    }

    // ------------------------------------------------------------------ //
    // Compliance readout
    // ------------------------------------------------------------------ //

    public void SetCompliance(float compliance, bool valid)
    {
        if (complianceText == null) { return; }
        complianceText.text  = valid ? $"C = {compliance:G4}" : "C = --";
        complianceText.color = optimizeColor;
    }

    // ------------------------------------------------------------------ //
    // Compliance graph
    // ------------------------------------------------------------------ //

    /// <summary>
    /// Redraw the 2D compliance history graph.
    /// showCursor = true when in Rollback tool (splits line at cursor position).
    /// </summary>
    public void UpdateGraph(
        IReadOnlyList<StructureSnapshot> history,
        int cursorIndex, int bestIndex, bool showCursor)
    {
        complianceGraph?.UpdateGraph(history, cursorIndex, bestIndex, showCursor);
    }

    // ------------------------------------------------------------------ //
    // Optimize panel (legacy — replaced by direct SetCompliance + UpdateGraph calls)
    // ------------------------------------------------------------------ //

    public void UpdateOptimizePanel(float compliance, bool isNewBest, bool visible)
    {
        SetCompliance(compliance, visible);
    }

    public void ShowOptimizeFeedback(bool improved)
    {
        if (_flashCoroutine != null) { StopCoroutine(_flashCoroutine); }
        _flashCoroutine = StartCoroutine(FlashHint(
            improved ? "↓ Compliance reduced!" : "No improvement",
            improved ? new Color(0.2f, 1f, 0.4f) : new Color(1f, 0.55f, 0.1f),
            1.5f));
    }

    // ------------------------------------------------------------------ //
    // Transient messages
    // ------------------------------------------------------------------ //

    public void ShowMessage(string message, Color color)
    {
        if (hintText == null) { return; }
        hintText.text  = message;
        hintText.color = color;
    }

    public void ClearMessage(OVRGraphController.Mode mode)
    {
        if (hintText == null || _flashCoroutine != null) { return; }
        hintText.text  = GetHint(mode,
            OVRGraphController.BuildTool.Node,
            OVRGraphController.SetupTool.World);
        hintText.color = hintColor;
    }

    // ------------------------------------------------------------------ //
    // Row builders
    // ------------------------------------------------------------------ //

    string BuildModeRow(OVRGraphController.Mode active, bool selecting, int cursor)
    {
        var sb = new StringBuilder();
        sb.Append("<size=70%><color=#888888>[A]</color></size>  ");

        for (int i = 0; i < ModeNames.Length; i++)
        {
            if (i > 0) { sb.Append("   "); }

            string icon  = ModeIcons[i];
            string label = ModeNames[i];
            bool   isCursor = selecting && i == cursor;
            bool   isActive = i == (int)active;

            if (isCursor)
            {
                sb.Append($"<b><color=#{ColorHex(cursorColor)}>{label}</color></b>");
            }
            else if (isActive && !selecting)
            {
                Color c = ModeColor(i);
                sb.Append($"<b><color=#{ColorHex(c)}>{label}</color></b>");
            }
            else
            {
                sb.Append($"<color=#{ColorHex(dimColor)}>{label}</color>");
            }
        }
        return sb.ToString();
    }

    string BuildToolRow(
        OVRGraphController.Mode         mode,
        OVRGraphController.BuildTool    buildTool,
        OVRGraphController.SetupTool    setupTool,
        OVRGraphController.OptimizeTool optimizeTool,
        OVRGraphController.GridAxis     gridAxis,
        float                           gridSpacing)
    {
        var sb = new StringBuilder();
        sb.Append("<size=70%><color=#888888>[R←→]</color></size>  ");

        if (mode == OVRGraphController.Mode.Build)
        {
            int active = (int)buildTool;
            for (int i = 0; i < BuildToolNames.Length; i++)
            {
                if (i > 0) { sb.Append("  "); }
                sb.Append(i == active
                    ? $"<b><color=#FFFFFF>{BuildToolNames[i]}</color></b>"
                    : $"<color=#{ColorHex(dimColor)}>{BuildToolNames[i]}</color>");
            }
        }
        else if (mode == OVRGraphController.Mode.Setup)
        {
            int active = (int)setupTool;
            for (int i = 0; i < SetupToolNames.Length; i++)
            {
                if (i > 0) { sb.Append("  "); }
                string label = SetupToolNames[i];
                if ((OVRGraphController.SetupTool)i == OVRGraphController.SetupTool.Grid
                    && i == active)
                {
                    label += gridAxis == OVRGraphController.GridAxis.Spacing
                        ? $" [{gridAxis}:{gridSpacing:F3}m]"
                        : $" [{gridAxis}]";
                }
                sb.Append(i == active
                    ? $"<b><color=#FFFFFF>{label}</color></b>"
                    : $"<color=#{ColorHex(dimColor)}>{label}</color>");
            }
        }
        else if (mode == OVRGraphController.Mode.Optimize)
        {
            int active = (int)optimizeTool;
            for (int i = 0; i < OptimizeToolNames.Length; i++)
            {
                if (i > 0) { sb.Append("  "); }
                sb.Append(i == active
                    ? $"<b><color=#FFFFFF>{OptimizeToolNames[i]}</color></b>"
                    : $"<color=#{ColorHex(dimColor)}>{OptimizeToolNames[i]}</color>");
            }
        }

        return sb.ToString();
    }

    // Plain (no rich-text color tags) version for dimming
    string BuildToolRowPlain(
        OVRGraphController.Mode         mode,
        OVRGraphController.BuildTool    buildTool,
        OVRGraphController.SetupTool    setupTool,
        OVRGraphController.OptimizeTool optimizeTool,
        OVRGraphController.GridAxis     gridAxis,
        float                           gridSpacing)
    {
        string[] names = mode == OVRGraphController.Mode.Build    ? BuildToolNames
                       : mode == OVRGraphController.Mode.Setup    ? SetupToolNames
                       : mode == OVRGraphController.Mode.Optimize ? OptimizeToolNames
                       : System.Array.Empty<string>();
        return "[R←→]  " + string.Join("  ", names);
    }

    static string DimText(string plain) =>
        $"<color=#444444>{plain}</color>";

    // ------------------------------------------------------------------ //
    // Hint lookup
    // ------------------------------------------------------------------ //

    static string GetHint(
        OVRGraphController.Mode         mode,
        OVRGraphController.BuildTool    buildTool,
        OVRGraphController.SetupTool    setupTool,
        OVRGraphController.OptimizeTool optimizeTool = OVRGraphController.OptimizeTool.Move)
    {
        if (mode == OVRGraphController.Mode.Build)
        {
            int    idx      = (int)buildTool;
            string toolHint = idx < BuildHints.Length ? BuildHints[idx] : "";
            // Grip hint always on top; tool-specific hint below
            return BuildGripHint + "\n" + toolHint;
        }
        if (mode == OVRGraphController.Mode.Setup)
        {
            int idx = (int)setupTool;
            return idx < SetupHints.Length ? SetupHints[idx] : "";
        }
        // Optimize
        {
            int idx = (int)optimizeTool;
            return idx < OptimizeHints.Length ? OptimizeHints[idx] : "";
        }
    }

    // ------------------------------------------------------------------ //
    // Helpers
    // ------------------------------------------------------------------ //

    Color ModeColor(int idx) => idx switch
    {
        0 => setupColor,
        1 => buildColor,
        2 => optimizeColor,
        _ => Color.white
    };

    static string ColorHex(Color c)
    {
        return $"{ToByte(c.r):X2}{ToByte(c.g):X2}{ToByte(c.b):X2}";
    }

    static int ToByte(float f) => Mathf.Clamp(Mathf.RoundToInt(f * 255), 0, 255);

    IEnumerator FlashHint(string message, Color color, float duration)
    {
        if (hintText != null)
        {
            hintText.text  = message;
            hintText.color = color;
        }
        yield return new WaitForSeconds(duration);
        _flashCoroutine = null;
    }
}
