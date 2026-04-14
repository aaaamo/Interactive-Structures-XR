using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Manages the visual hints for structural optimization:
///   - Nudge arrows on each free node pointing toward the ghost (converged) position
///   - Debug ghost showing where the structure would converge after N gradient steps
///
/// Arrow direction = (ghostPosition - currentPosition), so each arrow tells the user
/// exactly where to move that node to reduce compliance.
/// When showGhost is false, arrows fall back to single-step gradient descent direction.
///
/// Attach to the same GameObject as StructuralAnalyzer (or any persistent object).
/// Wire references in the Inspector.
/// </summary>
public class OptimizeVisualizer : MonoBehaviour
{
    [Header("References")]
    public StructuralAnalyzer structuralAnalyzer;
    public GraphManager graphManager;
    public ModeDisplayUI modeDisplayUI;

    [Header("Ghost Settings")]
    public Material ghostMaterial;  // Same material slot as displacedMaterialPrefab
    public bool showGhost = true;
    public int ghostSteps = 30;
    public float ghostStepSize = 0.005f;

    [Header("Arrow Settings")]
    public float maxArrowLength = 0.3f;
    public float minArrowLength = 0.02f;

    // ------------------------------------------------------------------ //
    // Internal state
    // ------------------------------------------------------------------ //

    private readonly Dictionary<NodeBehaviour, GameObject> _arrowObjects
        = new Dictionary<NodeBehaviour, GameObject>();

    private readonly List<GameObject> _ghostNodes = new List<GameObject>();
    private readonly List<GameObject> _ghostEdges = new List<GameObject>();

    private bool _hintsActive;

    // ------------------------------------------------------------------ //
    // Public API
    // ------------------------------------------------------------------ //

    private void Start()
    {
        RefreshHints();
    }

    /// <summary>Called when entering Optimize mode (shows ghost).</summary>
    public void ShowHints()
    {
        _hintsActive = true;
        RefreshHints();
    }

    /// <summary>Called when leaving Optimize mode (hides ghost only).</summary>
    public void HideHints()
    {
        _hintsActive = false;
        DestroyGhost();
        // Arrows remain visible in all modes
    }

    /// <summary>Call after any structural modification.</summary>
    public void OnStructureChanged()
    {
        RefreshHints();
    }

// ------------------------------------------------------------------ //
    // Core refresh
    // ------------------------------------------------------------------ //

    private void RefreshHints()
    {
        NodeBehaviour[] nodes = FindObjectsByType<NodeBehaviour>(FindObjectsSortMode.InstanceID);
        EdgeBehaviour[] edges = FindObjectsByType<EdgeBehaviour>(FindObjectsSortMode.InstanceID);

        if (nodes.Length == 0) { return; }

        StructureData data = StructuralAnalyzer.BuildStructureData(nodes, edges);
        if (data.supportNodes.Count == 0 || data.nodeLoads.Count == 0)
        {
            DestroyArrows();
            DestroyGhost();
            modeDisplayUI?.SetCompliance(0f, false);
            return;
        }

        float E = structuralAnalyzer != null ? structuralAnalyzer.youngModulus : 2_060_000f;
        float A = structuralAnalyzer != null ? structuralAnalyzer.crossSectionalArea : 100f;

        TrussAnalysisResult result = TrussAnalyzer.AnalyzeTruss(data, E, A);
        if (result.errorMessage != null)
        {
            DestroyArrows();
            DestroyGhost();
            modeDisplayUI?.SetCompliance(0f, false);
            return;
        }

        // Compliance = Σ (load · displacement) over all loaded nodes
        float compliance = 0f;
        foreach (var kvp in data.nodeLoads)
        {
            if (result.displacements.TryGetValue(kvp.Key, out Vector3 disp))
            {
                compliance += Vector3.Dot(kvp.Value, disp);
            }
        }
        modeDisplayUI?.SetCompliance(compliance, true);

        // Gradient is used only to identify free (non-support, non-load) nodes
        Dictionary<NodeBehaviour, Vector3> gradient = TrussOptimizer.ComputeGradient(data, result, E, A);

        Debug.Log($"[Optimize] nodes={nodes.Length} supports={data.supportNodes.Count} loads={data.nodeLoads.Count} free={gradient.Count}");

        if (showGhost)
        {
            // Compute ghost positions and update arrows synchronously — no coroutine, no frame gap
            ComputeAndShowGhost(nodes, edges, E, A, gradient);
        }
        else
        {
            // No ghost: fall back to single-step gradient descent direction
            var descentVectors = new Dictionary<NodeBehaviour, Vector3>();
            foreach (var kvp in gradient)
            {
                descentVectors[kvp.Key] = -kvp.Value; // descent = -gradient
            }
            UpdateArrows(descentVectors);
        }
    }

    // ------------------------------------------------------------------ //
    // Arrow management
    // ------------------------------------------------------------------ //

    /// <summary>
    /// Update or create arrows for each node.
    /// vectors: desired arrow direction and relative magnitude per free node.
    /// Direction is vectors[node].normalized — no negation applied here.
    /// Length is scaled proportionally to max magnitude in the set.
    /// </summary>
    private void UpdateArrows(Dictionary<NodeBehaviour, Vector3> vectors)
    {
        var activeNodes = new HashSet<NodeBehaviour>(vectors.Keys);

        // Remove arrows for nodes no longer active
        var toRemove = new List<NodeBehaviour>();
        foreach (var kvp in _arrowObjects)
        {
            if (!activeNodes.Contains(kvp.Key))
            {
                if (kvp.Value != null) { Destroy(kvp.Value); }
                toRemove.Add(kvp.Key);
            }
        }
        foreach (var k in toRemove) { _arrowObjects.Remove(k); }

        // Normalize arrow lengths by max magnitude for scale-independent display
        float maxMag = 0f;
        foreach (var kvp in vectors)
        {
            if (kvp.Value.magnitude > maxMag) { maxMag = kvp.Value.magnitude; }
        }

        // Create or update arrows
        foreach (var kvp in vectors)
        {
            NodeBehaviour node = kvp.Key;
            Vector3 vec = kvp.Value;
            float mag = vec.magnitude;

            if (mag < 1e-6f)
            {
                // Near-zero displacement — node is already near-optimal, hide arrow
                if (_arrowObjects.TryGetValue(node, out GameObject existing))
                {
                    existing.SetActive(false);
                }
                continue;
            }

            Vector3 dir = vec.normalized;
            // If the largest delta exceeds minArrowLength, show actual world-space scale (no normalization).
            // If all deltas are tiny (structure near-optimal), normalize so the largest reaches maxArrowLength.
            float displayLength;
            if (maxMag > minArrowLength)
            {
                displayLength = Mathf.Clamp(mag, minArrowLength, maxArrowLength);
            }
            else
            {
                displayLength = Mathf.Clamp((mag / maxMag) * maxArrowLength, minArrowLength, maxArrowLength);
            }

            if (!_arrowObjects.TryGetValue(node, out GameObject arrowObj) || arrowObj == null)
            {
                arrowObj = CreateArrow(node);
                if (arrowObj == null) { continue; }
                _arrowObjects[node] = arrowObj;
            }

            arrowObj.SetActive(true);
            var lb = arrowObj.GetComponent<LoadBehaviour>();
            if (lb != null)
            {
                lb.direction = dir;
                lb.magnitude = displayLength;
                lb.UpdateArrow();
            }
        }
    }

    private void DestroyArrows()
    {
        foreach (var kvp in _arrowObjects)
        {
            if (kvp.Value != null) { Destroy(kvp.Value); }
        }
        _arrowObjects.Clear();
    }

    // ------------------------------------------------------------------ //
    // Ghost management
    // ------------------------------------------------------------------ //

    private void ComputeAndShowGhost(
        NodeBehaviour[] nodes, EdgeBehaviour[] edges, float E, float A,
        Dictionary<NodeBehaviour, Vector3> gradient)
    {
        // Snapshot current positions and build index map
        Vector3[] snapshot = new Vector3[nodes.Length];
        var nodeToIdx = new Dictionary<NodeBehaviour, int>();
        for (int i = 0; i < nodes.Length; i++)
        {
            snapshot[i] = nodes[i] != null ? nodes[i].transform.position : Vector3.zero;
            if (nodes[i] != null) { nodeToIdx[nodes[i]] = i; }
        }

        Vector3[] ghostPositions = TrussOptimizer.ComputeGhostPositions(
            nodes, edges, snapshot, E, A, ghostSteps, ghostStepSize);

        // Compute per-free-node displacement: current position → ghost position
        var ghostDeltas = new Dictionary<NodeBehaviour, Vector3>();
        foreach (NodeBehaviour node in gradient.Keys)
        {
            if (!nodeToIdx.TryGetValue(node, out int i)) { continue; }
            Vector3 delta = ghostPositions[i] - snapshot[i];
            ghostDeltas[node] = delta;
            Debug.Log($"[Optimize] node={node.name} ghostDelta=({delta.x:F3},{delta.y:F3},{delta.z:F3}) mag={delta.magnitude:F4}");
        }

        // Arrows point from current position toward ghost position
        UpdateArrows(ghostDeltas);
        UpdateGhostObjects(nodes, edges, ghostPositions);
    }

    private void UpdateGhostObjects(
        NodeBehaviour[] nodes, EdgeBehaviour[] edges, Vector3[] positions)
    {
        DestroyGhost();

        GameObject nodePrefab = graphManager != null ? graphManager.nodePrefab : null;
        GameObject edgePrefab = graphManager != null ? graphManager.edgePrefab : null;

        // Ghost nodes — instantiate node prefab, apply ghostMaterial
        for (int i = 0; i < nodes.Length; i++)
        {
            if (nodes[i] == null || nodePrefab == null) { continue; }
            GameObject gn = Instantiate(nodePrefab, positions[i], nodes[i].transform.rotation);
            ApplyGhostMaterial(gn);
            // Disable all scripts so ghost doesn't register as a live node
            foreach (var mb in gn.GetComponentsInChildren<MonoBehaviour>())
            {
                mb.enabled = false;
            }
            _ghostNodes.Add(gn);
        }

        // Build node-to-index map for edge lookup
        var nodeToGhostIdx = new Dictionary<NodeBehaviour, int>();
        for (int i = 0; i < nodes.Length; i++)
        {
            if (nodes[i] != null) { nodeToGhostIdx[nodes[i]] = i; }
        }

        // Ghost edges — instantiate edge prefab, apply ghostMaterial
        foreach (var edge in edges)
        {
            if (edge == null || edge.nodeA == null || edge.nodeB == null) { continue; }
            if (!nodeToGhostIdx.TryGetValue(edge.nodeA, out int ia)) { continue; }
            if (!nodeToGhostIdx.TryGetValue(edge.nodeB, out int ib)) { continue; }
            if (edgePrefab == null) { continue; }

            Vector3 start = positions[ia];
            Vector3 end   = positions[ib];
            float   len   = Vector3.Distance(start, end);
            if (len < 1e-4f) { continue; }

            GameObject ge = Instantiate(edgePrefab, (start + end) * 0.5f, Quaternion.identity);
            ge.transform.up = (end - start).normalized;
            Vector3 s = edge.transform.localScale;
            s.y = len * 0.5f;
            ge.transform.localScale = s;
            ApplyGhostMaterial(ge);
            foreach (var mb in ge.GetComponentsInChildren<MonoBehaviour>())
            {
                mb.enabled = false;
            }
            _ghostEdges.Add(ge);
        }
    }

    private void ApplyGhostMaterial(GameObject go)
    {
        if (ghostMaterial == null) { return; }
        foreach (var r in go.GetComponentsInChildren<Renderer>())
        {
            r.material = ghostMaterial;
        }
    }

    private void DestroyGhost()
    {
        foreach (var g in _ghostNodes)
        {
            if (g != null) { Destroy(g); }
        }
        foreach (var g in _ghostEdges)
        {
            if (g != null) { Destroy(g); }
        }
        _ghostNodes.Clear();
        _ghostEdges.Clear();
    }

    // ------------------------------------------------------------------ //
    // Arrow instantiation — mirrors HandleAddLoad ghost pattern
    // ------------------------------------------------------------------ //

    private GameObject CreateArrow(NodeBehaviour node)
    {
        if (graphManager == null || graphManager.loadPrefab == null) { return null; }

        GameObject go = Instantiate(graphManager.loadPrefab, node.transform.position, Quaternion.identity);
        go.transform.SetParent(node.transform);

        // Destroy NetworkObject so it doesn't register on the network
        var netObj = go.GetComponent<NetworkObject>();
        if (netObj != null) { Destroy(netObj); }

        var lb = go.GetComponent<LoadBehaviour>();
        if (lb != null)
        {
            lb.node = node;
            // OnTransformParentChanged fires during SetParent and adds this to node.loads.
            // Remove it — nudge arrows are visual only and must not affect FEM analysis.
            if (node.loads.Contains(lb)) { node.loads.Remove(lb); }
        }

        // Apply semi-transparent teal color to distinguish from load arrows
        if (ghostMaterial != null)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                r.material = ghostMaterial;
                r.material.color = new Color(0f, 1f, 0.8f, 0.6f); // teal, semi-transparent
            }
        }

        return go;
    }

    // ------------------------------------------------------------------ //
    // Cleanup on destroy
    // ------------------------------------------------------------------ //

    private void OnDestroy()
    {
        HideHints();
    }
}
