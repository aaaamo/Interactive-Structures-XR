using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class OVRGraphController : MonoBehaviour
{
    public enum Mode { AddNode, AddEdge, AddLoad, ToggleSupport, Move, Delete, Grab, Analyze, Grid }
    public Mode currentMode = Mode.AddNode;
    public enum GridAxis { X, Y, Z, Spacing }
    public GridAxis currentGridAxis = GridAxis.X;

    [Header("References")]
    public GraphManager graphManager;
    public Transform markerTransform;
    public TextMeshPro modeText;
    public StructuralAnalyzer structuralAnalyzer;
    public GridPointRenderer gridRenderer;
    public RaycastSurfaceFinder surfaceFinder;

    [Header("Visual Feedback")]
    public MarkerController markerController;
    public GameObject ghostNodePrefab;
    public Material ghostMaterial;
    public ModeDisplayUI modeDisplayUI;
    public TutorialTooltipSystem tutorialSystem;
    public ModeTutorialPanel tutorialPanel;
    public ContextualTutorialPanel contextualTutorial;

    private NodeBehaviour firstSelectedNode;
    private EdgeBehaviour tempEdge;
    private NodeBehaviour firstLoadNode = null;
    private LoadBehaviour tempLoad = null;
    private bool triggerHeldLastFrame = false;
    private float lastThumbTime = 0f;
    private float thumbCooldown = 0.3f;

    // Visual feedback tracking
    private GameObject ghostNode;
    private LineRenderer ghostEdgeLine;
    private GameObject hoveredObject;
    private HashSet<NodeBehaviour> highlightedGrabNodes = new HashSet<NodeBehaviour>();

    void Start()
    {
        UpdateModeText();
        if (currentMode == Mode.Analyze)
        {
            structuralAnalyzer?.resultsDisplay.gameObject.SetActive(true);
        }
        else
        {
            structuralAnalyzer?.resultsDisplay.gameObject.SetActive(false);
        }
        gridRenderer.ShowGrid();

        // Initialize marker controller
        if (markerController == null && markerTransform != null)
        {
            markerController = markerTransform.GetComponent<MarkerController>();
            if (markerController == null)
                markerController = markerTransform.gameObject.AddComponent<MarkerController>();
        }
        markerController?.SetModeColor(currentMode);

        // Setup ghost edge line renderer
        SetupGhostEdgeLine();
    }

    void Update()
    {
        if (graphManager == null || markerTransform == null) return;
        HandleModeSwitch();
        HandleTriggerInput();
        UpdateTemporaryEdge();
        UpdateTemporaryLoad();
        UpdateVisualFeedback();
    }

    void HandleModeSwitch()
    {
        Vector2 rightThumbAxis = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.RTouch);
        Vector2 leftThumbAxis = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.LTouch);
        bool leftThumbDown = OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.LTouch);

        if (Time.time - lastThumbTime < thumbCooldown) return;

        if (rightThumbAxis.x > 0.7f)
        {
            CycleMode(1);
            lastThumbTime = Time.time;
        }
        else if (rightThumbAxis.x < -0.7f)
        {
            CycleMode(-1);
            lastThumbTime = Time.time;
        }

        if (currentMode == Mode.Analyze)
        {
            if (leftThumbAxis.y > 0.7f)
            {
                structuralAnalyzer.exaggerationFactor *= 1.1f;
                structuralAnalyzer.RefreshDisplacements();
            }
            else if (leftThumbAxis.y < -0.7f)
            {
                structuralAnalyzer.exaggerationFactor /= 1.1f;
                structuralAnalyzer.RefreshDisplacements();
            }
        }

        if (currentMode == Mode.Grid && gridRenderer.isActive)
        {
            if (leftThumbDown)
            {
                gridRenderer.ToggleGrid();
                return;
            }
            if (leftThumbAxis.x > 0.7f)
            {
                CycleGridAxis(1);
                lastThumbTime = Time.time;
            }
            else if (leftThumbAxis.x < -0.7f)
            {
                CycleGridAxis(-1);
                lastThumbTime = Time.time;
            } 
            else if (leftThumbAxis.y > 0.7f)
            {
                if (currentGridAxis == GridAxis.Spacing)
                {
                    gridRenderer.spacing += 0.005f;
                }
                else if (currentGridAxis == GridAxis.X)
                {
                    gridRenderer.size.x += 1;
                }
                else if (currentGridAxis == GridAxis.Y)
                {
                    gridRenderer.size.y += 1;
                }
                else if (currentGridAxis == GridAxis.Z)
                {
                    gridRenderer.size.z += 1;
                }
                lastThumbTime = Time.time;
                gridRenderer.RefreshGrid();
            }
            else if (leftThumbAxis.y < -0.7f)
            {
                if (currentGridAxis == GridAxis.Spacing)
                {
                    gridRenderer.spacing = Mathf.Max(0.01f, gridRenderer.spacing - 0.01f);
                } 
                else if (currentGridAxis == GridAxis.X)
                {
                    gridRenderer.size.x = Mathf.Max(1, gridRenderer.size.x - 1);
                }
                else if (currentGridAxis == GridAxis.Y)
                {
                    gridRenderer.size.y = Mathf.Max(1, gridRenderer.size.y - 1);
                }
                else if (currentGridAxis == GridAxis.Z)
                {
                    gridRenderer.size.z = Mathf.Max(1, gridRenderer.size.z - 1);
                }
                lastThumbTime = Time.time;
                gridRenderer.RefreshGrid();
            }
        }
    }

    void CycleGridAxis(int dir)
    {
        int numAxes = System.Enum.GetNames(typeof(GridAxis)).Length;
        int next = ((int)currentGridAxis + dir + numAxes) % numAxes;
        currentGridAxis = (GridAxis)next;
        UpdateModeText();
    }

    void CycleMode(int dir)
    {
        int numModes = System.Enum.GetNames(typeof(Mode)).Length;
        int next = ((int)currentMode + dir + numModes) % numModes;
        currentMode = (Mode)next;
        UpdateModeText();

        // Haptic feedback for mode change
        HapticFeedback.Trigger(HapticFeedback.HapticType.Medium);

        // Update marker color
        markerController?.SetModeColor(currentMode);

        // Cleanup temporary objects
        CleanupGhostObjects();
        if (tempEdge != null)
        {
            graphManager.RemoveEdge(tempEdge);
            tempEdge = null;
            firstSelectedNode = null;
        }
        if (tempLoad != null)
        {
            Destroy(tempLoad.gameObject);
            tempLoad = null;
            firstLoadNode = null;
        }

        // Clear visual feedback
        VisualFeedbackManager.Instance?.ClearHover();
        VisualFeedbackManager.Instance?.ClearSelection();

        // Update tutorial panels for new mode
        tutorialPanel?.UpdateForMode(currentMode);
        contextualTutorial?.ShowForMode(currentMode);

        // Show confirmation when entering Analyze mode
        if (currentMode == Mode.Analyze)
        {
            ShowAnalyzePrompt();
            structuralAnalyzer?.resultsDisplay.gameObject.SetActive(true);
        }
        else
        {
            structuralAnalyzer?.resultsDisplay.gameObject.SetActive(false);
        }
    }

    void ShowAnalyzePrompt()
    {
        // Simple text prompt if no UI panel is set up
        if (modeText != null)
            modeText.text = "Mode: Analyze\nPress TRIGGER to run analysis\nPress GRIP to cancel";
    }

    void UpdateModeText()
    {
        // Update old text display (backward compatibility)
        if (modeText != null)
        {
            modeText.text = "Mode: " + currentMode.ToString();
            if (currentMode == Mode.Grid)
            {
                modeText.text += "\nGrid Axis: " + currentGridAxis.ToString();
                if (currentGridAxis == GridAxis.Spacing)
                {
                    modeText.text += $" = {gridRenderer.spacing:F3}";
                }
            }
        }

        // Update new mode display UI
        if (modeDisplayUI != null)
        {
            modeDisplayUI.UpdateModeDisplay(currentMode, currentGridAxis, gridRenderer != null ? gridRenderer.spacing : 0.1f);
        }

        // Show tutorial tooltip for first-time mode usage
        tutorialSystem?.ShowModeTooltip(currentMode);
    }

    void HandleTriggerInput()
    {
        bool rightTriggerPressed = OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch);
        if (rightTriggerPressed && !triggerHeldLastFrame)
            OnTriggerPressed();
        triggerHeldLastFrame = rightTriggerPressed;

        // Also check for grip button in Analyze mode (cancel)
        if (currentMode == Mode.Analyze)
        {
            if (OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch))
            {
                CancelAnalysis();
            }
        }
    }

    void OnTriggerPressed()
    {
        switch (currentMode)
        {
            case Mode.AddNode:
                graphManager.CreateNode(GetGridPoint(markerTransform.position));
                HapticFeedback.Trigger(HapticFeedback.HapticType.Medium);
                markerController?.Pulse();
                contextualTutorial?.OnActionPerformed(Mode.AddNode);
                break;
            case Mode.AddEdge:
                HandleAddEdge();
                break;
            case Mode.AddLoad:
                HandleAddLoad();
                break;
            case Mode.ToggleSupport:
                var nodeToToggle = GetNodeAtMarker();
                if (nodeToToggle != null)
                {
                    nodeToToggle.ToggleSupport();
                    HapticFeedback.Trigger(HapticFeedback.HapticType.Medium);
                    contextualTutorial?.OnActionPerformed(Mode.ToggleSupport);
                }
                break;
            case Mode.Move:
                var nodeToMove = GetNodeAtMarker();
                if (nodeToMove != null)
                {
                    StartCoroutine(MoveNodeCoroutine(nodeToMove));
                    break;
                }
                var edgeToMove = GetEdgeAtMarker();
                if (edgeToMove != null)
                {
                    StartCoroutine(MoveEdgeCoroutine(edgeToMove));
                    break;
                }
                var loadToMove = GetLoadAtMarker();
                if (loadToMove != null)
                {
                    StartCoroutine(MoveLoadCoroutine(loadToMove));
                    break;
                }
                break;
            case Mode.Delete:
                var nodeToDelete = GetNodeAtMarker();
                if (nodeToDelete != null)
                {
                    DeleteNode(nodeToDelete);
                    HapticFeedback.Trigger(HapticFeedback.HapticType.Strong);
                    break;
                }
                var edgeToDelete = GetEdgeAtMarker();
                if (edgeToDelete != null)
                {
                    DeleteEdge(edgeToDelete);
                    HapticFeedback.Trigger(HapticFeedback.HapticType.Strong);
                    break;
                }
                var loadToDelete = GetLoadAtMarker();
                if (loadToDelete != null)
                {
                    if (loadToDelete.node != null)
                        loadToDelete.node.loads.Remove(loadToDelete);
                    Destroy(loadToDelete.gameObject);
                    HapticFeedback.Trigger(HapticFeedback.HapticType.Strong);
                }
                break;
            case Mode.Grab:
                NodeBehaviour nodeToGrab = GetNodeAtMarker();
                if (nodeToGrab != null)
                    StartCoroutine(GrabStructureCoroutine(nodeToGrab));
                else
                {
                    EdgeBehaviour edgeToGrab = GetEdgeAtMarker();
                    if (edgeToGrab != null)
                    {
                        NodeBehaviour startNode = edgeToGrab.nodeA ?? edgeToGrab.nodeB;
                        StartCoroutine(GrabStructureCoroutine(startNode));
                    }
                }
                break;
            case Mode.Analyze:
                // Trigger confirms analysis
                if (structuralAnalyzer != null)
                {

                    ConfirmAnalysis();
                }
                break;
            case Mode.Grid:
                if (surfaceFinder != null)
                {
                    surfaceFinder.SetAnchor();
                }
                break;
        }
    }

    public void ConfirmAnalysis()
    {
        if (structuralAnalyzer != null)
        {
            Debug.LogWarning("Analysis started!");
            structuralAnalyzer.PerformAnalysis();
        }
        // Reset mode text
        UpdateModeText();
    }

    public void CancelAnalysis()
    {
        Debug.LogWarning("Analysis cancelled");

        // Reset mode text
        UpdateModeText();
    }

    void HandleAddEdge()
    {
        NodeBehaviour node = GetNodeAtMarker();
        if (node == null) return;

        if (firstSelectedNode == null)
        {
            firstSelectedNode = node;
            tempEdge = graphManager.CreateEdge(firstSelectedNode);
            HapticFeedback.Trigger(HapticFeedback.HapticType.Medium);
            VisualFeedbackManager.Instance?.SetSelected(node.gameObject, new Color(0.2f, 1f, 0.2f, 1f));
        }
        else if (node != firstSelectedNode)
        {
            bool edgeExists = false;
            if (firstSelectedNode.connectedEdges != null)
            {
                foreach (var e in firstSelectedNode.connectedEdges)
                {
                    if (e == null) continue;
                    if ((e.nodeA == firstSelectedNode && e.nodeB == node) ||
                        (e.nodeB == firstSelectedNode && e.nodeA == node))
                    {
                        edgeExists = true;
                        break;
                    }
                }
            }

            if (!edgeExists)
            {
                tempEdge.nodeB = node;
                if (firstSelectedNode.connectedEdges == null)
                    firstSelectedNode.connectedEdges = new List<EdgeBehaviour>();
                firstSelectedNode.connectedEdges.Add(tempEdge);
                if (node.connectedEdges == null)
                    node.connectedEdges = new List<EdgeBehaviour>();
                node.connectedEdges.Add(tempEdge);
                tempEdge.UpdateEdgePosition();
                HapticFeedback.Trigger(HapticFeedback.HapticType.Success);
            }
            else
            {
                graphManager.RemoveEdge(tempEdge);
                HapticFeedback.Trigger(HapticFeedback.HapticType.Error);
                modeDisplayUI?.ShowMessage("Edge already exists between these nodes!", new Color(1f, 0.3f, 0.3f));
                StartCoroutine(ClearMessageAfterDelay(2f));
            }
            VisualFeedbackManager.Instance?.ClearSelection();
            firstSelectedNode = null;
            tempEdge = null;
        }
    }

    void HandleAddLoad()
    {
        if (firstLoadNode == null)
        {
            NodeBehaviour node = GetNodeAtMarker();
            if (node == null) return;
            firstLoadNode = node;
            tempLoad = graphManager.CreateLoad(firstLoadNode, Vector3.down, 1f);
            HapticFeedback.Trigger(HapticFeedback.HapticType.Medium);
            VisualFeedbackManager.Instance?.SetSelected(node.gameObject, new Color(1f, 0.6f, 0.2f, 1f));
        }
        else
        {
            Vector3 dir = markerTransform.position - firstLoadNode.transform.position;
            float mag = dir.magnitude;
            tempLoad.SetDirection(dir.normalized);
            tempLoad.SetMagnitude(mag);
            firstLoadNode.loads.Add(tempLoad);
            HapticFeedback.Trigger(HapticFeedback.HapticType.Success);
            VisualFeedbackManager.Instance?.ClearSelection();
            firstLoadNode = null;
            tempLoad = null;
        }
    }

    void UpdateTemporaryEdge()
    {
        if (tempEdge != null && firstSelectedNode != null && markerTransform != null)
            tempEdge.UpdateEdgePosition(markerTransform.position);
    }

    void UpdateTemporaryLoad()
    {
        if (firstLoadNode == null || tempLoad == null) return;
        Vector3 dir = markerTransform.position - firstLoadNode.transform.position;
        float mag = dir.magnitude;
        tempLoad.SetDirection(dir.normalized);
        tempLoad.SetMagnitude(mag);
    }

    Vector3 GetGridPoint(Vector3 pos)
    {
        if (gridRenderer == null) return pos;
        return gridRenderer.GetClosestGridPoint(pos);
    }

    NodeBehaviour GetNodeAtMarker()
    {
        NodeBehaviour[] nodes = FindObjectsOfType<NodeBehaviour>();
        NodeBehaviour closest = null;
        float minDist = 0.03f;
        foreach (var node in nodes)
        {
            if (node == null) continue;
            float dist = Vector3.Distance(node.transform.position, markerTransform.position);
            if (dist < minDist)
            {
                closest = node;
                break;
            }
        }
        return closest;
    }

    EdgeBehaviour GetEdgeAtMarker()
    {
        EdgeBehaviour[] edges = FindObjectsOfType<EdgeBehaviour>();
        EdgeBehaviour closest = null;
        float minDist = 0.02f;
        foreach (var edge in edges)
        {
            if (edge == null || edge.nodeA == null || edge.nodeB == null) continue;
            Vector3 closestPoint = ClosestPointOnSegment(edge.nodeA.transform.position, edge.nodeB.transform.position, markerTransform.position);
            float dist = Vector3.Distance(markerTransform.position, closestPoint);
            if (dist < minDist)
            {
                closest = edge;
                break;
            }
        }
        return closest;
    }

    LoadBehaviour GetLoadAtMarker()
    {
        LoadBehaviour[] loads = FindObjectsOfType<LoadBehaviour>();
        LoadBehaviour closest = null;
        float minDist = 0.03f;
        foreach (var load in loads)
        {
            if (load == null) continue;
            float dist = Vector3.Distance(load.EndPoint(), markerTransform.position);
            if (dist < minDist)
            {
                closest = load;
                break;
            }
        }
        return closest;
    }

    Vector3 ClosestPointOnSegment(Vector3 a, Vector3 b, Vector3 point)
    {
        Vector3 ab = b - a;
        float t = Vector3.Dot(point - a, ab) / Vector3.Dot(ab, ab);
        t = Mathf.Clamp01(t);
        return a + t * ab;
    }

    IEnumerator MoveNodeCoroutine(NodeBehaviour node)
    {
        if (node == null) yield break;
        Vector3 offset = node.transform.position - markerTransform.position;
        while (OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
        {
            Vector3 movedPos = GetGridPoint(markerTransform.position + offset);
            node.transform.position = movedPos;
            if (node.connectedEdges != null)
            {
                foreach (var edge in node.connectedEdges)
                {
                    if (edge != null)
                        edge.UpdateEdgePosition();
                }
            }
            yield return null;
        }
    }

    IEnumerator MoveEdgeCoroutine(EdgeBehaviour edge)
    {
        if (edge == null || edge.nodeA == null || edge.nodeB == null) yield break;
        Vector3 offsetA = edge.nodeA.transform.position - markerTransform.position;
        Vector3 offsetB = edge.nodeB.transform.position - markerTransform.position;
        while (OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
        {
            edge.nodeA.transform.position = markerTransform.position + offsetA;
            edge.nodeB.transform.position = markerTransform.position + offsetB;
            foreach (var node in new NodeBehaviour[] { edge.nodeA, edge.nodeB })
            {
                if (node.connectedEdges != null)
                {
                    foreach (var e in node.connectedEdges)
                    {
                        if (e != null)
                            e.UpdateEdgePosition();
                    }
                }
            }
            yield return null;
        }
    }

    IEnumerator MoveLoadCoroutine(LoadBehaviour load)
    {
        if (load == null || load.node == null) yield break;
        Vector3 offset = load.EndPoint() - markerTransform.position;
        while (OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
        {
            Vector3 dir = (markerTransform.position + offset) - load.node.transform.position;
            float mag = dir.magnitude;
            load.SetDirection(dir.normalized);
            load.SetMagnitude(mag);
            yield return null;
        }
    }

    IEnumerator GrabStructureCoroutine(NodeBehaviour startNode)
    {
        if (startNode == null) yield break;
        HashSet<NodeBehaviour> connectedNodes = new HashSet<NodeBehaviour>();
        Queue<NodeBehaviour> queue = new Queue<NodeBehaviour>();
        queue.Enqueue(startNode);
        connectedNodes.Add(startNode);
        while (queue.Count > 0)
        {
            NodeBehaviour node = queue.Dequeue();
            if (node.connectedEdges != null)
            {
                foreach (EdgeBehaviour edge in node.connectedEdges)
                {
                    NodeBehaviour other = edge.nodeA == node ? edge.nodeB : edge.nodeA;
                    if (other != null && !connectedNodes.Contains(other))
                    {
                        connectedNodes.Add(other);
                        queue.Enqueue(other);
                    }
                }
            }
        }
        //Vector3 GridSnapPos = GetGridPoint(markerTransform.position);

        Dictionary<NodeBehaviour, Vector3> offsets = new Dictionary<NodeBehaviour, Vector3>();
        foreach (var node in connectedNodes)
            offsets[node] = node.transform.position - markerTransform.position;
        while (OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
        {
            foreach (var node in connectedNodes)
                node.transform.position = markerTransform.position + offsets[node];
            foreach (var node in connectedNodes)
            {
                if (node.connectedEdges != null)
                {
                    foreach (var edge in node.connectedEdges)
                        edge?.UpdateEdgePosition();
                }
            }
            yield return null;
        }
    }

    void DeleteNode(NodeBehaviour node)
    {
        if (node == null) return;
        if (node.loads != null)
        {
            foreach (var load in node.loads)
            {
                if (load != null)
                    Destroy(load.gameObject);
            }
        }
        if (node.connectedEdges != null)
        {
            foreach (var edge in node.connectedEdges)
            {
                if (edge != null)
                {
                    if (edge.nodeA != null && edge.nodeA != node)
                        edge.nodeA.connectedEdges?.Remove(edge);
                    if (edge.nodeB != null && edge.nodeB != node)
                        edge.nodeB.connectedEdges?.Remove(edge);
                    graphManager.RemoveEdge(edge);
                }
            }
        }
        Destroy(node.gameObject);
    }

    void DeleteEdge(EdgeBehaviour edge)
    {
        if (edge == null) return;
        if (edge.nodeA != null)
            edge.nodeA.connectedEdges?.Remove(edge);
        if (edge.nodeB != null)
            edge.nodeB.connectedEdges?.Remove(edge);
        graphManager.RemoveEdge(edge);
    }

    // ========== VISUAL FEEDBACK METHODS ==========

    void SetupGhostEdgeLine()
    {
        GameObject lineObj = new GameObject("GhostEdgeLine");
        ghostEdgeLine = lineObj.AddComponent<LineRenderer>();
        ghostEdgeLine.startWidth = 0.005f;
        ghostEdgeLine.endWidth = 0.005f;
        ghostEdgeLine.material = new Material(Shader.Find("Sprites/Default"));
        ghostEdgeLine.startColor = new Color(0.2f, 0.6f, 1f, 0.5f);
        ghostEdgeLine.endColor = new Color(0.2f, 0.6f, 1f, 0.5f);
        ghostEdgeLine.enabled = false;
        ghostEdgeLine.positionCount = 2;
    }

    void UpdateVisualFeedback()
    {
        // Clear previous hover
        if (hoveredObject != null)
        {
            VisualFeedbackManager.Instance?.ClearHover();
            hoveredObject = null;
        }

        switch (currentMode)
        {
            case Mode.AddNode:
                UpdateAddNodeFeedback();
                break;
            case Mode.AddEdge:
                UpdateAddEdgeFeedback();
                break;
            case Mode.AddLoad:
                UpdateAddLoadFeedback();
                break;
            case Mode.ToggleSupport:
                UpdateToggleSupportFeedback();
                break;
            case Mode.Move:
                UpdateMoveFeedback();
                break;
            case Mode.Delete:
                UpdateDeleteFeedback();
                break;
            case Mode.Grab:
                UpdateGrabFeedback();
                break;
        }
    }

    void UpdateAddNodeFeedback()
    {
        // Show ghost node at grid snap position
        Vector3 snapPos = GetGridPoint(markerTransform.position);

        if (ghostNode == null && graphManager.nodePrefab != null)
        {
            ghostNode = Instantiate(graphManager.nodePrefab, snapPos, Quaternion.identity);
            ghostNode.name = "GhostNode";

            // Make it semi-transparent
            Renderer[] renderers = ghostNode.GetComponentsInChildren<Renderer>();
            foreach (var rend in renderers)
            {
                Material[] mats = rend.materials;
                for (int i = 0; i < mats.Length; i++)
                {
                    mats[i] = new Material(mats[i]);
                    Color c = mats[i].color;
                    c.a = 0.3f;
                    mats[i].color = c;
                }
                rend.materials = mats;
            }

            // Disable behavior scripts
            var nodeBehaviour = ghostNode.GetComponent<NodeBehaviour>();
            if (nodeBehaviour != null) nodeBehaviour.enabled = false;
        }
        else if (ghostNode != null)
        {
            ghostNode.transform.position = snapPos;
        }
    }

    void UpdateAddEdgeFeedback()
    {
        // Cleanup ghost node if it exists
        if (ghostNode != null)
        {
            Destroy(ghostNode);
            ghostNode = null;
        }

        // Hover highlight on nodes
        NodeBehaviour node = GetNodeAtMarker();
        if (node != null)
        {
            hoveredObject = node.gameObject;
            Color highlightColor = (firstSelectedNode == null) ?
                new Color(0.2f, 1f, 0.2f, 1f) : new Color(0.2f, 0.6f, 1f, 1f);

            if (VisualFeedbackManager.Instance != null)
            {
                VisualFeedbackManager.Instance.HighlightHover(node.gameObject, highlightColor);
            }
        }

        // Show ghost edge line
        if (firstSelectedNode != null && ghostEdgeLine != null)
        {
            ghostEdgeLine.enabled = true;
            ghostEdgeLine.SetPosition(0, firstSelectedNode.transform.position);
            ghostEdgeLine.SetPosition(1, markerTransform.position);

            // Check if edge would be duplicate
            if (node != null && node != firstSelectedNode)
            {
                bool isDuplicate = CheckDuplicateEdge(firstSelectedNode, node);
                ghostEdgeLine.startColor = isDuplicate ? new Color(1f, 0f, 0f, 0.5f) : new Color(0.2f, 1f, 0.2f, 0.5f);
                ghostEdgeLine.endColor = isDuplicate ? new Color(1f, 0f, 0f, 0.5f) : new Color(0.2f, 1f, 0.2f, 0.5f);
            }
        }
        else if (ghostEdgeLine != null)
        {
            ghostEdgeLine.enabled = false;
        }
    }

    bool CheckDuplicateEdge(NodeBehaviour nodeA, NodeBehaviour nodeB)
    {
        if (nodeA.connectedEdges != null)
        {
            foreach (var e in nodeA.connectedEdges)
            {
                if (e == null) continue;
                if ((e.nodeA == nodeA && e.nodeB == nodeB) ||
                    (e.nodeB == nodeA && e.nodeA == nodeB))
                {
                    return true;
                }
            }
        }
        return false;
    }

    void UpdateAddLoadFeedback()
    {
        // Cleanup
        if (ghostNode != null)
        {
            Destroy(ghostNode);
            ghostNode = null;
        }
        if (ghostEdgeLine != null)
        {
            ghostEdgeLine.enabled = false;
        }

        // Hover on nodes
        if (firstLoadNode == null)
        {
            NodeBehaviour node = GetNodeAtMarker();
            if (node != null)
            {
                hoveredObject = node.gameObject;
                VisualFeedbackManager.Instance?.HighlightHover(node.gameObject, new Color(1f, 0.6f, 0.2f, 1f));
            }
        }
    }

    void UpdateToggleSupportFeedback()
    {
        CleanupGhostObjects();

        NodeBehaviour node = GetNodeAtMarker();
        if (node != null)
        {
            hoveredObject = node.gameObject;
            VisualFeedbackManager.Instance?.HighlightHover(node.gameObject, new Color(0.8f, 0.8f, 0.2f, 1f));
        }
    }

    void UpdateMoveFeedback()
    {
        CleanupGhostObjects();

        // Highlight moveable objects
        NodeBehaviour node = GetNodeAtMarker();
        if (node != null)
        {
            hoveredObject = node.gameObject;
            VisualFeedbackManager.Instance?.HighlightHover(node.gameObject, new Color(0.6f, 0.3f, 1f, 1f));
            return;
        }

        EdgeBehaviour edge = GetEdgeAtMarker();
        if (edge != null)
        {
            hoveredObject = edge.gameObject;
            VisualFeedbackManager.Instance?.HighlightHover(edge.gameObject, new Color(0.6f, 0.3f, 1f, 1f));
            return;
        }

        LoadBehaviour load = GetLoadAtMarker();
        if (load != null)
        {
            hoveredObject = load.gameObject;
            VisualFeedbackManager.Instance?.HighlightHover(load.gameObject, new Color(0.6f, 0.3f, 1f, 1f));
        }
    }

    void UpdateDeleteFeedback()
    {
        CleanupGhostObjects();

        // Highlight deleteable objects in red
        NodeBehaviour node = GetNodeAtMarker();
        if (node != null)
        {
            hoveredObject = node.gameObject;
            VisualFeedbackManager.Instance?.HighlightHover(node.gameObject, new Color(1f, 0.2f, 0.2f, 1f));
            return;
        }

        EdgeBehaviour edge = GetEdgeAtMarker();
        if (edge != null)
        {
            hoveredObject = edge.gameObject;
            VisualFeedbackManager.Instance?.HighlightHover(edge.gameObject, new Color(1f, 0.2f, 0.2f, 1f));
            return;
        }

        LoadBehaviour load = GetLoadAtMarker();
        if (load != null)
        {
            hoveredObject = load.gameObject;
            VisualFeedbackManager.Instance?.HighlightHover(load.gameObject, new Color(1f, 0.2f, 0.2f, 1f));
        }
    }

    void UpdateGrabFeedback()
    {
        CleanupGhostObjects();

        // Clear previous grab highlights
        if (highlightedGrabNodes.Count > 0)
        {
            VisualFeedbackManager.Instance?.ClearConnectedHighlight(highlightedGrabNodes);
            highlightedGrabNodes.Clear();
        }

        // Get hovered node or edge
        NodeBehaviour startNode = GetNodeAtMarker();
        if (startNode == null)
        {
            EdgeBehaviour edge = GetEdgeAtMarker();
            if (edge != null)
                startNode = edge.nodeA ?? edge.nodeB;
        }

        if (startNode != null)
        {
            // Find all connected nodes using BFS
            HashSet<NodeBehaviour> connectedNodes = new HashSet<NodeBehaviour>();
            Queue<NodeBehaviour> queue = new Queue<NodeBehaviour>();
            queue.Enqueue(startNode);
            connectedNodes.Add(startNode);

            while (queue.Count > 0)
            {
                NodeBehaviour node = queue.Dequeue();
                if (node.connectedEdges != null)
                {
                    foreach (EdgeBehaviour edge in node.connectedEdges)
                    {
                        NodeBehaviour other = edge.nodeA == node ? edge.nodeB : edge.nodeA;
                        if (other != null && !connectedNodes.Contains(other))
                        {
                            connectedNodes.Add(other);
                            queue.Enqueue(other);
                        }
                    }
                }
            }

            // Highlight all connected nodes
            highlightedGrabNodes = connectedNodes;
            VisualFeedbackManager.Instance?.HighlightConnectedStructure(connectedNodes, new Color(0.3f, 0.9f, 1f, 1f));
        }
    }

    void CleanupGhostObjects()
    {
        if (ghostNode != null)
        {
            Destroy(ghostNode);
            ghostNode = null;
        }
        if (ghostEdgeLine != null)
        {
            ghostEdgeLine.enabled = false;
        }
    }

    IEnumerator ClearMessageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        modeDisplayUI?.ClearMessage(currentMode);
    }
}