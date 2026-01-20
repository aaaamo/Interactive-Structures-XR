//using System;
//using System.Collections.Generic;
//using System.IO;
//using UnityEngine;

///// <summary>
///// Manages saving and loading structures to/from JSON files
///// </summary>
//public class SaveLoadManager : MonoBehaviour
//{
//    [Header("References")]
//    public GraphManager graphManager;

//    [Header("Settings")]
//    public string saveDirectory = "StructureSaves";
//    public string defaultFileName = "MyStructure";

//    private string SavePath
//    {
//        get
//        {
//            string path = Path.Combine(Application.persistentDataPath, saveDirectory);
//            if (!Directory.Exists(path))
//                Directory.CreateDirectory(path);
//            return path;
//        }
//    }

//    // Singleton
//    private static SaveLoadManager _instance;
//    public static SaveLoadManager Instance
//    {
//        get
//        {
//            if (_instance == null)
//                _instance = FindObjectOfType<SaveLoadManager>();
//            return _instance;
//        }
//    }

//    void Awake()
//    {
//        if (_instance == null)
//            _instance = this;
//        else if (_instance != this)
//            Destroy(gameObject);
//    }

//    /// <summary>
//    /// Save current structure to file
//    /// </summary>
//    public bool SaveStructure(string fileName = null)
//    {
//        try
//        {
//            if (string.IsNullOrEmpty(fileName))
//                fileName = defaultFileName;

//            // Ensure .json extension
//            if (!fileName.EndsWith(".json"))
//                fileName += ".json";

//            // Create save data
//            StructureSaveData saveData = CreateSaveData(fileName);

//            // Convert to JSON
//            string json = JsonUtility.ToJson(saveData, true);

//            // Write to file
//            string filePath = Path.Combine(SavePath, fileName);
//            File.WriteAllText(filePath, json);

//            Debug.Log($"[SAVE] Structure saved to: {filePath}");
//            Debug.Log($"[SAVE] Nodes: {saveData.nodes.Count}, Edges: {saveData.edges.Count}, Loads: {saveData.loads.Count}");

//            HapticFeedback.Trigger(HapticFeedback.HapticType.Success);
//            return true;
//        }
//        catch (Exception e)
//        {
//            Debug.LogError($"[SAVE] Failed to save structure: {e.Message}");
//            HapticFeedback.Trigger(HapticFeedback.HapticType.Error);
//            return false;
//        }
//    }

//    /// <summary>
//    /// Load structure from file
//    /// </summary>
//    public bool LoadStructure(string fileName)
//    {
//        try
//        {
//            // Ensure .json extension
//            if (!fileName.EndsWith(".json"))
//                fileName += ".json";

//            // Try persistent data path first
//            string filePath = Path.Combine(SavePath, fileName);

//            // If not found, try the project's SavedStructures folder (for bundled examples)
//            if (!File.Exists(filePath))
//            {
//                Debug.Log($"[LOAD] Not in persistent path, checking project folder...");
//                string projectPath = Path.Combine(Application.dataPath, "..", "SavedStructures", fileName);
//                Debug.Log($"[LOAD] Checking: {projectPath}");
//                if (File.Exists(projectPath))
//                {
//                    filePath = projectPath;
//                    Debug.Log($"[LOAD] Found file in project folder!");
//                }
//            }

//            if (!File.Exists(filePath))
//            {
//                Debug.LogError($"[LOAD] File not found: {filePath}");
//                HapticFeedback.Trigger(HapticFeedback.HapticType.Error);
//                return false;
//            }

//            // Read file
//            string json = File.ReadAllText(filePath);

//            // Parse JSON
//            StructureSaveData saveData = JsonUtility.FromJson<StructureSaveData>(json);

//            // Clear existing structure
//            ClearCurrentStructure();

//            // Load structure into scene
//            LoadStructureData(saveData);

//            Debug.Log($"[LOAD] Structure loaded from: {filePath}");
//            Debug.Log($"[LOAD] Nodes: {saveData.nodes.Count}, Edges: {saveData.edges.Count}, Loads: {saveData.loads.Count}");

//            HapticFeedback.Trigger(HapticFeedback.HapticType.Success);
//            return true;
//        }
//        catch (Exception e)
//        {
//            Debug.LogError($"[LOAD] Failed to load structure: {e.Message}");
//            HapticFeedback.Trigger(HapticFeedback.HapticType.Error);
//            return false;
//        }
//    }

//    /// <summary>
//    /// Create save data from current scene
//    /// </summary>
//    private StructureSaveData CreateSaveData(string structureName)
//    {
//        StructureSaveData saveData = new StructureSaveData();
//        saveData.structureName = structureName;
//        saveData.dateCreated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

//        // Find all nodes in scene
//        NodeBehaviour[] allNodes = FindObjectsByType<NodeBehaviour>(FindObjectsSortMode.None);
//        Dictionary<NodeBehaviour, int> nodeToId = new Dictionary<NodeBehaviour, int>();

//        // Save nodes
//        for (int i = 0; i < allNodes.Length; i++)
//        {
//            NodeBehaviour node = allNodes[i];
//            nodeToId[node] = i;

//            StructureSaveData.NodeData nodeData = new StructureSaveData.NodeData(
//                i,
//                node.transform.position,
//                node.isSupport
//            );

//            saveData.nodes.Add(nodeData);
//        }

//        // Find all edges
//        EdgeBehaviour[] allEdges = FindObjectsByType<EdgeBehaviour>(FindObjectsSortMode.None);
//        HashSet<EdgeBehaviour> savedEdges = new HashSet<EdgeBehaviour>();

//        // Save edges (avoid duplicates)
//        foreach (var edge in allEdges)
//        {
//            if (edge == null || edge.nodeA == null || edge.nodeB == null) continue;
//            if (savedEdges.Contains(edge)) continue;

//            if (nodeToId.ContainsKey(edge.nodeA) && nodeToId.ContainsKey(edge.nodeB))
//            {
//                StructureSaveData.EdgeData edgeData = new StructureSaveData.EdgeData(
//                    nodeToId[edge.nodeA],
//                    nodeToId[edge.nodeB]
//                );

//                saveData.edges.Add(edgeData);
//                savedEdges.Add(edge);
//            }
//        }

//        // Save loads
//        foreach (var node in allNodes)
//        {
//            if (node.loads != null)
//            {
//                foreach (var load in node.loads)
//                {
//                    if (load == null) continue;

//                    StructureSaveData.LoadData loadData = new StructureSaveData.LoadData(
//                        nodeToId[node],
//                        load.direction,
//                        load.magnitude
//                    );

//                    saveData.loads.Add(loadData);
//                }
//            }
//        }

//        return saveData;
//    }

//    /// <summary>
//    /// Load structure data into scene
//    /// </summary>
//    private void LoadStructureData(StructureSaveData saveData)
//    {
//        if (graphManager == null)
//        {
//            Debug.LogError("[LOAD] GraphManager reference missing!");
//            return;
//        }

//        Dictionary<int, NodeBehaviour> idToNode = new Dictionary<int, NodeBehaviour>();

//        // Create nodes
//        foreach (var nodeData in saveData.nodes)
//        {
//            NodeBehaviour node = graphManager.CreateNode(nodeData.GetPosition());
//            if (node == null) continue;

//            node.isSupport = nodeData.isSupport;

//            // Update visual based on support state
//            if (node.isSupport)
//            {
//                node.freeVisual?.SetActive(false);
//                node.supportVisual?.SetActive(true);
//            }
//            else
//            {
//                node.freeVisual?.SetActive(true);
//                node.supportVisual?.SetActive(false);
//            }

//            idToNode[nodeData.id] = node;
//        }

//        // Create edges
//        foreach (var edgeData in saveData.edges)
//        {
//            if (idToNode.ContainsKey(edgeData.nodeAId) && idToNode.ContainsKey(edgeData.nodeBId))
//            {
//                NodeBehaviour nodeA = idToNode[edgeData.nodeAId];
//                NodeBehaviour nodeB = idToNode[edgeData.nodeBId];
//                if (nodeA == null || nodeB == null) continue;

//                EdgeBehaviour edge = graphManager.CreateEdge(nodeA);
//                if (edge == null) continue;

//                edge.nodeB = nodeB;

//                // Add to node connection lists
//                if (nodeA.connectedEdges == null)
//                    nodeA.connectedEdges = new List<EdgeBehaviour>();
//                if (nodeB.connectedEdges == null)
//                    nodeB.connectedEdges = new List<EdgeBehaviour>();

//                nodeA.connectedEdges.Add(edge);
//                nodeB.connectedEdges.Add(edge);

//                edge.UpdateEdgePosition();
//            }
//        }

//        // Create loads
//        foreach (var loadData in saveData.loads)
//        {
//            if (idToNode.ContainsKey(loadData.nodeId))
//            {
//                NodeBehaviour node = idToNode[loadData.nodeId];
//                if (node == null) continue;

//                LoadBehaviour load = graphManager.CreateLoad(
//                    node,
//                    loadData.GetDirection(),
//                    loadData.magnitude
//                );
//                if (load == null) continue;

//                if (node.loads == null)
//                    node.loads = new List<LoadBehaviour>();

//                node.loads.Add(load);
//            }
//        }
//    }

//    /// <summary>
//    /// Clear all existing structures from scene
//    /// </summary>
//    private void ClearCurrentStructure()
//    {
//        // Delete all nodes (this will cascade to edges and loads)
//        NodeBehaviour[] allNodes = FindObjectsByType<NodeBehaviour>(FindObjectsSortMode.None);
//        foreach (var node in allNodes)
//        {
//            if (node != null)
//            {
//                // Delete loads
//                if (node.loads != null)
//                {
//                    foreach (var load in node.loads)
//                    {
//                        if (load != null)
//                            Destroy(load.gameObject);
//                    }
//                }

//                // Delete edges
//                if (node.connectedEdges != null)
//                {
//                    foreach (var edge in node.connectedEdges)
//                    {
//                        if (edge != null)
//                            Destroy(edge.gameObject);
//                    }
//                }

//                Destroy(node.gameObject);
//            }
//        }

//        // Clean up any orphaned edges
//        EdgeBehaviour[] allEdges = FindObjectsByType<EdgeBehaviour>(FindObjectsSortMode.None);
//        foreach (var edge in allEdges)
//        {
//            if (edge != null)
//                Destroy(edge.gameObject);
//        }

//        // Clean up any orphaned loads
//        LoadBehaviour[] allLoads = FindObjectsByType<LoadBehaviour>(FindObjectsSortMode.None);
//        foreach (var load in allLoads)
//        {
//            if (load != null)
//                Destroy(load.gameObject);
//        }
//    }

//    /// <summary>
//    /// Get list of all saved structure files
//    /// </summary>
//    public List<string> GetSavedStructures()
//    {
//        List<string> files = new List<string>();

//        if (Directory.Exists(SavePath))
//        {
//            string[] filePaths = Directory.GetFiles(SavePath, "*.json");
//            foreach (string path in filePaths)
//            {
//                files.Add(Path.GetFileNameWithoutExtension(path));
//            }
//        }

//        return files;
//    }

//    /// <summary>
//    /// Delete a saved structure file
//    /// </summary>
//    public bool DeleteStructure(string fileName)
//    {
//        try
//        {
//            if (!fileName.EndsWith(".json"))
//                fileName += ".json";

//            string filePath = Path.Combine(SavePath, fileName);

//            if (File.Exists(filePath))
//            {
//                File.Delete(filePath);
//                Debug.Log($"[DELETE] Deleted structure: {filePath}");
//                HapticFeedback.Trigger(HapticFeedback.HapticType.Medium);
//                return true;
//            }

//            return false;
//        }
//        catch (Exception e)
//        {
//            Debug.LogError($"[DELETE] Failed to delete structure: {e.Message}");
//            return false;
//        }
//    }

//    /// <summary>
//    /// Get full path to save directory (for debugging)
//    /// </summary>
//    public string GetSaveDirectoryPath()
//    {
//        return SavePath;
//    }
//}


using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Manages saving and loading structures to/from JSON files.
/// Compatible with both Editor and Mobile (Android/iOS).
/// </summary>
public class SaveLoadManager : MonoBehaviour
{
    [Header("References")]
    public GraphManager graphManager;

    [Header("Settings")]
    public string saveDirectory = "StructureSaves";
    public string defaultFileName = "MyStructure";

    // This is where user-created saves go
    private string PersistentSavePath
    {
        get
        {
            string path = Path.Combine(Application.persistentDataPath, saveDirectory);
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return path;
        }
    }

    // Singleton Pattern
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
    /// Save current structure to persistent data path
    /// </summary>
    public bool SaveStructure(string fileName = null)
    {
        try
        {
            if (string.IsNullOrEmpty(fileName))
                fileName = defaultFileName;

            if (!fileName.EndsWith(".json"))
                fileName += ".json";

            StructureSaveData saveData = CreateSaveData(fileName);
            string json = JsonUtility.ToJson(saveData, true);

            string filePath = Path.Combine(PersistentSavePath, fileName);
            File.WriteAllText(filePath, json);

            Debug.Log($"[SAVE] Saved to: {filePath}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SAVE] Failed: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Load structure. Checks Persistent Data first, then StreamingAssets.
    /// </summary>
    public bool LoadStructure(string fileName)
    {
        if (!fileName.EndsWith(".json"))
            fileName += ".json";

        // 1. Try to find the file in Persistent Data (User Saves)
        string persistentPath = Path.Combine(PersistentSavePath, fileName);
        if (File.Exists(persistentPath))
        {
            Debug.Log($"[LOAD] Loading from Persistent path: {persistentPath}");
            string json = File.ReadAllText(persistentPath);
            return ParseAndApplyJson(json);
        }

        // 2. Try to find the file in StreamingAssets (Bundled Defaults)
        string streamingPath = Path.Combine(Application.streamingAssetsPath, fileName);
        Debug.Log($"[LOAD] Checking StreamingAssets: {streamingPath}");

        if (Application.platform == RuntimePlatform.Android)
        {
            // Android requires WebRequest for StreamingAssets
            UnityWebRequest www = UnityWebRequest.Get(streamingPath);
            www.SendWebRequest();

            // Synchronous wait (Blocking) - okay for small JSON files
            while (!www.isDone) { }

            if (www.result == UnityWebRequest.Result.Success)
            {
                return ParseAndApplyJson(www.downloadHandler.text);
            }
        }
        else if (File.Exists(streamingPath))
        {
            // PC/iOS/Editor can use standard File IO for StreamingAssets
            string json = File.ReadAllText(streamingPath);
            return ParseAndApplyJson(json);
        }

        Debug.LogError($"[LOAD] File not found in any location: {fileName}");
        return false;
    }

    private bool ParseAndApplyJson(string json)
    {
        try
        {
            StructureSaveData saveData = JsonUtility.FromJson<StructureSaveData>(json);

            ClearCurrentStructure();
            LoadStructureData(saveData);

            Debug.Log($"[LOAD] Success. Nodes: {saveData.nodes.Count}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[LOAD] JSON Error: {e.Message}");
            return false;
        }
    }

    private StructureSaveData CreateSaveData(string structureName)
    {
        StructureSaveData saveData = new StructureSaveData();
        saveData.structureName = structureName;
        saveData.dateCreated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        NodeBehaviour[] allNodes = FindObjectsByType<NodeBehaviour>(FindObjectsSortMode.None);
        Dictionary<NodeBehaviour, int> nodeToId = new Dictionary<NodeBehaviour, int>();

        for (int i = 0; i < allNodes.Length; i++)
        {
            NodeBehaviour node = allNodes[i];
            nodeToId[node] = i;
            saveData.nodes.Add(new StructureSaveData.NodeData(i, node.transform.position, node.isSupport));
        }

        EdgeBehaviour[] allEdges = FindObjectsByType<EdgeBehaviour>(FindObjectsSortMode.None);
        HashSet<EdgeBehaviour> processedEdges = new HashSet<EdgeBehaviour>();

        foreach (var edge in allEdges)
        {
            if (edge == null || edge.nodeA == null || edge.nodeB == null || processedEdges.Contains(edge)) continue;

            if (nodeToId.ContainsKey(edge.nodeA) && nodeToId.ContainsKey(edge.nodeB))
            {
                saveData.edges.Add(new StructureSaveData.EdgeData(nodeToId[edge.nodeA], nodeToId[edge.nodeB]));
                processedEdges.Add(edge);
            }
        }

        foreach (var node in allNodes)
        {
            if (node.loads == null) continue;
            foreach (var load in node.loads)
            {
                if (load == null) continue;
                saveData.loads.Add(new StructureSaveData.LoadData(nodeToId[node], load.direction, load.magnitude));
            }
        }

        return saveData;
    }

    private void LoadStructureData(StructureSaveData saveData)
    {
        if (graphManager == null) return;

        Dictionary<int, NodeBehaviour> idToNode = new Dictionary<int, NodeBehaviour>();

        foreach (var nodeData in saveData.nodes)
        {
            NodeBehaviour node = graphManager.CreateNode(nodeData.GetPosition());
            if (node == null) continue;

            node.isSupport = nodeData.isSupport;
            node.freeVisual?.SetActive(!node.isSupport);
            node.supportVisual?.SetActive(node.isSupport);
            idToNode[nodeData.id] = node;
        }

        foreach (var edgeData in saveData.edges)
        {
            if (idToNode.TryGetValue(edgeData.nodeAId, out NodeBehaviour nA) &&
                idToNode.TryGetValue(edgeData.nodeBId, out NodeBehaviour nB))
            {
                EdgeBehaviour edge = graphManager.CreateEdge(nA);
                if (edge == null) continue;
                edge.nodeB = nB;

                if (nA.connectedEdges == null) nA.connectedEdges = new List<EdgeBehaviour>();
                if (nB.connectedEdges == null) nB.connectedEdges = new List<EdgeBehaviour>();

                nA.connectedEdges.Add(edge);
                nB.connectedEdges.Add(edge);
                edge.UpdateEdgePosition();
            }
        }

        foreach (var loadData in saveData.loads)
        {
            if (idToNode.TryGetValue(loadData.nodeId, out NodeBehaviour node))
            {
                LoadBehaviour load = graphManager.CreateLoad(node, loadData.GetDirection(), loadData.magnitude);
                if (node.loads == null) node.loads = new List<LoadBehaviour>();
                node.loads.Add(load);
            }
        }
    }

    private void ClearCurrentStructure()
    {
        NodeBehaviour[] allNodes = FindObjectsByType<NodeBehaviour>(FindObjectsSortMode.None);
        foreach (var n in allNodes) if (n != null) Destroy(n.gameObject);

        EdgeBehaviour[] allEdges = FindObjectsByType<EdgeBehaviour>(FindObjectsSortMode.None);
        foreach (var e in allEdges) if (e != null) Destroy(e.gameObject);

        LoadBehaviour[] allLoads = FindObjectsByType<LoadBehaviour>(FindObjectsSortMode.None);
        foreach (var l in allLoads) if (l != null) Destroy(l.gameObject);
    }

    public List<string> GetSavedStructures()
    {
        List<string> files = new List<string>();
        if (Directory.Exists(PersistentSavePath))
        {
            foreach (string path in Directory.GetFiles(PersistentSavePath, "*.json"))
                files.Add(Path.GetFileNameWithoutExtension(path));
        }
        return files;
    }

    public bool DeleteStructure(string fileName)
    {
        if (!fileName.EndsWith(".json")) fileName += ".json";
        string filePath = Path.Combine(PersistentSavePath, fileName);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            return true;
        }
        return false;
    }
}