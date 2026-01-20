using UnityEngine;
using TMPro;

/// <summary>
/// Quick debug script to test if visual feedback is working
/// Press keys to manually test highlighting
/// </summary>
public class DebugUITest : MonoBehaviour
{
    [Header("Test Settings")]
    public bool enableDebugMode = true;

    void Update()
    {
        if (!enableDebugMode) return;

        // Test 1: Highlight all nodes in green when pressing 1
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Debug.Log("[DEBUG] Testing node highlights (GREEN)");
            NodeBehaviour[] nodes = FindObjectsByType<NodeBehaviour>(FindObjectsSortMode.None);
            foreach (var node in nodes)
            {
                SimpleHighlight highlight = node.gameObject.GetComponent<SimpleHighlight>();
                if (highlight == null)
                {
                    highlight = node.gameObject.AddComponent<SimpleHighlight>();
                }
                highlight.Highlight(Color.green);
            }
            Debug.Log($"[DEBUG] Highlighted {nodes.Length} nodes");
        }

        // Test 2: Clear all highlights when pressing 2
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Debug.Log("[DEBUG] Clearing all highlights");
            NodeBehaviour[] nodes = FindObjectsByType<NodeBehaviour>(FindObjectsSortMode.None);
            foreach (var node in nodes)
            {
                SimpleHighlight highlight = node.gameObject.GetComponent<SimpleHighlight>();
                if (highlight != null)
                {
                    highlight.ClearHighlight();
                }
            }
            Debug.Log($"[DEBUG] Cleared {nodes.Length} nodes");
        }

        // Test 3: Test VisualFeedbackManager when pressing 3
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            Debug.Log("[DEBUG] Testing VisualFeedbackManager");
            if (VisualFeedbackManager.Instance == null)
            {
                Debug.LogError("[DEBUG] VisualFeedbackManager.Instance is NULL!");
            }
            else
            {
                Debug.Log("[DEBUG] VisualFeedbackManager found!");
                NodeBehaviour[] nodes = FindObjectsByType<NodeBehaviour>(FindObjectsSortMode.None);
                if (nodes.Length > 0)
                {
                    VisualFeedbackManager.Instance.HighlightHover(nodes[0].gameObject, Color.yellow);
                    Debug.Log($"[DEBUG] Highlighted first node via VisualFeedbackManager");
                }
                else
                {
                    Debug.LogWarning("[DEBUG] No nodes found in scene!");
                }
            }
        }

        // Test 4: Check if components exist when pressing 4
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            Debug.Log("[DEBUG] === COMPONENT CHECK ===");

            var saveLoad = FindObjectOfType<SaveLoadManager>();
            Debug.Log($"[DEBUG] SaveLoadManager: {(saveLoad != null ? "FOUND" : "NOT FOUND")}");

            var graphManager = FindObjectOfType<GraphManager>();
            Debug.Log($"[DEBUG] GraphManager: {(graphManager != null ? "FOUND" : "NOT FOUND")}");

            var visualFeedback = FindObjectOfType<VisualFeedbackManager>();
            Debug.Log($"[DEBUG] VisualFeedbackManager: {(visualFeedback != null ? "FOUND" : "NOT FOUND")}");

            var ovrController = FindObjectOfType<OVRGraphController>();
            Debug.Log($"[DEBUG] OVRGraphController: {(ovrController != null ? "FOUND" : "NOT FOUND")}");

            var unifiedTutorial = FindObjectOfType<UnifiedTutorialSystem>();
            Debug.Log($"[DEBUG] UnifiedTutorialSystem: {(unifiedTutorial != null ? "FOUND" : "NOT FOUND")}");

            NodeBehaviour[] nodes = FindObjectsByType<NodeBehaviour>(FindObjectsSortMode.None);
            Debug.Log($"[DEBUG] Nodes in scene: {nodes.Length}");

            EdgeBehaviour[] edges = FindObjectsByType<EdgeBehaviour>(FindObjectsSortMode.None);
            Debug.Log($"[DEBUG] Edges in scene: {edges.Length}");

            Debug.Log("[DEBUG] === END CHECK ===");
        }

        // Test 5: Manual create simple structure when pressing 5
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            Debug.Log("[DEBUG] Creating test structure");
            CreateTestStructure();
        }
    }

    void CreateTestStructure()
    {
        GraphManager gm = FindObjectOfType<GraphManager>();
        if (gm == null)
        {
            Debug.LogError("[DEBUG] GraphManager not found!");
            return;
        }

        // Create 3 nodes in a triangle
        Vector3 pos1 = new Vector3(0f, 0.5f, 0f);
        Vector3 pos2 = new Vector3(0.3f, 0.5f, 0f);
        Vector3 pos3 = new Vector3(0.15f, 0.8f, 0f);

        NodeBehaviour n1 = gm.CreateNode(pos1);
        NodeBehaviour n2 = gm.CreateNode(pos2);
        NodeBehaviour n3 = gm.CreateNode(pos3);

        n1.isSupport = true;
        n1.freeVisual.SetActive(false);
        n1.supportVisual.SetActive(true);

        n2.isSupport = true;
        n2.freeVisual.SetActive(false);
        n2.supportVisual.SetActive(true);

        // Create edges
        EdgeBehaviour e1 = gm.CreateEdge(n1);
        e1.nodeB = n2;
        n1.connectedEdges.Add(e1);
        n2.connectedEdges.Add(e1);
        e1.UpdateEdgePosition();

        EdgeBehaviour e2 = gm.CreateEdge(n2);
        e2.nodeB = n3;
        n2.connectedEdges.Add(e2);
        n3.connectedEdges.Add(e2);
        e2.UpdateEdgePosition();

        EdgeBehaviour e3 = gm.CreateEdge(n3);
        e3.nodeB = n1;
        n3.connectedEdges.Add(e3);
        n1.connectedEdges.Add(e3);
        e3.UpdateEdgePosition();

        // Add load
        LoadBehaviour load = gm.CreateLoad(n3, Vector3.down, 1000f);
        n3.loads.Add(load);

        Debug.Log("[DEBUG] Created triangle structure with 3 nodes, 3 edges, 1 load");
    }

    void OnGUI()
    {
        if (!enableDebugMode) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 500));
        GUILayout.Label("=== DEBUG CONTROLS ===");
        GUILayout.Label("1: Highlight all nodes GREEN");
        GUILayout.Label("2: Clear all highlights");
        GUILayout.Label("3: Test VisualFeedbackManager");
        GUILayout.Label("4: Check components");
        GUILayout.Label("5: Create test structure");
        GUILayout.Label("");

        if (GUILayout.Button("Highlight Nodes"))
        {
            NodeBehaviour[] nodes = FindObjectsByType<NodeBehaviour>(FindObjectsSortMode.None);
            foreach (var node in nodes)
            {
                SimpleHighlight highlight = node.gameObject.GetComponent<SimpleHighlight>();
                if (highlight == null)
                    highlight = node.gameObject.AddComponent<SimpleHighlight>();
                highlight.Highlight(Color.green);
            }
        }

        if (GUILayout.Button("Clear Highlights"))
        {
            NodeBehaviour[] nodes = FindObjectsByType<NodeBehaviour>(FindObjectsSortMode.None);
            foreach (var node in nodes)
            {
                SimpleHighlight highlight = node.gameObject.GetComponent<SimpleHighlight>();
                if (highlight != null)
                    highlight.ClearHighlight();
            }
        }

        if (GUILayout.Button("Create Test Structure"))
        {
            CreateTestStructure();
        }

        GUILayout.EndArea();
    }
}
