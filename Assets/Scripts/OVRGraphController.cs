using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Core partial: enums, inspector fields, lifecycle (Start / Update),
/// mode selection, tool cycling, mode text, and public API.
/// </summary>
public partial class OVRGraphController : MonoBehaviour
{
    // ------------------------------------------------------------------ //
    // Enums
    // ------------------------------------------------------------------ //

    public enum Mode         { Setup, Build, Optimize }
    public enum BuildTool    { Node, Edge, Load, Support, Delete, Select, Analyze }
    public enum SetupTool    { World, Grid, Network, Import }
    public enum GridAxis     { X, Y, Z, Spacing }
    public enum OptimizeTool { Move, Rollback }

    // ------------------------------------------------------------------ //
    // Public state
    // ------------------------------------------------------------------ //

    public Mode         currentMode         = Mode.Build;
    public BuildTool    currentBuildTool    = BuildTool.Node;
    public SetupTool    currentSetupTool    = SetupTool.World;
    public GridAxis     currentGridAxis     = GridAxis.X;
    public OptimizeTool currentOptimizeTool = OptimizeTool.Move;

    // ------------------------------------------------------------------ //
    // Inspector references
    // ------------------------------------------------------------------ //

    [Header("References")]
    public GraphManager           graphManager;
    public Transform              markerTransform;
    public TextMeshPro            modeText;
    public StructuralAnalyzer     structuralAnalyzer;
    public OptimizeVisualizer     optimizeVisualizer;
    public OptimizeSession        optimizeSession;
    public GridPointRenderer      gridRenderer;
    public RaycastSurfaceFinder   surfaceFinder;
    public WorldCoordinateManager worldCoordinateManager;

    [Header("UI")]
    public MarkerController  markerController;
    public GameObject        ghostNodePrefab;
    public Material          ghostMaterial;
    public ModeDisplayUI     modeDisplayUI;

    [Header("Tutorial System")]
    public UnifiedTutorialSystem unifiedTutorial;

    [Header("Network")]
    public NetworkConnect networkConnect;

    // ------------------------------------------------------------------ //
    // Private state
    // ------------------------------------------------------------------ //

    // Two-step workflow state (Edge / Load)
    private NodeBehaviour firstSelectedNode;
    private EdgeBehaviour tempEdge;
    private NodeBehaviour firstLoadNode;
    private GameObject    ghostLoad;

    // Multi-select (BuildTool.Select)
    private readonly HashSet<NodeBehaviour> _selectedNodes = new HashSet<NodeBehaviour>();

    // Inline mode selection state (Y button)
    private bool _selectingMode  = false;
    private int  _modeCursor     = 0;

    // Input throttling
    private bool  triggerHeldLastFrame;
    private float lastThumbTime;
    private const float ThumbCooldown = 0.25f;

    // Visual feedback tracking
    private GameObject             ghostNode;
    private LineRenderer           ghostEdgeLine;
    private GameObject             hoveredObject;
    private HashSet<NodeBehaviour> highlightedGrabNodes = new HashSet<NodeBehaviour>();

    // ------------------------------------------------------------------ //
    // Unity lifecycle
    // ------------------------------------------------------------------ //

    void Start()
    {
        Debug.LogWarning("[OVRGraphController] Start() called");

        // Always start in Build mode regardless of Inspector value
        currentMode = Mode.Build;

        if (graphManager == null)
        {
            graphManager = FindObjectOfType<GraphManager>();
            if (graphManager == null)
                Debug.LogError("[OVRGraphController] GraphManager not found!");
        }

        if (markerController == null && markerTransform != null)
        {
            markerController = markerTransform.GetComponent<MarkerController>();
            if (markerController == null)
                markerController = markerTransform.gameObject.AddComponent<MarkerController>();
        }

        SetupGhostEdgeLine();
        SwitchToMode(currentMode);
    }

    void Update()
    {
        // Mode selection (Y button) — always runs, even before scene is fully wired
        HandleModeSelectInput();

        // Block all other input while selecting mode
        if (_selectingMode) { UpdateModeText(); return; }

        if (graphManager == null || markerTransform == null) { return; }

        HandleToolSwitch();
        HandleGripDrag();

        bool isConnected = NetworkManager.Singleton != null &&
                           (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer);

        switch (currentMode)
        {
            case Mode.Build:
                if (isConnected) { HandleBuildTrigger(); }
                UpdateBuildVisualFeedback();
                break;
            case Mode.Setup:
                HandleSetupTrigger(isConnected);
                UpdateSetupVisualFeedback();
                break;
            case Mode.Optimize:
                HandleOptimizeTrigger();
                break;
        }

        UpdateTemporaryEdge();
        UpdateTemporaryLoad();
        UpdateModeText();
    }

    // ------------------------------------------------------------------ //
    // Inline mode selection (Y button)
    // ------------------------------------------------------------------ //

    void HandleModeSelectInput()
    {
        bool yPressed = OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch);

        if (yPressed)
        {
            if (!_selectingMode)
            {
                // Enter mode-select state — cursor starts at current mode
                _selectingMode = true;
                _modeCursor    = (int)currentMode;
                HapticFeedback.Trigger(HapticFeedback.HapticType.Light);
            }
            else
            {
                // Y again → confirm selection
                ConfirmModeSelection();
            }
            return;
        }

        if (!_selectingMode) { return; }

        // Right thumbstick X moves cursor while selecting
        if (Time.time - lastThumbTime >= ThumbCooldown)
        {
            float x = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.RTouch).x;
            int count = System.Enum.GetValues(typeof(Mode)).Length;
            if (x > 0.7f)
            {
                _modeCursor   = (_modeCursor + 1) % count;
                lastThumbTime = Time.time;
                HapticFeedback.Trigger(HapticFeedback.HapticType.Light);
            }
            else if (x < -0.7f)
            {
                _modeCursor   = (_modeCursor - 1 + count) % count;
                lastThumbTime = Time.time;
                HapticFeedback.Trigger(HapticFeedback.HapticType.Light);
            }
        }

        // Trigger confirms, B cancels
        if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
        {
            ConfirmModeSelection();
        }
        else if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch))
        {
            _selectingMode = false; // cancel
            HapticFeedback.Trigger(HapticFeedback.HapticType.Light);
        }
    }

    void ConfirmModeSelection()
    {
        _selectingMode = false;
        SwitchToMode((Mode)_modeCursor);
    }

    // ------------------------------------------------------------------ //
    // Mode switching
    // ------------------------------------------------------------------ //

    void SwitchToMode(Mode newMode)
    {
        Mode oldMode = currentMode;
        currentMode  = newMode;

        HapticFeedback.Trigger(HapticFeedback.HapticType.Medium);
        markerController?.SetModeColor(currentMode);
        UpdatePreviewVisibility();
        ClearTempVisuals();
        ClearSelection();
        VisualFeedbackManager.Instance?.ClearHover();
        VisualFeedbackManager.Instance?.ClearSelection();

        if (oldMode == Mode.Optimize && newMode != Mode.Optimize)
        {
            optimizeVisualizer?.HideHints();
            optimizeVisualizer?.ClearRollbackPreview();
            currentOptimizeTool = OptimizeTool.Move;
        }

        if (newMode == Mode.Optimize)
        {
            // Clear any leftover displacement ghosts from Build/Analyze; force colors
            // will be reapplied by OptimizeVisualizer.RefreshHints via ApplyForceColors.
            structuralAnalyzer?.ClearVisuals();
            optimizeVisualizer?.ShowHints();
            // Refresh graph with existing history (Move tool → no cursor shown)
            if (optimizeSession != null)
                modeDisplayUI?.UpdateGraph(optimizeSession.History, optimizeSession.CursorIndex, optimizeSession.BestIndex, false);
        }
        else
        {
            structuralAnalyzer?.ClearVisuals();
        }
        // Results text panel is never shown — analysis info is in the HUD only
        if (structuralAnalyzer?.resultsDisplay != null)
            structuralAnalyzer.resultsDisplay.gameObject.SetActive(false);

        unifiedTutorial?.ShowForMode(currentMode);
        UpdateModeText();
    }

    // ------------------------------------------------------------------ //
    // Tool cycling — Right thumbstick X (only when NOT selecting mode)
    // ------------------------------------------------------------------ //

    void HandleToolSwitch()
    {
        Vector2 rightThumb  = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.RTouch);
        Vector2 leftThumb   = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.LTouch);
        bool    leftThumbDown = OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.LTouch);

        if (Time.time - lastThumbTime < ThumbCooldown) { return; }

        // Right thumbstick X: cycle tools (left/right)
        if (rightThumb.x > 0.7f)
        {
            if      (currentMode == Mode.Build)    { CycleBuildTool(1);    lastThumbTime = Time.time; }
            else if (currentMode == Mode.Setup)    { CycleSetupTool(1);    lastThumbTime = Time.time; }
            else if (currentMode == Mode.Optimize) { CycleOptimizeTool(1); lastThumbTime = Time.time; }
        }
        else if (rightThumb.x < -0.7f)
        {
            if      (currentMode == Mode.Build)    { CycleBuildTool(-1);    lastThumbTime = Time.time; }
            else if (currentMode == Mode.Setup)    { CycleSetupTool(-1);    lastThumbTime = Time.time; }
            else if (currentMode == Mode.Optimize) { CycleOptimizeTool(-1); lastThumbTime = Time.time; }
        }

        // Left thumbstick Y: displacement exaggeration in Build/Analyze tool
        if (currentMode == Mode.Build && currentBuildTool == BuildTool.Analyze && structuralAnalyzer != null)
        {
            if (leftThumb.y > 0.7f)
            {
                structuralAnalyzer.exaggerationFactor *= 1.1f;
                structuralAnalyzer.RefreshDisplacements();
                lastThumbTime = Time.time;
            }
            else if (leftThumb.y < -0.7f)
            {
                structuralAnalyzer.exaggerationFactor /= 1.1f;
                structuralAnalyzer.RefreshDisplacements();
                lastThumbTime = Time.time;
            }
        }

        // Left thumbstick X: compliance history cursor — only in Optimize / Rollback tool
        // Right = newer (forward in history), Left = older (backward)
        if (currentMode == Mode.Optimize && currentOptimizeTool == OptimizeTool.Rollback && optimizeSession != null)
        {
            if (leftThumb.x > 0.7f)
            {
                optimizeSession.MoveCursor(1);
                lastThumbTime = Time.time;
                optimizeVisualizer?.ShowRollbackPreview(optimizeSession.GetCursorSnapshot());
                modeDisplayUI?.UpdateGraph(optimizeSession.History, optimizeSession.CursorIndex, optimizeSession.BestIndex, true);
                UpdateModeText();
            }
            else if (leftThumb.x < -0.7f)
            {
                optimizeSession.MoveCursor(-1);
                lastThumbTime = Time.time;
                optimizeVisualizer?.ShowRollbackPreview(optimizeSession.GetCursorSnapshot());
                modeDisplayUI?.UpdateGraph(optimizeSession.History, optimizeSession.CursorIndex, optimizeSession.BestIndex, true);
                UpdateModeText();
            }
        }

        // Setup.Grid: left thumbstick adjusts axis/size
        if (currentMode == Mode.Setup && currentSetupTool == SetupTool.Grid && gridRenderer != null)
        {
            if      (leftThumb.x >  0.7f) { CycleGridAxis(1);   lastThumbTime = Time.time; }
            else if (leftThumb.x < -0.7f) { CycleGridAxis(-1);  lastThumbTime = Time.time; }
            else if (leftThumb.y >  0.7f) { AdjustGridSize(1);  lastThumbTime = Time.time; }
            else if (leftThumb.y < -0.7f) { AdjustGridSize(-1); lastThumbTime = Time.time; }
        }

        // Left thumbstick press: toggle grid snap
        if (leftThumbDown && gridRenderer != null && gridRenderer.isGridSet)
        {
            gridRenderer.isSnapEnabled = !gridRenderer.isSnapEnabled;
            if  ( gridRenderer.isSnapEnabled && !gridRenderer.isActive) { gridRenderer.ShowGrid(); }
            else if (!gridRenderer.isSnapEnabled &&  gridRenderer.isActive) { gridRenderer.HideGrid(); }
            HapticFeedback.Trigger(HapticFeedback.HapticType.Light);
        }
    }

    void CycleOptimizeTool(int dir)
    {
        OptimizeTool prev = currentOptimizeTool;
        int count = System.Enum.GetValues(typeof(OptimizeTool)).Length;
        currentOptimizeTool = (OptimizeTool)(((int)currentOptimizeTool + dir + count) % count);

        // Leaving Rollback: clear ghost preview, redraw graph without cursor
        if (prev == OptimizeTool.Rollback && currentOptimizeTool != OptimizeTool.Rollback)
        {
            optimizeVisualizer?.ClearRollbackPreview();
            if (optimizeSession != null)
                modeDisplayUI?.UpdateGraph(optimizeSession.History, optimizeSession.CursorIndex, optimizeSession.BestIndex, false);
        }

        // Entering Rollback: immediately show current cursor in graph
        if (prev != OptimizeTool.Rollback && currentOptimizeTool == OptimizeTool.Rollback)
        {
            optimizeVisualizer?.ShowRollbackPreview(optimizeSession?.GetCursorSnapshot());
            if (optimizeSession != null)
                modeDisplayUI?.UpdateGraph(optimizeSession.History, optimizeSession.CursorIndex, optimizeSession.BestIndex, true);
        }

        HapticFeedback.Trigger(HapticFeedback.HapticType.Light);
        UpdateModeText();
    }

    void CycleBuildTool(int dir)
    {
        BuildTool prev = currentBuildTool;
        int count = System.Enum.GetValues(typeof(BuildTool)).Length;
        currentBuildTool = (BuildTool)(((int)currentBuildTool + dir + count) % count);
        ClearTempVisuals();
        ClearSelection();

        // Hide analysis visuals when leaving Analyze tool
        if (prev == BuildTool.Analyze && currentBuildTool != BuildTool.Analyze)
        {
            structuralAnalyzer?.ClearVisuals();
            if (structuralAnalyzer != null)
                structuralAnalyzer.resultsDisplay.gameObject.SetActive(false);
        }

        HapticFeedback.Trigger(HapticFeedback.HapticType.Light);
        UpdateModeText();
    }

    void CycleSetupTool(int dir)
    {
        int count = System.Enum.GetValues(typeof(SetupTool)).Length;
        currentSetupTool = (SetupTool)(((int)currentSetupTool + dir + count) % count);
        UpdatePreviewVisibility();
        HapticFeedback.Trigger(HapticFeedback.HapticType.Light);
        UpdateModeText();
    }

    void CycleGridAxis(int dir)
    {
        int count = System.Enum.GetValues(typeof(GridAxis)).Length;
        currentGridAxis = (GridAxis)(((int)currentGridAxis + dir + count) % count);
        UpdateModeText();
    }

    void AdjustGridSize(int dir)
    {
        if      (currentGridAxis == GridAxis.Spacing) { gridRenderer.spacing = dir > 0 ? gridRenderer.spacing + 0.005f : Mathf.Max(0.01f, gridRenderer.spacing - 0.01f); }
        else if (currentGridAxis == GridAxis.X)       { gridRenderer.size.x = Mathf.Max(1, gridRenderer.size.x + dir); }
        else if (currentGridAxis == GridAxis.Y)       { gridRenderer.size.y = Mathf.Max(1, gridRenderer.size.y + dir); }
        else if (currentGridAxis == GridAxis.Z)       { gridRenderer.size.z = Mathf.Max(1, gridRenderer.size.z + dir); }
        gridRenderer.RefreshGrid();
    }

    // ------------------------------------------------------------------ //
    // UI — mode text
    // ------------------------------------------------------------------ //

    void UpdateModeText()
    {
        if (modeText != null)
        {
            modeText.text = $"Mode: {currentMode}";
            if (currentMode == Mode.Build)   { modeText.text += $" / {currentBuildTool}"; }
            if (currentMode == Mode.Setup)   { modeText.text += $" / {currentSetupTool}"; }
            if (currentMode == Mode.Optimize) { modeText.text += $" / {currentOptimizeTool}"; }
        }

        modeDisplayUI?.UpdateModeDisplay(
            currentMode, currentBuildTool, currentSetupTool,
            currentGridAxis, gridRenderer != null ? gridRenderer.spacing : 0.1f,
            currentOptimizeTool, _selectingMode, _modeCursor);
    }

    // ------------------------------------------------------------------ //
    // Public API
    // ------------------------------------------------------------------ //

    public void ConfirmAnalysis()
    {
        structuralAnalyzer?.PerformAnalysis();
        UpdateModeText();
    }

    public void CancelAnalysis()
    {
        UpdateModeText();
    }

    /// <summary>
    /// Called when the entire structure is wiped (network reset, load, etc.).
    /// Resets optimize history and clears the compliance graph.
    /// </summary>
    public void OnStructureReset()
    {
        optimizeSession?.ResetHistory();
        optimizeVisualizer?.HideHints();
        modeDisplayUI?.UpdateGraph(null, -1, -1, false);
    }
}
