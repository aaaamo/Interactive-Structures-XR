using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using Unity.Netcode.Components;

public class GraphManager : NetworkBehaviour
{
    public GameObject nodePrefab;
    public GameObject edgePrefab;
    public GameObject loadPrefab;
    public WorldCoordinateManager worldCoordinateManager;

    // Unified access to ParentWorld
    private Transform ParentWorld => worldCoordinateManager?.parentWorld;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Debug.Log($"[GraphManager] OnNetworkSpawn - IsServer: {IsServer}, IsClient: {IsClient}, IsHost: {IsHost}");
    }

    [ServerRpc(RequireOwnership = false)]
    public void CreateNodeServerRpc(Vector3 localPosition)
    {
        Debug.Log($"[GraphManager] CreateNodeServerRpc called at local position {localPosition}");
        // Convert local position to server's world position for instantiation
        GameObject obj = Instantiate(nodePrefab, localPosition, Quaternion.identity);

        // Set initial position in NetworkVariable BEFORE spawning
        // This allows clients to read the correct position immediately on spawn
        NodeBehaviour node = obj.GetComponent<NodeBehaviour>();

        obj.GetComponent<NetworkObject>().Spawn();
        obj.GetComponent<NetworkObject>().TrySetParent(ParentWorld.GetComponent<NetworkObject>(), false);
        // Ensure local position is exactly what was requested (fixes floating point drift)
        Debug.Log($"[GraphManager] Node spawned successfully at local position {localPosition}");
    }

    [ServerRpc(RequireOwnership = false)]
    public void CreateEdgeServerRpc(ulong nodeAId, ulong nodeBId)
    {
        GameObject edgeObj = Instantiate(edgePrefab, Vector3.zero, Quaternion.identity);
        EdgeBehaviour edge = edgeObj.GetComponent<EdgeBehaviour>();

        edge.nodeAId.Value = nodeAId;
        edge.nodeBId.Value = nodeBId;

        edgeObj.GetComponent<NetworkObject>().Spawn();
        edgeObj.GetComponent<NetworkObject>().TrySetParent(ParentWorld.GetComponent<NetworkObject>());
    }

    [ServerRpc(RequireOwnership = false)]
    public void CreateLoadServerRpc(ulong nodeId, Vector3 localDirection, float magnitude)
    {
        Debug.Log($"[GraphManager] Attempting to create load for Node ID: {nodeId}");

        // 1. Find the Node via its NetworkObjectId
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(nodeId, out NetworkObject nodeNetObj))
        {
            Debug.LogError($"[GraphManager] Failed to find Node with ID {nodeId}");
            return;
        }

        // 2. Instantiate the Load prefab
        // Position and rotation don't matter yet because we will reset them after parenting
        GameObject loadObj = Instantiate(loadPrefab, Vector3.zero, Quaternion.identity);
        NetworkObject loadNetObj = loadObj.GetComponent<NetworkObject>();

        // 3. Spawn the object on the network
        loadNetObj.Spawn();

        // 4. Set the Parent to the Node
        // 'false' means: do NOT keep world position. Snap to the parent's (0,0,0) instead.
        bool parentSuccess = loadNetObj.TrySetParent(nodeNetObj, false);

        if (parentSuccess)
        {
            // 5. Initialize the networked data
            LoadBehaviour load = loadObj.GetComponent<LoadBehaviour>();
            load.directionNet.Value = localDirection.normalized;
            load.magnitudeNet.Value = magnitude;

            // Extra safety: ensure local position is zeroed
            loadObj.transform.localPosition = Vector3.zero;

            Debug.Log($"[GraphManager] Load successfully attached to Node {nodeId}");
        }
        else
        {
            Debug.LogError($"[GraphManager] Parenting failed for Load on Node {nodeId}");
            // Optional: Despawn if parenting fails to prevent "ghost" objects at origin
            loadNetObj.Despawn();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void DeleteNetworkObjectServerRpc(ulong networkObjectId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject obj))
        {
            // 노드 삭제 시 연결된 엣지와 하중도 함께 삭제 (Cascading Delete)
            var node = obj.GetComponent<NodeBehaviour>();
            if (node != null)
            {
                // 연결된 엣지 삭제
                if (node.connectedEdges != null)
                {
                    // 리스트가 수정될 수 있으므로 복사본으로 순회
                    var edgesToRemove = new List<EdgeBehaviour>(node.connectedEdges);
                    foreach (var edge in edgesToRemove)
                    {
                        if (edge != null && edge.IsSpawned)
                        {
                            edge.NetworkObject.Despawn();
                        }
                    }
                }

                // 연결된 하중 삭제
                if (node.loads != null)
                {
                    var loadsToRemove = new List<LoadBehaviour>(node.loads);
                    foreach (var load in loadsToRemove)
                    {
                        if (load != null && load.IsSpawned)
                        {
                            load.NetworkObject.Despawn();
                        }
                    }
                }
            }

            obj.Despawn();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void LoadStructureServerRpc(string json)
    {
        Debug.Log("[GraphManager] Loading structure from JSON on server...");

        // 1. Clear existing structure (Server side)
        var existingNodes = FindObjectsByType<NodeBehaviour>(FindObjectsSortMode.None);
        foreach (var node in existingNodes)
        {
            if (node.IsSpawned) node.GetComponent<NetworkObject>().Despawn();
        }
        var existingEdges = FindObjectsByType<EdgeBehaviour>(FindObjectsSortMode.None);
        foreach (var edge in existingEdges)
        {
            if (edge.IsSpawned) edge.GetComponent<NetworkObject>().Despawn();
        }
        // Loads are usually children of nodes and despawn with them, but safe to check
        var existingLoads = FindObjectsByType<LoadBehaviour>(FindObjectsSortMode.None);
        foreach (var load in existingLoads)
        {
            if (load.IsSpawned) load.GetComponent<NetworkObject>().Despawn();
        }

        // 2. Parse Data
        StructureSaveData data = JsonUtility.FromJson<StructureSaveData>(json);
        Dictionary<int, NodeBehaviour> fileIdToNode = new Dictionary<int, NodeBehaviour>();

        // 3. Create Nodes
        foreach (var nodeData in data.nodes)
        {
            Vector3 localPos = nodeData.GetPosition();
            Vector3 worldPos = ParentWorld != null
                ? ParentWorld.TransformPoint(localPos)
                : localPos;

            GameObject nodeObj = Instantiate(nodePrefab, worldPos, Quaternion.identity);
            NodeBehaviour nodeBehaviour = nodeObj.GetComponent<NodeBehaviour>();

            NetworkObject netObj = nodeObj.GetComponent<NetworkObject>();
            netObj.Spawn();

            if (ParentWorld != null && ParentWorld.GetComponent<NetworkObject>() != null)
                netObj.TrySetParent(ParentWorld.GetComponent<NetworkObject>());

            if (nodeBehaviour != null)
            {
                nodeBehaviour.isSupport = nodeData.isSupport;
                fileIdToNode[nodeData.id] = nodeBehaviour;
            }
        }

        // 4. Create Edges
        foreach (var edgeData in data.edges)
        {
            if (fileIdToNode.TryGetValue(edgeData.nodeAId, out NodeBehaviour nodeA) &&
                fileIdToNode.TryGetValue(edgeData.nodeBId, out NodeBehaviour nodeB))
            {
                GameObject edgeObj = Instantiate(edgePrefab, Vector3.zero, Quaternion.identity);
                EdgeBehaviour edge = edgeObj.GetComponent<EdgeBehaviour>();

                edge.nodeAId.Value = nodeA.NetworkObjectId;
                edge.nodeBId.Value = nodeB.NetworkObjectId;

                edgeObj.GetComponent<NetworkObject>().Spawn();
                edgeObj.GetComponent<NetworkObject>().TrySetParent(ParentWorld.GetComponent<NetworkObject>());
            }
        }

        // 5. Create Loads
        foreach (var loadData in data.loads)
        {
            if (fileIdToNode.TryGetValue(loadData.nodeId, out NodeBehaviour targetNode))
            {
                Vector3 dir = new Vector3(loadData.dirX, loadData.dirY, loadData.dirZ);
                GameObject loadObj = Instantiate(loadPrefab, targetNode.transform.position, Quaternion.identity);

                loadObj.GetComponent<NetworkObject>().Spawn();
                loadObj.GetComponent<NetworkObject>().TrySetParent(targetNode.NetworkObject);

                LoadBehaviour load = loadObj.GetComponent<LoadBehaviour>();
                load.directionNet.Value = dir;
                load.magnitudeNet.Value = loadData.magnitude;
            }
        }

        Debug.Log("[GraphManager] Structure loaded successfully on server.");
    }

    // 로컬 전용 메서드 (프리뷰 등에서 사용)
    public EdgeBehaviour CreateLocalEdge(NodeBehaviour nodeA)
    {
        GameObject edgeObj = Instantiate(edgePrefab, Vector3.zero, Quaternion.identity);
        EdgeBehaviour edge = edgeObj.GetComponent<EdgeBehaviour>();
        edge.nodeA = nodeA;
        // NetworkObject 컴포넌트 제거 (로컬 전용이므로)
        var netObj = edgeObj.GetComponent<NetworkObject>();
        if (netObj != null) Destroy(netObj);
        return edge;
    }

    public void RemoveLocalEdge(EdgeBehaviour edge)
    {
        if (edge != null) Destroy(edge.gameObject);
    }
}
