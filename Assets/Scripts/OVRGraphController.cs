using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class OVRGraphController : MonoBehaviour
{
    public enum Mode { AddNode, AddEdge, AddLoad, ToggleSupport, Move, Delete, Grab, Analyze }
    public Mode currentMode = Mode.AddNode;

    [Header("References")]
    public GraphManager graphManager;
    public Transform markerTransform;
    public TextMeshPro modeText;
    public StructuralAnalyzer structuralAnalyzer;
    public GameObject analyzeConfirmPanel;

    private NodeBehaviour firstSelectedNode;
    private EdgeBehaviour tempEdge;
    private NodeBehaviour firstLoadNode = null;
    private LoadBehaviour tempLoad = null;
    private bool triggerHeldLastFrame = false;
    private float lastThumbTime = 0f;
    private float thumbCooldown = 0.3f;

    void Start()
    {
        UpdateModeText();
        if (analyzeConfirmPanel != null)
            analyzeConfirmPanel.SetActive(false);
        if (currentMode == Mode.Analyze)
        {
            structuralAnalyzer?.resultsDisplay.gameObject.SetActive(true);
        }
        else
        {
            structuralAnalyzer?.resultsDisplay.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (graphManager == null || markerTransform == null) return;
        HandleModeSwitch();
        HandleTriggerInput();
        UpdateTemporaryEdge();
        UpdateTemporaryLoad();
    }

    void HandleModeSwitch()
    {
        Vector2 thumbAxis = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.RTouch);
        if (Time.time - lastThumbTime < thumbCooldown) return;

        if (thumbAxis.x > 0.7f)
        {
            CycleMode(1);
            lastThumbTime = Time.time;
        }
        else if (thumbAxis.x < -0.7f)
        {
            CycleMode(-1);
            lastThumbTime = Time.time;
        }
    }

    void CycleMode(int dir)
    {
        int numModes = System.Enum.GetNames(typeof(Mode)).Length;
        int next = ((int)currentMode + dir + numModes) % numModes;
        currentMode = (Mode)next;
        UpdateModeText();

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

        // Show confirmation when entering Analyze mode
        if (currentMode == Mode.Analyze)
        {
            if (analyzeConfirmPanel != null)
            {
                analyzeConfirmPanel.SetActive(true);
            }
            else
            {
                ShowAnalyzePrompt(); // Fallback if no UI panel assigned
            }
            structuralAnalyzer?.resultsDisplay.gameObject.SetActive(true);
        }
        else
        {
            if (analyzeConfirmPanel != null)
            {
                analyzeConfirmPanel.SetActive(false);
            }
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
        if (modeText != null)
            modeText.text = "Mode: " + currentMode.ToString();
    }

    void HandleTriggerInput()
    {
        bool triggerPressed = OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch);
        if (triggerPressed && !triggerHeldLastFrame)
            OnTriggerPressed();
        triggerHeldLastFrame = triggerPressed;

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
                graphManager.CreateNode(markerTransform.position);
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
                    nodeToToggle.ToggleSupport();
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
                    break;
                }
                var edgeToDelete = GetEdgeAtMarker();
                if (edgeToDelete != null)
                {
                    DeleteEdge(edgeToDelete);
                    break;
                }
                var loadToDelete = GetLoadAtMarker();
                if (loadToDelete != null)
                {
                    if (loadToDelete.node != null)
                        loadToDelete.node.loads.Remove(loadToDelete);
                    Destroy(loadToDelete.gameObject);
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
        }
    }

    public void ConfirmAnalysis()
    {
        if (structuralAnalyzer != null)
        {
            Debug.LogWarning("Analysis started!");
            structuralAnalyzer.PerformAnalysis();
        }
        if (analyzeConfirmPanel != null)
            analyzeConfirmPanel.SetActive(false);

        // Reset mode text
        UpdateModeText();
    }

    public void CancelAnalysis()
    {
        if (analyzeConfirmPanel != null)
            analyzeConfirmPanel.SetActive(false);
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
            }
            else
            {
                graphManager.RemoveEdge(tempEdge);
            }
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
        }
        else
        {
            Vector3 dir = markerTransform.position - firstLoadNode.transform.position;
            float mag = dir.magnitude;
            tempLoad.SetDirection(dir.normalized);
            tempLoad.SetMagnitude(mag);
            firstLoadNode.loads.Add(tempLoad);
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
            node.transform.position = markerTransform.position + offset;
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
}