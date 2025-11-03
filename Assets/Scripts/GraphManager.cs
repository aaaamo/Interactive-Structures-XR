using UnityEngine;

public class GraphManager : MonoBehaviour
{
    public GameObject nodePrefab;
    public GameObject edgePrefab;
    public GameObject loadPrefab;

    public NodeBehaviour CreateNode(Vector3 position)
    {
        if (nodePrefab == null)
        {
            Debug.LogError("Node prefab not assigned!");
            return null;
        }

        GameObject obj = Instantiate(nodePrefab, position, Quaternion.identity);
        NodeBehaviour node = obj.GetComponent<NodeBehaviour>();
        if (node == null)
        {
            Debug.LogError("Node prefab missing NodeBehaviour script!");
            return null;
        }
        return node;
    }

    public EdgeBehaviour CreateEdge(NodeBehaviour nodeA)
    {
        if (edgePrefab == null)
        {
            Debug.LogError("Edge prefab not assigned!");
            return null;
        }

        GameObject edgeObj = Instantiate(edgePrefab, Vector3.zero, Quaternion.identity);
        EdgeBehaviour edge = edgeObj.GetComponent<EdgeBehaviour>();
        if (edge == null)
        {
            Debug.LogError("Edge prefab missing EdgeBehaviour script!");
            return null;
        }

        edge.nodeA = nodeA;
        edge.nodeB = null; // temporary edge
        return edge;
    }

    public LoadBehaviour CreateLoad(NodeBehaviour node, Vector3 direction, float magnitude)
    {
        if (loadPrefab == null)
        {
            Debug.LogError("Load prefab not assigned!");
            return null;
        }

        // Instantiate prefab at node position
        GameObject loadObj = Instantiate(loadPrefab, node.transform.position, Quaternion.identity);

        // Get LoadBehaviour
        LoadBehaviour load = loadObj.GetComponent<LoadBehaviour>();
        if (load == null)
        {
            Debug.LogError("Load prefab missing LoadBehaviour script!");
            return null;
        }

        // Parent it to the node (keeps world position)
        load.transform.SetParent(node.transform, true);

        // Ensure exact node alignment
        load.transform.localPosition = Vector3.zero;

        // Initialize properties
        load.node = node;
        load.direction = direction.normalized;
        load.magnitude = Mathf.Max(0.01f, magnitude);
        load.UpdateArrow();

        return load;
    }

    //public LoadBehaviour CreateLoad(NodeBehaviour node, Vector3 direction, float magnitude)
    //{
    //    if (loadPrefab == null)
    //    {
    //        Debug.LogError("Load prefab not assigned!");
    //        return null;
    //    }

    //    GameObject loadObj = Instantiate(loadPrefab, Vector3.zero, Quaternion.identity);

    //    // Parent the load to the node so it moves with it
    //    loadObj.transform.SetParent(node.transform);

    //    LoadBehaviour load = loadObj.GetComponent<LoadBehaviour>();
    //    if (load == null)
    //    {
    //        Debug.LogError("Load prefab missing LoadBehaviour script!");
    //        return null;
    //    }

    //    // Assign values
    //    load.node = node;
    //    load.direction = direction.normalized;
    //    load.magnitude = magnitude;

    //    // Track in node list
    //    node.loads.Add(load);

    //    // Apply arrow direction & length visually
    //    load.transform.rotation = Quaternion.LookRotation(load.direction);
    //    load.transform.localScale = new Vector3(1, 1, magnitude);

    //    return load;
    //}

    //public LoadBehaviour CreateLoad(NodeBehaviour node, Vector3 direction, float magnitude)
    //{
    //    if (loadPrefab == null)
    //    {
    //        Debug.LogError("Load prefab not assigned!");
    //        return null;
    //    }

    //    GameObject loadObj = Instantiate(loadPrefab, node.transform.position, Quaternion.identity);
    //    LoadBehaviour load = loadObj.GetComponent<LoadBehaviour>();
    //    if (load == null)
    //    {
    //        Debug.LogError("Load prefab missing Load script!");
    //        return null;
    //    }
    //    return load;
    //}

    public void RemoveEdge(EdgeBehaviour edge)
    {
        if (edge != null)
            Destroy(edge.gameObject);
    }
}
