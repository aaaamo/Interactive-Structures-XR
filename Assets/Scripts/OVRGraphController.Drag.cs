using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Drag partial: grip-drag dispatch and all IEnumerator drag coroutines.
/// Available in every mode — no mode check needed at call sites.
/// </summary>
public partial class OVRGraphController
{
    // ------------------------------------------------------------------ //
    // Grip drag dispatch
    // ------------------------------------------------------------------ //

    void HandleGripDrag()
    {
        if (!OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch)) { return; }

        // Both grips → grab entire structure (all live nodes)
        if (OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.LTouch))
        {
            StartCoroutine(GrabAllNodesCoroutine());
            return;
        }

        // Multi-select drag takes priority when Select tool is active with nodes selected
        if (currentMode == Mode.Build && currentBuildTool == BuildTool.Select && _selectedNodes.Count > 0)
        {
            StartCoroutine(MoveMultipleNodesCoroutine(new HashSet<NodeBehaviour>(_selectedNodes)));
            return;
        }

        var node = GetNodeAtMarker();
        if (node != null) { StartCoroutine(MoveNodeCoroutine(node)); return; }

        var edge = GetEdgeAtMarker();
        if (edge != null) { StartCoroutine(MoveEdgeCoroutine(edge)); return; }

        var load = GetLoadAtMarker();
        if (load != null) { StartCoroutine(MoveLoadCoroutine(load)); return; }
    }

    // ------------------------------------------------------------------ //
    // Drag coroutines
    // ------------------------------------------------------------------ //

    IEnumerator MoveNodeCoroutine(NodeBehaviour node)
    {
        if (node == null) { yield break; }

        // Snapshot original local position so we can restore it on escalation
        Vector3 originalLocalPos = node.transform.localPosition;
        Vector3 offset           = node.transform.position - markerTransform.position;

        while (OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch))
        {
            // Left grip added → restore node then escalate to connected-component grab
            if (OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.LTouch))
            {
                // Restore node to where it was before the right grip landed
                if (NetworkManager.Singleton.IsServer) { node.transform.localPosition = originalLocalPos; }
                else                                   { node.MoveServerRpc(originalLocalPos); }
                yield return null;   // one frame for position to propagate
                yield return StartCoroutine(GrabAllNodesCoroutine());
                yield break;
            }

            Vector3 movedPos = GetGridPoint(markerTransform.position + offset);
            Vector3 localPos = worldCoordinateManager.WorldToLocal(movedPos);
            if (NetworkManager.Singleton.IsServer) { node.transform.localPosition = localPos; }
            else                                   { node.MoveServerRpc(localPos); }
            yield return null;
        }
        OnDragEnd();
    }

    IEnumerator MoveEdgeCoroutine(EdgeBehaviour edge)
    {
        if (edge == null || edge.nodeA == null || edge.nodeB == null) { yield break; }
        Vector3 offsetA = edge.nodeA.transform.position - markerTransform.position;
        Vector3 offsetB = edge.nodeB.transform.position - markerTransform.position;
        while (OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch))
        {
            Vector3 localA = worldCoordinateManager.WorldToLocal(markerTransform.position + offsetA);
            Vector3 localB = worldCoordinateManager.WorldToLocal(markerTransform.position + offsetB);
            if (NetworkManager.Singleton.IsServer)
            {
                edge.nodeA.transform.localPosition = localA;
                edge.nodeB.transform.localPosition = localB;
            }
            else
            {
                edge.nodeA.MoveServerRpc(localA);
                edge.nodeB.MoveServerRpc(localB);
            }
            yield return null;
        }
        OnDragEnd();
    }

    IEnumerator MoveLoadCoroutine(LoadBehaviour load)
    {
        if (load == null || load.node == null) { yield break; }
        Vector3 offset = load.EndPoint() - markerTransform.position;
        while (OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch))
        {
            Vector3 worldDir = (markerTransform.position + offset) - load.node.transform.position;
            float   mag      = worldDir.magnitude;
            Vector3 localDir = load.node.transform.InverseTransformDirection(worldDir.normalized);
            if (NetworkManager.Singleton.IsServer)
            {
                load.directionNet.Value  = localDir;
                load.magnitudeNet.Value  = mag;
            }
            else
            {
                load.UpdateLoadServerRpc(localDir, mag);
            }
            yield return null;
        }
        optimizeVisualizer?.OnStructureChanged();
    }

    IEnumerator GrabStructureCoroutine(NodeBehaviour startNode)
    {
        if (startNode == null) { yield break; }

        // BFS to collect all nodes connected to startNode
        var connectedNodes = new HashSet<NodeBehaviour>();
        var queue = new Queue<NodeBehaviour>();
        queue.Enqueue(startNode);
        connectedNodes.Add(startNode);
        while (queue.Count > 0)
        {
            NodeBehaviour n = queue.Dequeue();
            if (n.connectedEdges == null) { continue; }
            foreach (EdgeBehaviour edge in n.connectedEdges)
            {
                NodeBehaviour other = edge.nodeA == n ? edge.nodeB : edge.nodeA;
                if (other != null && !connectedNodes.Contains(other))
                {
                    connectedNodes.Add(other);
                    queue.Enqueue(other);
                }
            }
        }

        var offsets = new Dictionary<NodeBehaviour, Vector3>();
        foreach (var n in connectedNodes)
        {
            offsets[n] = n.transform.position - markerTransform.position;
        }

        while (OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch))
        {
            foreach (var n in connectedNodes)
            {
                Vector3 localPos = worldCoordinateManager.WorldToLocal(markerTransform.position + offsets[n]);
                if (NetworkManager.Singleton.IsServer) { n.transform.localPosition = localPos; }
                else                                   { n.MoveServerRpc(localPos); }
            }
            yield return null;
        }
        OnDragEnd();
    }

    /// <summary>Both grips: BFS-grabs the connected component nearest to the right marker.</summary>
    IEnumerator GrabAllNodesCoroutine()
    {
        // Find the nearest live node as BFS seed (wide radius)
        NodeBehaviour seed = GetNodeAtMarker(radius: 0.5f);
        if (seed == null) { yield break; }

        // BFS — same logic as GrabStructureCoroutine
        var connected = new HashSet<NodeBehaviour>();
        var queue     = new Queue<NodeBehaviour>();
        queue.Enqueue(seed);
        connected.Add(seed);
        while (queue.Count > 0)
        {
            NodeBehaviour n = queue.Dequeue();
            if (n.connectedEdges == null) { continue; }
            foreach (EdgeBehaviour edge in n.connectedEdges)
            {
                NodeBehaviour other = edge.nodeA == n ? edge.nodeB : edge.nodeA;
                if (other != null && !connected.Contains(other))
                {
                    connected.Add(other);
                    queue.Enqueue(other);
                }
            }
        }

        var offsets = new Dictionary<NodeBehaviour, Vector3>();
        foreach (var n in connected)
            offsets[n] = n.transform.position - markerTransform.position;

        // Continue while EITHER grip is held
        while (OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch) ||
               OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.LTouch))
        {
            foreach (var kvp in offsets)
            {
                Vector3 localPos = worldCoordinateManager.WorldToLocal(markerTransform.position + kvp.Value);
                if (NetworkManager.Singleton.IsServer) { kvp.Key.transform.localPosition = localPos; }
                else                                   { kvp.Key.MoveServerRpc(localPos); }
            }
            yield return null;
        }
        OnDragEnd();
    }

    IEnumerator MoveMultipleNodesCoroutine(HashSet<NodeBehaviour> nodes)
    {
        var offsets = new Dictionary<NodeBehaviour, Vector3>();
        foreach (var n in nodes)
        {
            if (n != null) { offsets[n] = n.transform.position - markerTransform.position; }
        }

        while (OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch))
        {
            foreach (var kvp in offsets)
            {
                NodeBehaviour n = kvp.Key;
                if (n == null) { continue; }
                Vector3 newPos   = GetGridPoint(markerTransform.position + kvp.Value);
                Vector3 localPos = worldCoordinateManager.WorldToLocal(newPos);
                if (NetworkManager.Singleton.IsServer) { n.transform.localPosition = localPos; }
                else                                   { n.MoveServerRpc(localPos); }
            }
            yield return null;
        }
        OnDragEnd();
    }

    // ------------------------------------------------------------------ //
    // Drag end callback
    // ------------------------------------------------------------------ //

    /// <summary>Called after any grip drag ends to refresh optimization hints and snapshot.</summary>
    void OnDragEnd()
    {
        bool inOptimize = currentMode == Mode.Optimize;
        optimizeVisualizer?.OnStructureChanged(fromDrag: inOptimize);
    }
}
