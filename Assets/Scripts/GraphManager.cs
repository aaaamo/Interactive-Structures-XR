using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class GraphManager : NetworkBehaviour
{
    public GameObject nodePrefab;
    public GameObject edgePrefab;
    public GameObject loadPrefab;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Debug.Log($"[GraphManager] OnNetworkSpawn - IsServer: {IsServer}, IsClient: {IsClient}, IsHost: {IsHost}");
    }

    [ServerRpc(RequireOwnership = false)]
    public void CreateNodeServerRpc(Vector3 position)
    {
        Debug.Log($"[GraphManager] CreateNodeServerRpc called at {position}");
        GameObject obj = Instantiate(nodePrefab, position, Quaternion.identity);
        obj.GetComponent<NetworkObject>().Spawn();
        Debug.Log($"[GraphManager] Node spawned successfully");
    }

    [ServerRpc(RequireOwnership = false)]
    public void CreateEdgeServerRpc(ulong nodeAId, ulong nodeBId)
    {
        GameObject edgeObj = Instantiate(edgePrefab, Vector3.zero, Quaternion.identity);
        EdgeBehaviour edge = edgeObj.GetComponent<EdgeBehaviour>();

        edge.nodeAId.Value = nodeAId;
        edge.nodeBId.Value = nodeBId;

        edgeObj.GetComponent<NetworkObject>().Spawn();
    }

    [ServerRpc(RequireOwnership = false)]
    public void CreateLoadServerRpc(ulong nodeId, Vector3 direction, float magnitude)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(nodeId, out NetworkObject nodeObj)) return;

        GameObject loadObj = Instantiate(loadPrefab, nodeObj.transform.position, Quaternion.identity);
        LoadBehaviour load = loadObj.GetComponent<LoadBehaviour>();

        loadObj.GetComponent<NetworkObject>().Spawn();
        loadObj.GetComponent<NetworkObject>().TrySetParent(nodeObj);

        load.directionNet.Value = direction;
        load.magnitudeNet.Value = magnitude;
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
        Dictionary<int, ulong> fileIdToRuntimeId = new Dictionary<int, ulong>();

        // 3. Create Nodes & Map IDs
        foreach (var nodeData in data.nodes)
        {
            GameObject nodeObj = Instantiate(nodePrefab, nodeData.GetPosition(), Quaternion.identity);
            NetworkObject netObj = nodeObj.GetComponent<NetworkObject>();
            netObj.Spawn();

            // Apply properties
            NodeBehaviour nodeBehaviour = nodeObj.GetComponent<NodeBehaviour>();
            if (nodeBehaviour != null)
            {
                nodeBehaviour.isSupport = nodeData.isSupport;
            }

            fileIdToRuntimeId[nodeData.id] = netObj.NetworkObjectId;
        }

        // 4. Create Edges
        foreach (var edgeData in data.edges)
        {
            if (fileIdToRuntimeId.TryGetValue(edgeData.nodeAId, out ulong idA) &&
                fileIdToRuntimeId.TryGetValue(edgeData.nodeBId, out ulong idB))
            {
                CreateEdgeServerRpc(idA, idB);
            }
        }

        // 5. Create Loads
        foreach (var loadData in data.loads)
        {
            if (fileIdToRuntimeId.TryGetValue(loadData.nodeId, out ulong nodeId))
            {
                Vector3 dir = new Vector3(loadData.dirX, loadData.dirY, loadData.dirZ);
                CreateLoadServerRpc(nodeId, dir, loadData.magnitude);
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
