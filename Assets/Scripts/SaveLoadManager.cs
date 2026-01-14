using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Manages saving and loading structures to/from JSON files
/// </summary>
public class SaveLoadManager : MonoBehaviour
{
    [Header("References")]
    public GraphManager graphManager;

    [Header("Settings")]
    public string saveDirectory = "StructureSaves";
    public string defaultFileName = "MyStructure";

    private string SavePath
    {
        get
        {
            string path = Path.Combine(Application.persistentDataPath, saveDirectory);
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return path;
        }
    }

    // Singleton
    private static SaveLoadManager _instance;
    public static SaveLoadManager Instance
    {
        get
        {
            if (_instance == null)
                _instance = FindObjectOfType<SaveLoadManager>();
            return _instance;
        }
    }

    void Awake()
    {
        if (_instance == null)
            _instance = this;
        else if (_instance != this)
            Destroy(gameObject);
    }

    /// <summary>
    /// Save current structure to file
    /// </summary>
    public bool SaveStructure(string fileName = null)
    {
        try
        {
            if (string.IsNullOrEmpty(fileName))
                fileName = defaultFileName;

            // Ensure .json extension
            if (!fileName.EndsWith(".json"))
                fileName += ".json";

            // Create save data
            StructureSaveData saveData = CreateSaveData(fileName);

            // Convert to JSON
            string json = JsonUtility.ToJson(saveData, true);

            // Write to file
            string filePath = Path.Combine(SavePath, fileName);
            File.WriteAllText(filePath, json);

            Debug.Log($"[SAVE] Structure saved to: {filePath}");
            Debug.Log($"[SAVE] Nodes: {saveData.nodes.Count}, Edges: {saveData.edges.Count}, Loads: {saveData.loads.Count}");

            HapticFeedback.Trigger(HapticFeedback.HapticType.Success);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SAVE] Failed to save structure: {e.Message}");
            HapticFeedback.Trigger(HapticFeedback.HapticType.Error);
            return false;
        }
    }

    /// <summary>
    /// Load structure from file
    /// </summary>
    public bool LoadStructure(string fileName)
    {
        try
        {
            // Ensure .json extension
            if (!fileName.EndsWith(".json"))
                fileName += ".json";

            string filePath = Path.Combine(SavePath, fileName);

            if (!File.Exists(filePath))
            {
                Debug.LogError($"[LOAD] File not found: {filePath}");
                HapticFeedback.Trigger(HapticFeedback.HapticType.Error);
                return false;
            }

            // Read file
            string json = File.ReadAllText(filePath);

            // Parse JSON
            StructureSaveData saveData = JsonUtility.FromJson<StructureSaveData>(json);

            // Clear existing structure
            ClearCurrentStructure();

            // Load structure into scene
            LoadStructureData(saveData);

            Debug.Log($"[LOAD] Structure loaded from: {filePath}");
            Debug.Log($"[LOAD] Nodes: {saveData.nodes.Count}, Edges: {saveData.edges.Count}, Loads: {saveData.loads.Count}");

            HapticFeedback.Trigger(HapticFeedback.HapticType.Success);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[LOAD] Failed to load structure: {e.Message}");
            HapticFeedback.Trigger(HapticFeedback.HapticType.Error);
            return false;
        }
    }

    /// <summary>
    /// Create save data from current scene
    /// </summary>
    private StructureSaveData CreateSaveData(string structureName)
    {
        StructureSaveData saveData = new StructureSaveData();
        saveData.structureName = structureName;
        saveData.dateCreated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        // Find all nodes in scene
        NodeBehaviour[] allNodes = FindObjectsOfType<NodeBehaviour>();
        Dictionary<NodeBehaviour, int> nodeToId = new Dictionary<NodeBehaviour, int>();

        // Save nodes
        for (int i = 0; i < allNodes.Length; i++)
        {
            NodeBehaviour node = allNodes[i];
            nodeToId[node] = i;

            StructureSaveData.NodeData nodeData = new StructureSaveData.NodeData(
                i,
                node.transform.position,
                node.isSupport
            );

            saveData.nodes.Add(nodeData);
        }

        // Find all edges
        EdgeBehaviour[] allEdges = FindObjectsOfType<EdgeBehaviour>();
        HashSet<EdgeBehaviour> savedEdges = new HashSet<EdgeBehaviour>();

        // Save edges (avoid duplicates)
        foreach (var edge in allEdges)
        {
            if (edge == null || edge.nodeA == null || edge.nodeB == null) continue;
            if (savedEdges.Contains(edge)) continue;

            if (nodeToId.ContainsKey(edge.nodeA) && nodeToId.ContainsKey(edge.nodeB))
            {
                StructureSaveData.EdgeData edgeData = new StructureSaveData.EdgeData(
                    nodeToId[edge.nodeA],
                    nodeToId[edge.nodeB]
                );

                saveData.edges.Add(edgeData);
                savedEdges.Add(edge);
            }
        }

        // Save loads
        foreach (var node in allNodes)
        {
            if (node.loads != null)
            {
                foreach (var load in node.loads)
                {
                    if (load == null) continue;

                    StructureSaveData.LoadData loadData = new StructureSaveData.LoadData(
                        nodeToId[node],
                        load.direction,
                        load.magnitude
                    );

                    saveData.loads.Add(loadData);
                }
            }
        }

        return saveData;
    }

    /// <summary>
    /// Load structure data into scene
    /// </summary>
    private void LoadStructureData(StructureSaveData saveData)
    {
        if (graphManager == null)
        {
            Debug.LogError("[LOAD] GraphManager reference missing!");
            return;
        }

        Dictionary<int, NodeBehaviour> idToNode = new Dictionary<int, NodeBehaviour>();

        // Create nodes
        foreach (var nodeData in saveData.nodes)
        {
            NodeBehaviour node = graphManager.CreateNode(nodeData.GetPosition());
            node.isSupport = nodeData.isSupport;

            // Update visual based on support state
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

                // Add to node connection lists
                if (nodeA.connectedEdges == null)
                    nodeA.connectedEdges = new List<EdgeBehaviour>();
                if (nodeB.connectedEdges == null)
                    nodeB.connectedEdges = new List<EdgeBehaviour>();

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
                    node.loads = new List<LoadBehaviour>();

                node.loads.Add(load);
            }
        }
    }

    /// <summary>
    /// Clear all existing structures from scene
    /// </summary>
    private void ClearCurrentStructure()
    {
        // Delete all nodes (this will cascade to edges and loads)
        NodeBehaviour[] allNodes = FindObjectsOfType<NodeBehaviour>();
        foreach (var node in allNodes)
        {
            if (node != null)
            {
                // Delete loads
                if (node.loads != null)
                {
                    foreach (var load in node.loads)
                    {
                        if (load != null)
                            Destroy(load.gameObject);
                    }
                }

                // Delete edges
                if (node.connectedEdges != null)
                {
                    foreach (var edge in node.connectedEdges)
                    {
                        if (edge != null)
                            Destroy(edge.gameObject);
                    }
                }

                Destroy(node.gameObject);
            }
        }

        // Clean up any orphaned edges
        EdgeBehaviour[] allEdges = FindObjectsOfType<EdgeBehaviour>();
        foreach (var edge in allEdges)
        {
            if (edge != null)
                Destroy(edge.gameObject);
        }

        // Clean up any orphaned loads
        LoadBehaviour[] allLoads = FindObjectsOfType<LoadBehaviour>();
        foreach (var load in allLoads)
        {
            if (load != null)
                Destroy(load.gameObject);
        }
    }

    /// <summary>
    /// Get list of all saved structure files
    /// </summary>
    public List<string> GetSavedStructures()
    {
        List<string> files = new List<string>();

        if (Directory.Exists(SavePath))
        {
            string[] filePaths = Directory.GetFiles(SavePath, "*.json");
            foreach (string path in filePaths)
            {
                files.Add(Path.GetFileNameWithoutExtension(path));
            }
        }

        return files;
    }

    /// <summary>
    /// Delete a saved structure file
    /// </summary>
    public bool DeleteStructure(string fileName)
    {
        try
        {
            if (!fileName.EndsWith(".json"))
                fileName += ".json";

            string filePath = Path.Combine(SavePath, fileName);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                Debug.Log($"[DELETE] Deleted structure: {filePath}");
                HapticFeedback.Trigger(HapticFeedback.HapticType.Medium);
                return true;
            }

            return false;
        }
        catch (Exception e)
        {
            Debug.LogError($"[DELETE] Failed to delete structure: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Get full path to save directory (for debugging)
    /// </summary>
    public string GetSaveDirectoryPath()
    {
        return SavePath;
    }
}
