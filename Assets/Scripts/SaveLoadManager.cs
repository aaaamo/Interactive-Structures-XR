using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using Unity.Netcode;

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
    public void LoadStructure(string fileName, Action<bool> onComplete = null)
    {
        StartCoroutine(LoadStructureCoroutine(fileName, onComplete));
    }

    private IEnumerator LoadStructureCoroutine(string fileName, Action<bool> onComplete)
    {
        // Ensure .json extension
        if (!fileName.EndsWith(".json"))
            fileName += ".json";

        string json = null;
        string persistentPath = Path.Combine(SavePath, fileName);

        // 1. Try persistent data path first (User saved files)
        if (File.Exists(persistentPath))
        {
            json = File.ReadAllText(persistentPath);
            Debug.Log($"[LOAD] Loaded from persistent path: {persistentPath}");
        }
        else
        {
            // 2. Try StreamingAssets (Works on Android/Quest inside APK)
            string streamingPath = Path.Combine(Application.streamingAssetsPath, fileName);

            // On Android, path is jar:file://..., on PC it needs file:// prefix for UnityWebRequest
            if (!streamingPath.Contains("://"))
            {
                streamingPath = "file://" + streamingPath;
            }

            using (UnityWebRequest www = UnityWebRequest.Get(streamingPath))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    json = www.downloadHandler.text;
                    Debug.Log($"[LOAD] Loaded from StreamingAssets: {streamingPath}");
                }
                else
                {
                    Debug.LogWarning($"[LOAD] Not found in StreamingAssets: {streamingPath}. Error: {www.error}");
                }
            }
        }

        if (string.IsNullOrEmpty(json))
        {
            Debug.LogError($"[LOAD] File not found: {fileName}");
            HapticFeedback.Trigger(HapticFeedback.HapticType.Error);
            onComplete?.Invoke(false);
            yield break;
        }

        try
        {
            // Parse JSON
            StructureSaveData saveData = JsonUtility.FromJson<StructureSaveData>(json);

            // Clear existing structure - Handled by Server in LoadStructureData
            // ClearCurrentStructure(); 

            // Load structure into scene
            LoadStructureData(json);

            Debug.Log($"[LOAD] Structure loaded successfully");
            Debug.Log($"[LOAD] Nodes: {saveData.nodes.Count}, Edges: {saveData.edges.Count}, Loads: {saveData.loads.Count}");

            HapticFeedback.Trigger(HapticFeedback.HapticType.Success);
            onComplete?.Invoke(true);
        }
        catch (Exception e)
        {
            Debug.LogError($"[LOAD] Failed to load structure: {e.Message}");
            HapticFeedback.Trigger(HapticFeedback.HapticType.Error);
            onComplete?.Invoke(false);
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
        NodeBehaviour[] allNodes = FindObjectsByType<NodeBehaviour>(FindObjectsSortMode.None);
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
        EdgeBehaviour[] allEdges = FindObjectsByType<EdgeBehaviour>(FindObjectsSortMode.None);
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
    private void LoadStructureData(string json)
    {
        if (graphManager == null)
        {
            Debug.LogError("[LOAD] GraphManager reference missing!");
            return;
        }

        // Send the raw JSON to the server to reconstruct the scene properly
        graphManager.LoadStructureServerRpc(json);
    }

    /// <summary>
    /// Clear all existing structures from scene
    /// </summary>
    private void ClearCurrentStructure()
    {
        // In networked mode, we should ask server to despawn everything
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            var nodes = FindObjectsByType<NodeBehaviour>(FindObjectsSortMode.None);
            foreach (var node in nodes) node.GetComponent<NetworkObject>().Despawn();

            var edges = FindObjectsByType<EdgeBehaviour>(FindObjectsSortMode.None);
            foreach (var edge in edges) edge.GetComponent<NetworkObject>().Despawn();

            // Loads are children of nodes, usually despawned with nodes, but if separate:
            var loads = FindObjectsByType<LoadBehaviour>(FindObjectsSortMode.None);
            foreach (var load in loads) load.GetComponent<NetworkObject>().Despawn();

            return;
        }

        // Delete all nodes (this will cascade to edges and loads)
        NodeBehaviour[] allNodes = FindObjectsByType<NodeBehaviour>(FindObjectsSortMode.None);
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
        EdgeBehaviour[] allEdges = FindObjectsByType<EdgeBehaviour>(FindObjectsSortMode.None);
        foreach (var edge in allEdges)
        {
            if (edge != null)
                Destroy(edge.gameObject);
        }

        // Clean up any orphaned loads
        LoadBehaviour[] allLoads = FindObjectsByType<LoadBehaviour>(FindObjectsSortMode.None);
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
