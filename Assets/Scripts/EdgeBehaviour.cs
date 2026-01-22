using UnityEngine;
using Unity.Netcode;

public class EdgeBehaviour : NetworkBehaviour
{
    public NodeBehaviour nodeA;
    public NodeBehaviour nodeB;

    public NetworkVariable<ulong> nodeAId = new NetworkVariable<ulong>();
    public NetworkVariable<ulong> nodeBId = new NetworkVariable<ulong>();

    private Transform edgeTransform;

    private GameObject displacedEdge;

    void Awake()
    {
        edgeTransform = transform;
        if (edgeTransform == null)
            Debug.LogError("Edge transform missing!");
    }

    public override void OnNetworkSpawn()
    {
        nodeAId.OnValueChanged += (oldVal, newVal) => ConnectNodes();
        nodeBId.OnValueChanged += (oldVal, newVal) => ConnectNodes();
        ConnectNodes();
    }

    public override void OnNetworkDespawn()
    {
        if (nodeA != null && nodeA.connectedEdges.Contains(this))
            nodeA.connectedEdges.Remove(this);
        if (nodeB != null && nodeB.connectedEdges.Contains(this))
            nodeB.connectedEdges.Remove(this);
    }

    void ConnectNodes()
    {
        if (NetworkManager.Singleton == null) return;

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(nodeAId.Value, out NetworkObject objA))
        {
            nodeA = objA.GetComponent<NodeBehaviour>();
            if (nodeA != null && !nodeA.connectedEdges.Contains(this)) nodeA.connectedEdges.Add(this);
        }

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(nodeBId.Value, out NetworkObject objB))
        {
            nodeB = objB.GetComponent<NodeBehaviour>();
            if (nodeB != null && !nodeB.connectedEdges.Contains(this)) nodeB.connectedEdges.Add(this);
        }

        UpdateEdgePosition();
    }

    void Update()
    {
        if (nodeA != null && nodeB != null)
        {
            UpdateEdgePosition();
        }
    }

    public void UpdateEdgePosition(Vector3? tempEnd = null)
    {
        if (nodeA == null)
        {
            Debug.LogWarning("EdgeBehaviour nodeA is null!");
            return;
        }

        Vector3 start = nodeA.transform.position;
        Vector3 end;

        if (nodeB != null)
            end = nodeB.transform.position;
        else if (tempEnd.HasValue)
            end = tempEnd.Value;
        else
            return; // nothing to update

        PositionEdge(edgeTransform, start, end);
    }

    public void ShowDisplacement(float scale, Material displacedMaterial)
    {
        if (nodeA == null || nodeB == null)
        {
            Debug.LogWarning("Cannot show displacement: missing nodes");
            return;
        }

        // Calculate displaced positions
        Vector3 startDisplaced = nodeA.transform.position + nodeA.displacementVector * scale;
        Vector3 endDisplaced = nodeB.transform.position + nodeB.displacementVector * scale;

        // Create displaced edge visual if it doesn't exist
        if (displacedEdge == null)
        {
            displacedEdge = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            displacedEdge.transform.SetParent(transform.parent);
            displacedEdge.name = gameObject.name + "_displaced";

            // Match the original edge's scale (at least X and Z for thickness)
            displacedEdge.transform.localScale = edgeTransform.localScale;

            // Apply displaced material
            Renderer renderer = displacedEdge.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = displacedMaterial;
            }

            // Remove collider if you don't need it
            Collider col = displacedEdge.GetComponent<Collider>();
            if (col != null) Destroy(col);
        }

        PositionEdge(displacedEdge.transform, startDisplaced, endDisplaced);
        displacedEdge.SetActive(true);
    }

    public void HideDisplacement()
    {
        if (displacedEdge != null)
        {
            displacedEdge.SetActive(false);
        }
    }

    public void CleanupDisplacement()
    {
        if (displacedEdge != null)
        {
            Destroy(displacedEdge);
            displacedEdge = null;
        }
    }

    private void PositionEdge(Transform transform, Vector3 start, Vector3 end)
    {
        Vector3 middle = (start + end) / 2f;
        transform.position = middle;

        // Scale along Y-axis
        Vector3 scale = transform.localScale;
        scale.y = Vector3.Distance(start, end) / 2f;
        transform.localScale = scale;

        // Rotate cylinder to align
        transform.up = (end - start).normalized;
    }
}
