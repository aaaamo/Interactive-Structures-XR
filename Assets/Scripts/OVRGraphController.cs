
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
    }

    void Update()
    {
        if (graphManager == null || markerTransform == null) return;

        HandleModeSwitch();
        HandleTriggerInput();
        UpdateTemporaryEdge();
        UpdateTemporaryLoad();
    }

    #region Mode Switching
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
        // Get the number of enum values dynamically
        int numModes = System.Enum.GetNames(typeof(Mode)).Length;

        int next = ((int)currentMode + dir + numModes) % numModes;
        currentMode = (Mode)next;

        UpdateModeText();

        // Cancel temporary edge if switching mode
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

    }


    void UpdateModeText()
    {
        if (modeText != null)
            modeText.text = "Mode: " + currentMode.ToString();
    }
    #endregion

    #region Trigger Handling
    void HandleTriggerInput()
    {
        bool triggerPressed = OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch);

        if (triggerPressed && !triggerHeldLastFrame)
            OnTriggerPressed();

        triggerHeldLastFrame = triggerPressed;
    }
    #endregion

    #region Actions
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
                {
                    nodeToToggle.ToggleSupport();
                }
                break;

            case Mode.Move:
                var nodeToMove = GetNodeAtMarker();
                if (nodeToMove != null)
                {
                    StartCoroutine(MoveNodeCoroutine(nodeToMove));
                }
                else
                {
                    var edgeToMove = GetEdgeAtMarker();
                    if (edgeToMove != null)
                    {
                        StartCoroutine(MoveEdgeCoroutine(edgeToMove));
                    }
                }
                break;

            case Mode.Delete:
                var nodeToDelete = GetNodeAtMarker();
                if (nodeToDelete != null)
                {
                    DeleteNode(nodeToDelete);
                }
                else
                {
                    var edgeToDelete = GetEdgeAtMarker();
                    if (edgeToDelete != null)
                    {
                        DeleteEdge(edgeToDelete);
                    }
                }
                break;

            case Mode.Grab:
                NodeBehaviour nodeToGrab = GetNodeAtMarker();
                if (nodeToGrab != null)
                {
                    StartCoroutine(GrabStructureCoroutine(nodeToGrab));
                }
                else
                {
                    EdgeBehaviour edgeToGrab = GetEdgeAtMarker();
                    if (edgeToGrab != null)
                    {
                        // Pick one node from the edge to start BFS
                        NodeBehaviour startNode = edgeToGrab.nodeA ?? edgeToGrab.nodeB;
                        StartCoroutine(GrabStructureCoroutine(startNode));
                    }
                }
                break;
        }
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
            // Check if an edge already exists between firstSelectedNode and node
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
                // Complete the edge
                tempEdge.nodeB = node;

                // Add to nodes' connectedEdges
                if (firstSelectedNode.connectedEdges == null)
                    firstSelectedNode.connectedEdges = new System.Collections.Generic.List<EdgeBehaviour>();
                firstSelectedNode.connectedEdges.Add(tempEdge);

                if (node.connectedEdges == null)
                    node.connectedEdges = new System.Collections.Generic.List<EdgeBehaviour>();
                node.connectedEdges.Add(tempEdge);

                tempEdge.UpdateEdgePosition();
            }
            else
            {
                // Edge already exists, remove tempEdge preview
                graphManager.RemoveEdge(tempEdge);
            }

            // Reset for next edge
            firstSelectedNode = null;
            tempEdge = null;
        }
    }

    void HandleAddLoad()
    {
        if (firstLoadNode == null)
        {
            // STEP 1: select starting node
            NodeBehaviour node = GetNodeAtMarker();
            if (node == null) return;

            firstLoadNode = node;

            tempLoad = graphManager.CreateLoad(firstLoadNode, Vector3.down, 1f);
        }
        else
        {
            // STEP 2: finalize load
            Vector3 dir = markerTransform.position - firstLoadNode.transform.position;
            float mag = dir.magnitude;

            tempLoad.SetDirection(dir.normalized);
            tempLoad.SetMagnitude(mag);

            firstLoadNode.loads.Add(tempLoad);

            // Reset for next load
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

            // Compute distance from marker to the edge segment
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

    // Helper: find closest point on line segment
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

        // Compute initial offsets from marker to nodes
        Vector3 offsetA = edge.nodeA.transform.position - markerTransform.position;
        Vector3 offsetB = edge.nodeB.transform.position - markerTransform.position;

        while (OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
        {
            // Move both nodes with marker maintaining initial offset
            edge.nodeA.transform.position = markerTransform.position + offsetA;
            edge.nodeB.transform.position = markerTransform.position + offsetB;

            // Update all edges connected to the nodes
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

    IEnumerator GrabStructureCoroutine(NodeBehaviour startNode)
    {
        if (startNode == null) yield break;

        // BFS to collect all connected nodes
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

        // Compute offsets relative to marker
        Dictionary<NodeBehaviour, Vector3> offsets = new Dictionary<NodeBehaviour, Vector3>();
        foreach (var node in connectedNodes)
            offsets[node] = node.transform.position - markerTransform.position;

        // Move structure while trigger held
        while (OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
        {
            foreach (var node in connectedNodes)
                node.transform.position = markerTransform.position + offsets[node];

            // Update all edges
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

        // Remove loads
        if (node.loads != null)
        {
            foreach (var load in node.loads)
            {
                if (load != null)
                    Destroy(load.gameObject);
            }
        }

        // Remove all connected edges
        if (node.connectedEdges != null)
        {
            foreach (var edge in node.connectedEdges)
            {
                if (edge != null)
                {
                    // Remove edge from the other node's list
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

        // Remove from connected nodes
        if (edge.nodeA != null)
            edge.nodeA.connectedEdges?.Remove(edge);
        if (edge.nodeB != null)
            edge.nodeB.connectedEdges?.Remove(edge);

        graphManager.RemoveEdge(edge);
    }


    #endregion
}
