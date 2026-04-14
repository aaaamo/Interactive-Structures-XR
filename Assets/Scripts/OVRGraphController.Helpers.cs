using System.Collections;
using UnityEngine;

/// <summary>
/// Helpers partial: proximity detection, grid snapping, temporary-visual
/// management, ghost setup, and preview visibility.
/// </summary>
public partial class OVRGraphController
{
    // ------------------------------------------------------------------ //
    // Proximity detection
    // ------------------------------------------------------------------ //

    NodeBehaviour GetNodeAtMarker(float radius = 0.03f)
    {
        NodeBehaviour[] nodes = FindObjectsByType<NodeBehaviour>(FindObjectsSortMode.None);
        NodeBehaviour closest = null;
        float minDist = radius;
        foreach (var node in nodes)
        {
            // Skip ghost/preview nodes (MonoBehaviour disabled = visual-only)
            if (node == null || !node.enabled) { continue; }
            float dist = Vector3.Distance(node.transform.position, markerTransform.position);
            if (dist < minDist) { minDist = dist; closest = node; }
        }
        return closest;
    }

    EdgeBehaviour GetEdgeAtMarker(float radius = 0.02f)
    {
        EdgeBehaviour[] edges = FindObjectsByType<EdgeBehaviour>(FindObjectsSortMode.None);
        EdgeBehaviour closest = null;
        float minDist = radius;
        foreach (var edge in edges)
        {
            if (edge == null || !edge.enabled || edge.nodeA == null || edge.nodeB == null) { continue; }
            Vector3 pt   = ClosestPointOnSegment(edge.nodeA.transform.position, edge.nodeB.transform.position, markerTransform.position);
            float   dist = Vector3.Distance(markerTransform.position, pt);
            if (dist < minDist) { minDist = dist; closest = edge; }
        }
        return closest;
    }

    LoadBehaviour GetLoadAtMarker()
    {
        LoadBehaviour[] loads = FindObjectsByType<LoadBehaviour>(FindObjectsSortMode.None);
        LoadBehaviour closest = null;
        float minDist = 0.03f;
        foreach (var load in loads)
        {
            if (load == null || !load.enabled || load.node == null) { continue; }
            float dist = Vector3.Distance(load.EndPoint(), markerTransform.position);
            if (dist < minDist) { minDist = dist; closest = load; }
        }
        return closest;
    }

    Vector3 ClosestPointOnSegment(Vector3 a, Vector3 b, Vector3 point)
    {
        Vector3 ab = b - a;
        float t = Mathf.Clamp01(Vector3.Dot(point - a, ab) / Vector3.Dot(ab, ab));
        return a + t * ab;
    }

    bool CheckDuplicateEdge(NodeBehaviour nodeA, NodeBehaviour nodeB)
    {
        if (nodeA.connectedEdges == null) { return false; }
        foreach (var e in nodeA.connectedEdges)
        {
            if (e == null) { continue; }
            if ((e.nodeA == nodeA && e.nodeB == nodeB) || (e.nodeB == nodeA && e.nodeA == nodeB)) { return true; }
        }
        return false;
    }

    // ------------------------------------------------------------------ //
    // Grid snapping
    // ------------------------------------------------------------------ //

    Vector3 GetGridPoint(Vector3 pos)
    {
        if (gridRenderer == null || !gridRenderer.isGridSet || !gridRenderer.isSnapEnabled) { return pos; }
        return gridRenderer.GetClosestGridPoint(pos);
    }

    // ------------------------------------------------------------------ //
    // Temporary edge / load preview
    // ------------------------------------------------------------------ //

    void UpdateTemporaryEdge()
    {
        if (tempEdge != null && firstSelectedNode != null)
        {
            tempEdge.UpdateEdgePosition(markerTransform.position);
        }
    }

    void UpdateTemporaryLoad()
    {
        if (firstLoadNode == null || ghostLoad == null) { return; }
        Vector3 worldDir = markerTransform.position - firstLoadNode.transform.position;
        var lb = ghostLoad.GetComponent<LoadBehaviour>();
        if (lb != null)
        {
            lb.direction = worldDir;
            lb.magnitude = worldDir.magnitude;
            lb.UpdateArrow();
        }
    }

    // ------------------------------------------------------------------ //
    // Ghost / cleanup helpers
    // ------------------------------------------------------------------ //

    void SetupGhostEdgeLine()
    {
        GameObject lineObj = new GameObject("GhostEdgeLine");
        ghostEdgeLine = lineObj.AddComponent<LineRenderer>();
        ghostEdgeLine.startWidth    = 0.005f;
        ghostEdgeLine.endWidth      = 0.005f;
        ghostEdgeLine.material      = new Material(Shader.Find("Sprites/Default"));
        ghostEdgeLine.startColor    = new Color(0.2f, 0.6f, 1f, 0.5f);
        ghostEdgeLine.endColor      = new Color(0.2f, 0.6f, 1f, 0.5f);
        ghostEdgeLine.enabled       = false;
        ghostEdgeLine.positionCount = 2;
    }

    void CleanupGhostNode()
    {
        if (ghostNode != null) { Destroy(ghostNode); ghostNode = null; }
    }

    void CleanupGhostObjects()
    {
        CleanupGhostNode();
        if (ghostEdgeLine != null) { ghostEdgeLine.enabled = false; }
        if (highlightedGrabNodes.Count > 0)
        {
            VisualFeedbackManager.Instance?.ClearConnectedHighlight(highlightedGrabNodes);
            highlightedGrabNodes.Clear();
        }
    }

    void ClearTempVisuals()
    {
        CleanupGhostObjects();
        if (tempEdge != null) { graphManager.RemoveLocalEdge(tempEdge); tempEdge = null; }
        firstSelectedNode = null;
        if (ghostLoad != null) { Destroy(ghostLoad); ghostLoad = null; }
        firstLoadNode = null;
    }

    // ------------------------------------------------------------------ //
    // Preview visibility
    // ------------------------------------------------------------------ //

    void UpdatePreviewVisibility()
    {
        worldCoordinateManager?.SetVisualsActive(currentMode == Mode.Setup && currentSetupTool == SetupTool.World);
        surfaceFinder?.SetVisualsActive(currentMode == Mode.Setup && currentSetupTool == SetupTool.Grid);
    }

    // ------------------------------------------------------------------ //
    // Message timer
    // ------------------------------------------------------------------ //

    IEnumerator ClearMessageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        modeDisplayUI?.ClearMessage(currentMode);
    }
}
