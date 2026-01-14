using UnityEngine;

/// <summary>
/// Loads pre-made example structures from StreamingAssets or Resources
/// </summary>
public class ExampleStructures : MonoBehaviour
{
    [Header("References")]
    public SaveLoadManager saveLoadManager;

    [Header("Example Files")]
    public TextAsset simpleBridgeJSON;
    public TextAsset cantileverBeamJSON;
    public TextAsset simpleRoofTrussJSON;

    /// <summary>
    /// Load the simple truss bridge example
    /// </summary>
    public void LoadSimpleBridge()
    {
        if (simpleBridgeJSON != null)
        {
            LoadFromTextAsset(simpleBridgeJSON, "Simple Truss Bridge");
        }
        else
        {
            // Fallback: Load from file if TextAsset not assigned
            LoadFromProjectFolder("SimpleTrussBridge");
        }
    }

    /// <summary>
    /// Load structure from TextAsset
    /// </summary>
    void LoadFromTextAsset(TextAsset jsonAsset, string structureName)
    {
        try
        {
            StructureSaveData saveData = JsonUtility.FromJson<StructureSaveData>(jsonAsset.text);

            Debug.Log($"[EXAMPLE] Loading example: {structureName}");
            Debug.Log($"[EXAMPLE] Nodes: {saveData.nodes.Count}, Edges: {saveData.edges.Count}, Loads: {saveData.loads.Count}");

            // Use SaveLoadManager's loading functionality
            ClearCurrentStructure();
            LoadStructureData(saveData);

            HapticFeedback.Trigger(HapticFeedback.HapticType.Success);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[EXAMPLE] Failed to load example {structureName}: {e.Message}");
            HapticFeedback.Trigger(HapticFeedback.HapticType.Error);
        }
    }

    /// <summary>
    /// Load from project SavedStructures folder
    /// </summary>
    void LoadFromProjectFolder(string fileName)
    {
        string path = System.IO.Path.Combine(Application.dataPath, "..", "SavedStructures", fileName + ".json");

        if (System.IO.File.Exists(path))
        {
            string json = System.IO.File.ReadAllText(path);
            StructureSaveData saveData = JsonUtility.FromJson<StructureSaveData>(json);

            ClearCurrentStructure();
            LoadStructureData(saveData);

            Debug.Log($"[EXAMPLE] Loaded from project folder: {fileName}");
            HapticFeedback.Trigger(HapticFeedback.HapticType.Success);
        }
        else
        {
            Debug.LogError($"[EXAMPLE] Example file not found: {path}");
        }
    }

    /// <summary>
    /// Clear all existing structures
    /// </summary>
    void ClearCurrentStructure()
    {
        NodeBehaviour[] allNodes = FindObjectsOfType<NodeBehaviour>();
        foreach (var node in allNodes)
        {
            if (node != null)
            {
                if (node.loads != null)
                {
                    foreach (var load in node.loads)
                    {
                        if (load != null) Destroy(load.gameObject);
                    }
                }
                if (node.connectedEdges != null)
                {
                    foreach (var edge in node.connectedEdges)
                    {
                        if (edge != null) Destroy(edge.gameObject);
                    }
                }
                Destroy(node.gameObject);
            }
        }

        EdgeBehaviour[] allEdges = FindObjectsOfType<EdgeBehaviour>();
        foreach (var edge in allEdges)
        {
            if (edge != null) Destroy(edge.gameObject);
        }

        LoadBehaviour[] allLoads = FindObjectsOfType<LoadBehaviour>();
        foreach (var load in allLoads)
        {
            if (load != null) Destroy(load.gameObject);
        }
    }

    /// <summary>
    /// Load structure data into scene
    /// </summary>
    void LoadStructureData(StructureSaveData saveData)
    {
        GraphManager graphManager = FindObjectOfType<GraphManager>();
        if (graphManager == null)
        {
            Debug.LogError("[EXAMPLE] GraphManager not found!");
            return;
        }

        System.Collections.Generic.Dictionary<int, NodeBehaviour> idToNode = new System.Collections.Generic.Dictionary<int, NodeBehaviour>();

        // Create nodes
        foreach (var nodeData in saveData.nodes)
        {
            NodeBehaviour node = graphManager.CreateNode(nodeData.GetPosition());
            node.isSupport = nodeData.isSupport;

            if (node.isSupport)
            {
                node.freeVisual.SetActive(false);
                node.supportVisual.SetActive(true);
            }
            else
            {
                node.freeVisual.SetActive(true);
                node.supportVisual.SetActive(false);
            }

            idToNode[nodeData.id] = node;
        }

        // Create edges
        foreach (var edgeData in saveData.edges)
        {
            if (idToNode.ContainsKey(edgeData.nodeAId) && idToNode.ContainsKey(edgeData.nodeBId))
            {
                NodeBehaviour nodeA = idToNode[edgeData.nodeAId];
                NodeBehaviour nodeB = idToNode[edgeData.nodeBId];

                EdgeBehaviour edge = graphManager.CreateEdge(nodeA);
                edge.nodeB = nodeB;

                if (nodeA.connectedEdges == null)
                    nodeA.connectedEdges = new System.Collections.Generic.List<EdgeBehaviour>();
                if (nodeB.connectedEdges == null)
                    nodeB.connectedEdges = new System.Collections.Generic.List<EdgeBehaviour>();

                nodeA.connectedEdges.Add(edge);
                nodeB.connectedEdges.Add(edge);

                edge.UpdateEdgePosition();
            }
        }

        // Create loads
        foreach (var loadData in saveData.loads)
        {
            if (idToNode.ContainsKey(loadData.nodeId))
            {
                NodeBehaviour node = idToNode[loadData.nodeId];
                LoadBehaviour load = graphManager.CreateLoad(
                    node,
                    loadData.GetDirection(),
                    loadData.magnitude
                );

                if (node.loads == null)
                    node.loads = new System.Collections.Generic.List<LoadBehaviour>();

                node.loads.Add(load);
            }
        }
    }

    /// <summary>
    /// Create simple cantilever beam example programmatically
    /// </summary>
    public void CreateCantileverBeamExample()
    {
        ClearCurrentStructure();

        GraphManager graphManager = FindObjectOfType<GraphManager>();
        if (graphManager == null) return;

        // Create nodes for cantilever beam (5 nodes along X-axis)
        NodeBehaviour[] nodes = new NodeBehaviour[5];
        for (int i = 0; i < 5; i++)
        {
            Vector3 pos = new Vector3(i * 0.3f, 0.5f, 0f);
            nodes[i] = graphManager.CreateNode(pos);
        }

        // First node is support (fixed)
        nodes[0].isSupport = true;
        nodes[0].freeVisual.SetActive(false);
        nodes[0].supportVisual.SetActive(true);

        // Create edges connecting consecutive nodes
        for (int i = 0; i < 4; i++)
        {
            EdgeBehaviour edge = graphManager.CreateEdge(nodes[i]);
            edge.nodeB = nodes[i + 1];

            if (nodes[i].connectedEdges == null)
                nodes[i].connectedEdges = new System.Collections.Generic.List<EdgeBehaviour>();
            if (nodes[i + 1].connectedEdges == null)
                nodes[i + 1].connectedEdges = new System.Collections.Generic.List<EdgeBehaviour>();

            nodes[i].connectedEdges.Add(edge);
            nodes[i + 1].connectedEdges.Add(edge);

            edge.UpdateEdgePosition();
        }

        // Add load at the end (tip of cantilever)
        LoadBehaviour load = graphManager.CreateLoad(nodes[4], Vector3.down, 1000f);
        if (nodes[4].loads == null)
            nodes[4].loads = new System.Collections.Generic.List<LoadBehaviour>();
        nodes[4].loads.Add(load);

        Debug.Log("[EXAMPLE] Created cantilever beam example");
        HapticFeedback.Trigger(HapticFeedback.HapticType.Success);
    }

    /// <summary>
    /// Create simple roof truss example programmatically
    /// </summary>
    public void CreateRoofTrussExample()
    {
        ClearCurrentStructure();

        GraphManager graphManager = FindObjectOfType<GraphManager>();
        if (graphManager == null) return;

        // Create triangular roof truss
        Vector3[] positions = new Vector3[]
        {
            new Vector3(0f, 0.3f, 0f),      // Left base (support)
            new Vector3(0.5f, 0.6f, 0f),    // Peak
            new Vector3(1f, 0.3f, 0f),      // Right base (support)
            new Vector3(0.25f, 0.45f, 0f),  // Left mid
            new Vector3(0.75f, 0.45f, 0f)   // Right mid
        };

        NodeBehaviour[] nodes = new NodeBehaviour[5];
        for (int i = 0; i < 5; i++)
        {
            nodes[i] = graphManager.CreateNode(positions[i]);
        }

        // Supports at base
        nodes[0].isSupport = true;
        nodes[0].freeVisual.SetActive(false);
        nodes[0].supportVisual.SetActive(true);

        nodes[2].isSupport = true;
        nodes[2].freeVisual.SetActive(false);
        nodes[2].supportVisual.SetActive(true);

        // Edge connections
        int[,] edgeIndices = new int[,]
        {
            {0, 1}, // Left slope
            {1, 2}, // Right slope
            {0, 2}, // Base
            {0, 3}, // Left support
            {3, 1}, // Left to peak
            {1, 4}, // Peak to right mid
            {4, 2}, // Right mid to base
            {3, 4}  // Horizontal cross-member
        };

        for (int i = 0; i < edgeIndices.GetLength(0); i++)
        {
            int idxA = edgeIndices[i, 0];
            int idxB = edgeIndices[i, 1];

            EdgeBehaviour edge = graphManager.CreateEdge(nodes[idxA]);
            edge.nodeB = nodes[idxB];

            if (nodes[idxA].connectedEdges == null)
                nodes[idxA].connectedEdges = new System.Collections.Generic.List<EdgeBehaviour>();
            if (nodes[idxB].connectedEdges == null)
                nodes[idxB].connectedEdges = new System.Collections.Generic.List<EdgeBehaviour>();

            nodes[idxA].connectedEdges.Add(edge);
            nodes[idxB].connectedEdges.Add(edge);

            edge.UpdateEdgePosition();
        }

        // Add load at peak (snow load)
        LoadBehaviour load = graphManager.CreateLoad(nodes[1], Vector3.down, 3000f);
        if (nodes[1].loads == null)
            nodes[1].loads = new System.Collections.Generic.List<LoadBehaviour>();
        nodes[1].loads.Add(load);

        Debug.Log("[EXAMPLE] Created roof truss example");
        HapticFeedback.Trigger(HapticFeedback.HapticType.Success);
    }
}
