using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Build mode partial: trigger dispatch, individual tool handlers,
/// multi-select, and build visual feedback.
/// </summary>
public partial class OVRGraphController
{
    // ------------------------------------------------------------------ //
    // Build mode trigger dispatch
    // ------------------------------------------------------------------ //

    void HandleBuildTrigger()
    {
        bool triggerPressed = OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch);
        bool bPressed       = OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch);

        // B button: cancel two-step workflow or clear selection
        if (bPressed)
        {
            ClearTempVisuals();
            ClearSelection();
            return;
        }

        if (triggerPressed && !triggerHeldLastFrame)
        {
            switch (currentBuildTool)
            {
                case BuildTool.Node:    HandleAddNode();       break;
                case BuildTool.Edge:    HandleAddEdge();       break;
                case BuildTool.Load:    HandleAddLoad();       break;
                case BuildTool.Support: HandleToggleSupport(); break;
                case BuildTool.Delete:  HandleDelete();        break;
                case BuildTool.Select:  HandleSelectTrigger(); break;
                case BuildTool.Analyze: HandleAnalyze();       break;
            }
        }
        triggerHeldLastFrame = triggerPressed;
    }

    // ------------------------------------------------------------------ //
    // Individual build tool handlers
    // ------------------------------------------------------------------ //

    void HandleAddNode()
    {
        Vector3 worldPos = GetGridPoint(markerTransform.position);
        Vector3 localPos = worldCoordinateManager.WorldToLocal(worldPos);
        graphManager.CreateNodeServerRpc(localPos);
        HapticFeedback.Trigger(HapticFeedback.HapticType.Medium);
        markerController?.Pulse();
        unifiedTutorial?.OnActionPerformed(Mode.Build);
        OnStructureReset();
        optimizeVisualizer?.OnStructureChanged();
    }

    void HandleAddEdge()
    {
        NodeBehaviour node = GetNodeAtMarker();
        if (node == null) { return; }

        if (firstSelectedNode == null)
        {
            firstSelectedNode = node;
            tempEdge = graphManager.CreateLocalEdge(firstSelectedNode);
            HapticFeedback.Trigger(HapticFeedback.HapticType.Medium);
            VisualFeedbackManager.Instance?.SetSelected(node.gameObject, new Color(0.2f, 1f, 0.2f, 1f));
        }
        else if (node != firstSelectedNode)
        {
            if (!CheckDuplicateEdge(firstSelectedNode, node))
            {
                graphManager.CreateEdgeServerRpc(firstSelectedNode.NetworkObjectId, node.NetworkObjectId);
                HapticFeedback.Trigger(HapticFeedback.HapticType.Success);
                OnStructureReset();
                optimizeVisualizer?.OnStructureChanged();
            }
            else
            {
                HapticFeedback.Trigger(HapticFeedback.HapticType.Error);
                modeDisplayUI?.ShowMessage("Edge already exists!", new Color(1f, 0.3f, 0.3f));
                StartCoroutine(ClearMessageAfterDelay(2f));
            }
            VisualFeedbackManager.Instance?.ClearSelection();
            if (tempEdge != null) { graphManager.RemoveLocalEdge(tempEdge); }
            firstSelectedNode = null;
            tempEdge = null;
        }
    }

    void HandleAddLoad()
    {
        if (firstLoadNode == null)
        {
            NodeBehaviour node = GetNodeAtMarker();
            if (node == null) { return; }
            firstLoadNode = node;

            if (graphManager.loadPrefab != null)
            {
                ghostLoad = Instantiate(graphManager.loadPrefab, node.transform.position, Quaternion.identity);

                // Remove ALL NetworkObject components in the entire hierarchy BEFORE SetParent.
                // Even one remaining NetworkObject will throw SpawnStateException in
                // NetworkObject.OnTransformParentChanged when SetParent fires.
                foreach (var no in ghostLoad.GetComponentsInChildren<NetworkObject>(true))
                {
                    DestroyImmediate(no);
                }

                ghostLoad.transform.SetParent(node.transform);

                var lb = ghostLoad.GetComponent<LoadBehaviour>();
                if (lb != null)
                {
                    lb.node = node;
                    lb.UpdateArrow();
                    // OnTransformParentChanged may register lb into node.loads — remove it.
                    // Ghost load is visual-only and must not affect FEM analysis.
                    if (node.loads != null && node.loads.Contains(lb)) { node.loads.Remove(lb); }
                }
            }

            HapticFeedback.Trigger(HapticFeedback.HapticType.Medium);
            VisualFeedbackManager.Instance?.SetSelected(node.gameObject, new Color(1f, 0.6f, 0.2f, 1f));
        }
        else
        {
            Vector3 worldDir = markerTransform.position - firstLoadNode.transform.position;
            float   mag      = worldDir.magnitude;
            Vector3 localDir = firstLoadNode.transform.InverseTransformDirection(worldDir.normalized);

            graphManager.CreateLoadServerRpc(firstLoadNode.NetworkObjectId, localDir, mag);
            HapticFeedback.Trigger(HapticFeedback.HapticType.Success);
            VisualFeedbackManager.Instance?.ClearSelection();
            firstLoadNode = null;
            if (ghostLoad != null) { Destroy(ghostLoad); ghostLoad = null; }
            OnStructureReset();
            optimizeVisualizer?.OnStructureChanged();
        }
    }

    void HandleToggleSupport()
    {
        var node = GetNodeAtMarker();
        if (node == null) { return; }
        node.ToggleSupport();
        HapticFeedback.Trigger(HapticFeedback.HapticType.Medium);
        OnStructureReset();
        optimizeVisualizer?.OnStructureChanged();
    }

    void HandleDelete()
    {
        var node = GetNodeAtMarker();
        if (node != null)
        {
            graphManager.DeleteNetworkObjectServerRpc(node.NetworkObjectId);
            HapticFeedback.Trigger(HapticFeedback.HapticType.Strong);
            OnStructureReset();
            optimizeVisualizer?.OnStructureChanged();
            return;
        }
        var edge = GetEdgeAtMarker();
        if (edge != null)
        {
            graphManager.DeleteNetworkObjectServerRpc(edge.NetworkObjectId);
            HapticFeedback.Trigger(HapticFeedback.HapticType.Strong);
            OnStructureReset();
            optimizeVisualizer?.OnStructureChanged();
            return;
        }
        var load = GetLoadAtMarker();
        if (load != null)
        {
            graphManager.DeleteNetworkObjectServerRpc(load.NetworkObjectId);
            HapticFeedback.Trigger(HapticFeedback.HapticType.Strong);
            OnStructureReset();
            optimizeVisualizer?.OnStructureChanged();
        }
    }

    // ------------------------------------------------------------------ //
    // Multi-select (BuildTool.Select)
    // ------------------------------------------------------------------ //

    void HandleSelectTrigger()
    {
        var node = GetNodeAtMarker();
        if (node == null) { return; }

        if (_selectedNodes.Contains(node)) { _selectedNodes.Remove(node); }
        else                               { _selectedNodes.Add(node);    }

        UpdateSelectionHighlights();
        int count = _selectedNodes.Count;
        modeDisplayUI?.ShowMessage(
            count > 0 ? $"{count} node{(count > 1 ? "s" : "")} selected" : "Selection cleared",
            Color.cyan);
    }

    void UpdateSelectionHighlights()
    {
        VisualFeedbackManager.Instance?.ClearSelection();
        foreach (var node in _selectedNodes)
        {
            if (node != null)
            {
                // Bright magenta — very distinct from all other node colors
                VisualFeedbackManager.Instance?.SetSelected(node.gameObject, new Color(1f, 0.15f, 0.9f, 1f));
            }
        }
    }

    void ClearSelection()
    {
        _selectedNodes.Clear();
        VisualFeedbackManager.Instance?.ClearSelection();
    }

    // ------------------------------------------------------------------ //
    // Build visual feedback (per-frame hover highlights)
    // ------------------------------------------------------------------ //

    void UpdateBuildVisualFeedback()
    {
        if (hoveredObject != null)
        {
            VisualFeedbackManager.Instance?.ClearHover();
            hoveredObject = null;
        }

        switch (currentBuildTool)
        {
            case BuildTool.Node:    UpdateAddNodeFeedback();        break;
            case BuildTool.Edge:    UpdateAddEdgeFeedback();        break;
            case BuildTool.Load:    UpdateAddLoadFeedback();        break;
            case BuildTool.Support: UpdateToggleSupportFeedback();  break;
            case BuildTool.Delete:  UpdateDeleteFeedback();         break;
            case BuildTool.Select:  UpdateSelectFeedback();         break;
            case BuildTool.Analyze: UpdateAnalyzeFeedback();        break;
        }
    }

    void UpdateAddNodeFeedback()
    {
        Vector3 snapPos = GetGridPoint(markerTransform.position);
        if (ghostNode == null && graphManager.nodePrefab != null)
        {
            ghostNode = Instantiate(graphManager.nodePrefab, snapPos, Quaternion.identity);
            ghostNode.name = "GhostNode";
            foreach (var rend in ghostNode.GetComponentsInChildren<Renderer>())
            {
                Material[] mats = rend.materials;
                for (int i = 0; i < mats.Length; i++)
                {
                    mats[i] = new Material(mats[i]);
                    if (mats[i].HasProperty("_Color"))
                    {
                        Color c = mats[i].color;
                        c.a = 0.3f;
                        mats[i].color = c;
                    }
                }
                rend.materials = mats;
            }
            var nb = ghostNode.GetComponent<NodeBehaviour>();
            if (nb != null)
            {
                nb.enabled = false;
                if (nb.nodeLabel != null) { nb.nodeLabel.gameObject.SetActive(false); }
            }
        }
        else if (ghostNode != null)
        {
            ghostNode.transform.position = snapPos;
        }
    }

    void UpdateAddEdgeFeedback()
    {
        CleanupGhostNode();
        NodeBehaviour node = GetNodeAtMarker();
        if (node != null)
        {
            hoveredObject = node.gameObject;
            Color col = firstSelectedNode == null ? new Color(0.2f, 1f, 0.2f) : new Color(0.2f, 0.6f, 1f);
            VisualFeedbackManager.Instance?.HighlightHover(node.gameObject, col);
        }
        if (firstSelectedNode != null && ghostEdgeLine != null)
        {
            ghostEdgeLine.enabled = true;
            ghostEdgeLine.SetPosition(0, firstSelectedNode.transform.position);
            ghostEdgeLine.SetPosition(1, markerTransform.position);
            bool dup = node != null && node != firstSelectedNode && CheckDuplicateEdge(firstSelectedNode, node);
            var lineCol = dup ? new Color(1f, 0f, 0f, 0.5f) : new Color(0.2f, 1f, 0.2f, 0.5f);
            ghostEdgeLine.startColor = lineCol;
            ghostEdgeLine.endColor   = lineCol;
        }
        else if (ghostEdgeLine != null)
        {
            ghostEdgeLine.enabled = false;
        }
    }

    void UpdateAddLoadFeedback()
    {
        CleanupGhostNode();
        if (ghostEdgeLine != null) { ghostEdgeLine.enabled = false; }
        if (firstLoadNode == null)
        {
            var node = GetNodeAtMarker();
            if (node != null)
            {
                hoveredObject = node.gameObject;
                VisualFeedbackManager.Instance?.HighlightHover(node.gameObject, new Color(1f, 0.6f, 0.2f));
            }
        }
    }

    void UpdateToggleSupportFeedback()
    {
        CleanupGhostObjects();
        var node = GetNodeAtMarker();
        if (node != null)
        {
            hoveredObject = node.gameObject;
            VisualFeedbackManager.Instance?.HighlightHover(node.gameObject, new Color(0.8f, 0.8f, 0.2f));
        }
    }

    void UpdateDeleteFeedback()
    {
        CleanupGhostObjects();
        var node = GetNodeAtMarker();
        if (node != null) { hoveredObject = node.gameObject; VisualFeedbackManager.Instance?.HighlightHover(node.gameObject, new Color(1f, 0.2f, 0.2f)); return; }
        var edge = GetEdgeAtMarker();
        if (edge != null) { hoveredObject = edge.gameObject; VisualFeedbackManager.Instance?.HighlightHover(edge.gameObject, new Color(1f, 0.2f, 0.2f)); return; }
        var load = GetLoadAtMarker();
        if (load != null) { hoveredObject = load.gameObject; VisualFeedbackManager.Instance?.HighlightHover(load.gameObject, new Color(1f, 0.2f, 0.2f)); }
    }

    void UpdateSelectFeedback()
    {
        CleanupGhostObjects();
        var node = GetNodeAtMarker();
        if (node != null)
        {
            hoveredObject = node.gameObject;
            bool alreadySelected = _selectedNodes.Contains(node);
            VisualFeedbackManager.Instance?.HighlightHover(node.gameObject,
                alreadySelected ? new Color(1f, 0.15f, 0.9f) : new Color(1f, 0.6f, 0.95f));
        }
    }

    // ------------------------------------------------------------------ //
    // Analyze tool
    // ------------------------------------------------------------------ //

    void HandleAnalyze()
    {
        structuralAnalyzer?.PerformAnalysis();
        HapticFeedback.Trigger(HapticFeedback.HapticType.Medium);
        // resultsDisplay text panel intentionally kept hidden
    }

    void UpdateAnalyzeFeedback()
    {
        CleanupGhostObjects();
        // No hover highlight needed — analysis is a global action, not node-targeted
    }
}
