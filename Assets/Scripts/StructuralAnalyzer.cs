//using System.Collections.Generic;
//using UnityEngine;
//using TMPro;

//public class StructuralAnalyzer : MonoBehaviour
//{
//    [Header("References")]
//    public GraphManager graphManager;
//    public TextMeshPro resultsDisplay;

//    [Header("Material Properties")]
//    public float youngModulus = 200e9f; // Steel: 200 GPa
//    public float crossSectionalArea = 0.01f; // 10 cm²

//    public void PerformAnalysis()
//    {
//        if (graphManager == null)
//        {
//            Debug.LogError("GraphManager reference missing!");
//            return;
//        }

//        NodeBehaviour[] allNodes = FindObjectsOfType<NodeBehaviour>();
//        EdgeBehaviour[] allEdges = FindObjectsOfType<EdgeBehaviour>();

//        if (allNodes.Length == 0)
//        {
//            DisplayResults("No structure to analyze!");
//            return;
//        }

//        StructureData data = BuildStructureData(allNodes, allEdges);
//        TrussAnalysisResult result = TrussAnalyzer.AnalyzeTruss(data, youngModulus, crossSectionalArea);
//        DisplayAnalysisResults(result, data);
//        VisualizeForces(result, data);
//    }

//    StructureData BuildStructureData(NodeBehaviour[] nodes, EdgeBehaviour[] edges)
//    {
//        StructureData data = new StructureData();
//        data.nodeIndexMap = new Dictionary<NodeBehaviour, int>();
//        data.nodes = new List<NodeBehaviour>();

//        int idx = 0;
//        foreach (var node in nodes)
//        {
//            if (node != null)
//            {
//                data.nodeIndexMap[node] = idx++;
//                data.nodes.Add(node);
//            }
//        }

//        data.edges = new List<EdgeBehaviour>();
//        data.adjacency = new List<List<int>>();
//        for (int i = 0; i < data.nodes.Count; i++)
//            data.adjacency.Add(new List<int>());

//        foreach (var edge in edges)
//        {
//            if (edge != null && edge.nodeA != null && edge.nodeB != null)
//            {
//                data.edges.Add(edge);
//                int idxA = data.nodeIndexMap[edge.nodeA];
//                int idxB = data.nodeIndexMap[edge.nodeB];
//                data.adjacency[idxA].Add(idxB);
//                data.adjacency[idxB].Add(idxA);
//            }
//        }

//        data.supportNodes = new List<int>();
//        for (int i = 0; i < data.nodes.Count; i++)
//        {
//            if (data.nodes[i].isSupport)
//                data.supportNodes.Add(i);
//        }

//        data.nodeLoads = new Dictionary<int, Vector3>();
//        for (int i = 0; i < data.nodes.Count; i++)
//        {
//            Vector3 totalLoad = Vector3.zero;
//            if (data.nodes[i].loads != null)
//            {
//                foreach (var load in data.nodes[i].loads)
//                {
//                    if (load != null)
//                        totalLoad += load.GetForceVector();
//                }
//            }
//            if (totalLoad.magnitude > 0.001f)
//                data.nodeLoads[i] = totalLoad;
//        }

//        return data;
//    }

//    void DisplayAnalysisResults(TrussAnalysisResult result, StructureData data)
//    {
//        if (resultsDisplay == null) return;

//        string output = "=== STRUCTURAL ANALYSIS ===\n\n";
//        output += $"Nodes: {data.nodes.Count}\n";
//        output += $"Members: {data.edges.Count}\n";
//        output += $"Supports: {data.supportNodes.Count}\n\n";

//        if (result.errorMessage != null)
//        {
//            output += $"ERROR: {result.errorMessage}\n";
//            resultsDisplay.text = output;
//            return;
//        }

//        output += "=== NODE TOTAL FORCES ===\n";
//        for (int i = 0; i < data.nodes.Count; i++)
//        {
//            Vector3 totalForce = Vector3.zero;
//            foreach (var load in data.nodes[i].loads)
//            {
//                if (load != null)
//                    totalForce += load.GetForceVector();
//            }
//            output += $"Node {i}: ({totalForce.x:F2}, {totalForce.y:F2}, {totalForce.z:F2}) N\n";
//        }

//        output += "=== MEMBER FORCES ===\n";
//        for (int i = 0; i < data.edges.Count && i < result.memberForces.Length; i++)
//        {
//            float force = result.memberForces[i];
//            string type = force > 0 ? "TENSION" : "COMPRESSION";
//            output += $"Member {i}: {force:F2} N ({type})\n";
//        }

//        output += "\n=== REACTIONS ===\n";
//        foreach (var kvp in result.reactions)
//        {
//            output += $"Node {kvp.Key}: ({kvp.Value.x:F2}, {kvp.Value.y:F2}, {kvp.Value.z:F2}) N\n";
//        }

//        resultsDisplay.text = output;
//    }

//    void VisualizeForces(TrussAnalysisResult result, StructureData data)
//    {
//        float maxForce = 0f;
//        if (result.memberForces == null) return;

//        foreach (float f in result.memberForces)
//        {
//            maxForce = Mathf.Max(maxForce, Mathf.Abs(f));
//        }
//        if (maxForce < 0.001f) maxForce = 1f;

//        for (int i = 0; i < data.edges.Count && i < result.memberForces.Length; i++)
//        {
//            EdgeBehaviour edge = data.edges[i];
//            float force = result.memberForces[i];
//            Renderer rend = edge.GetComponent<Renderer>();
//            if (rend != null)
//            {
//                float normalized = Mathf.Abs(force) / maxForce;
//                if (force > 0)
//                    rend.material.color = Color.Lerp(Color.white, Color.red, normalized);
//                else
//                    rend.material.color = Color.Lerp(Color.white, Color.blue, normalized);
//            }
//        }
//    }

//    void DisplayResults(string message)
//    {
//        if (resultsDisplay != null)
//            resultsDisplay.text = message;
//    }
//}

//public class StructureData
//{
//    public List<NodeBehaviour> nodes;
//    public List<EdgeBehaviour> edges;
//    public Dictionary<NodeBehaviour, int> nodeIndexMap;
//    public List<List<int>> adjacency;
//    public List<int> supportNodes;
//    public Dictionary<int, Vector3> nodeLoads;
//}

//public class TrussAnalysisResult
//{
//    public float[] memberForces;
//    public Dictionary<int, Vector3> reactions;
//    public string errorMessage;
//}


using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class StructuralAnalyzer : MonoBehaviour
{
    [Header("References")]
    public GraphManager graphManager;
    public TextMeshPro resultsDisplay;

    [Header("Material Properties")]
    public float youngModulus = 200e9f; // Steel: 200 GPa
    public float crossSectionalArea = 0.01f; // 10 cm²

    public void PerformAnalysis()
    {
        if (graphManager == null)
        {
            Debug.LogError("GraphManager reference missing!");
            return;
        }

        NodeBehaviour[] allNodes = FindObjectsOfType<NodeBehaviour>();
        EdgeBehaviour[] allEdges = FindObjectsOfType<EdgeBehaviour>();

        if (allNodes.Length == 0)
        {
            DisplayResults("No structure to analyze!");
            return;
        }

        // Find all independent subgraphs
        List<StructureData> subgraphs = FindIndependentSubgraphs(allNodes, allEdges);

        if (subgraphs.Count == 0)
        {
            DisplayResults("No connected structures found!");
            return;
        }

        // Analyze each subgraph independently
        List<SubgraphAnalysisResult> results = new List<SubgraphAnalysisResult>();
        for (int i = 0; i < subgraphs.Count; i++)
        {
            StructureData data = subgraphs[i];
            TrussAnalysisResult result = TrussAnalyzer.AnalyzeTruss(data, youngModulus, crossSectionalArea);
            results.Add(new SubgraphAnalysisResult
            {
                subgraphIndex = i,
                data = data,
                result = result
            });
        }

        // Display and visualize all results
        DisplayAllAnalysisResults(results);
        VisualizeAllForces(results);
    }

    List<StructureData> FindIndependentSubgraphs(NodeBehaviour[] allNodes, EdgeBehaviour[] allEdges)
    {
        List<StructureData> subgraphs = new List<StructureData>();
        HashSet<NodeBehaviour> visited = new HashSet<NodeBehaviour>();

        foreach (var startNode in allNodes)
        {
            if (startNode == null || visited.Contains(startNode))
                continue;

            // BFS to find all connected nodes
            HashSet<NodeBehaviour> subgraphNodes = new HashSet<NodeBehaviour>();
            Queue<NodeBehaviour> queue = new Queue<NodeBehaviour>();
            queue.Enqueue(startNode);
            visited.Add(startNode);
            subgraphNodes.Add(startNode);

            while (queue.Count > 0)
            {
                NodeBehaviour node = queue.Dequeue();

                if (node.connectedEdges != null)
                {
                    foreach (EdgeBehaviour edge in node.connectedEdges)
                    {
                        if (edge == null) continue;

                        NodeBehaviour other = edge.nodeA == node ? edge.nodeB : edge.nodeA;
                        if (other != null && !visited.Contains(other))
                        {
                            visited.Add(other);
                            subgraphNodes.Add(other);
                            queue.Enqueue(other);
                        }
                    }
                }
            }

            // Build StructureData for this subgraph
            List<NodeBehaviour> nodesList = new List<NodeBehaviour>(subgraphNodes);
            List<EdgeBehaviour> edgesList = new List<EdgeBehaviour>();

            // Find edges that belong to this subgraph
            foreach (var edge in allEdges)
            {
                if (edge != null &&
                    edge.nodeA != null && edge.nodeB != null &&
                    subgraphNodes.Contains(edge.nodeA) &&
                    subgraphNodes.Contains(edge.nodeB))
                {
                    edgesList.Add(edge);
                }
            }

            // Only add subgraphs with at least one node
            if (nodesList.Count > 0)
            {
                StructureData data = BuildStructureData(nodesList.ToArray(), edgesList.ToArray());
                subgraphs.Add(data);
            }
        }

        return subgraphs;
    }

    StructureData BuildStructureData(NodeBehaviour[] nodes, EdgeBehaviour[] edges)
    {
        StructureData data = new StructureData();
        data.nodeIndexMap = new Dictionary<NodeBehaviour, int>();
        data.nodes = new List<NodeBehaviour>();

        int idx = 0;
        foreach (var node in nodes)
        {
            if (node != null)
            {
                data.nodeIndexMap[node] = idx++;
                data.nodes.Add(node);
            }
        }

        data.edges = new List<EdgeBehaviour>();
        data.adjacency = new List<List<int>>();
        for (int i = 0; i < data.nodes.Count; i++)
            data.adjacency.Add(new List<int>());

        foreach (var edge in edges)
        {
            if (edge != null && edge.nodeA != null && edge.nodeB != null)
            {
                // Only add edge if both nodes are in this subgraph
                if (data.nodeIndexMap.ContainsKey(edge.nodeA) && data.nodeIndexMap.ContainsKey(edge.nodeB))
                {
                    data.edges.Add(edge);
                    int idxA = data.nodeIndexMap[edge.nodeA];
                    int idxB = data.nodeIndexMap[edge.nodeB];
                    data.adjacency[idxA].Add(idxB);
                    data.adjacency[idxB].Add(idxA);
                }
            }
        }

        data.supportNodes = new List<int>();
        for (int i = 0; i < data.nodes.Count; i++)
        {
            if (data.nodes[i].isSupport)
                data.supportNodes.Add(i);
        }

        data.nodeLoads = new Dictionary<int, Vector3>();
        for (int i = 0; i < data.nodes.Count; i++)
        {
            Vector3 totalLoad = Vector3.zero;
            if (data.nodes[i].loads != null)
            {
                foreach (var load in data.nodes[i].loads)
                {
                    if (load != null)
                        totalLoad += load.GetForceVector();
                }
            }
            if (totalLoad.magnitude > 0.001f)
                data.nodeLoads[i] = totalLoad;
        }

        return data;
    }

    void DisplayAllAnalysisResults(List<SubgraphAnalysisResult> results)
    {
        if (resultsDisplay == null) return;

        string output = "=== STRUCTURAL ANALYSIS ===\n\n";
        output += $"Total Independent Structures: {results.Count}\n\n";

        for (int s = 0; s < results.Count; s++)
        {
            var subResult = results[s];
            StructureData data = subResult.data;
            TrussAnalysisResult result = subResult.result;

            output += $"========== STRUCTURE {s + 1} ==========\n";
            output += $"Nodes: {data.nodes.Count}\n";
            output += $"Members: {data.edges.Count}\n";
            output += $"Supports: {data.supportNodes.Count}\n\n";

            if (result.errorMessage != null)
            {
                output += $"ERROR: {result.errorMessage}\n\n";
                continue;
            }

            output += "--- NODE FORCES ---\n";
            for (int i = 0; i < data.nodes.Count; i++)
            {
                Vector3 totalForce = Vector3.zero;
                foreach (var load in data.nodes[i].loads)
                {
                    if (load != null)
                        totalForce += load.GetForceVector();
                }
                if (totalForce.magnitude > 0.001f)
                {
                    output += $"N{i}: ({totalForce.x:F2}, {totalForce.y:F2}, {totalForce.z:F2}) N\n";
                }
            }

            output += "\n--- MEMBER FORCES ---\n";
            for (int i = 0; i < data.edges.Count && i < result.memberForces.Length; i++)
            {
                float force = result.memberForces[i];
                string type = force > 0 ? "T" : "C"; // Tension or Compression
                output += $"M{i}: {force:F2} N ({type})\n";
            }

            output += "\n--- REACTIONS ---\n";
            foreach (var kvp in result.reactions)
            {
                output += $"N{kvp.Key}: ({kvp.Value.x:F2}, {kvp.Value.y:F2}, {kvp.Value.z:F2}) N\n";
            }

            output += "\n";
        }

        resultsDisplay.text = output;
    }

    void VisualizeAllForces(List<SubgraphAnalysisResult> results)
    {
        foreach (var subResult in results)
        {
            float subMaxForce= 0f;
            if (subResult.result.memberForces != null)
            {
                foreach (float f in subResult.result.memberForces)
                {
                    subMaxForce = Mathf.Max(subMaxForce, Mathf.Abs(f));
                }
            }
            if (subMaxForce < 0.001f) subMaxForce = 1f;
            VisualizeForces(subResult.result, subResult.data, subMaxForce);
        }
    }

    void VisualizeForces(TrussAnalysisResult result, StructureData data, float maxForce)
    {
        if (result.memberForces == null) return;

        for (int i = 0; i < data.edges.Count && i < result.memberForces.Length; i++)
        {
            EdgeBehaviour edge = data.edges[i];
            float force = result.memberForces[i];
            Renderer rend = edge.GetComponent<Renderer>();
            if (rend != null)
            {
                float normalized = Mathf.Abs(force) / maxForce;
                if (force > 0)
                    rend.material.color = Color.Lerp(Color.white, Color.red, normalized);
                else
                    rend.material.color = Color.Lerp(Color.white, Color.blue, normalized);
            }
        }
    }

    void DisplayResults(string message)
    {
        if (resultsDisplay != null)
            resultsDisplay.text = message;
    }
}

public class StructureData
{
    public List<NodeBehaviour> nodes;
    public List<EdgeBehaviour> edges;
    public Dictionary<NodeBehaviour, int> nodeIndexMap;
    public List<List<int>> adjacency;
    public List<int> supportNodes;
    public Dictionary<int, Vector3> nodeLoads;
}

public class TrussAnalysisResult
{
    public float[] memberForces;
    public Dictionary<int, Vector3> reactions;
    public string errorMessage;
}

public class SubgraphAnalysisResult
{
    public int subgraphIndex;
    public StructureData data;
    public TrussAnalysisResult result;
}