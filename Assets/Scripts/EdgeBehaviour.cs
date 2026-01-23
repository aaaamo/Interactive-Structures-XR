using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class EdgeBehaviour : NetworkBehaviour
{
    public NodeBehaviour nodeA;
    public NodeBehaviour nodeB;

    public NetworkVariable<ulong> nodeAId = new NetworkVariable<ulong>();
    public NetworkVariable<ulong> nodeBId = new NetworkVariable<ulong>();

    private Transform edgeTransform;

    private GameObject displacedEdge;

    // For delayed visual show (prevents "fly-in" effect on clients)
    private Renderer[] allRenderers;

    void Awake()
    {
        edgeTransform = transform;
        if (edgeTransform == null)
            Debug.LogError("Edge transform missing!");

        // Cache all renderers for visibility control
        allRenderers = GetComponentsInChildren<Renderer>(true);

        // Note: Don't hide visuals in Awake - let non-networked instances (local preview edges) show immediately
        // Networked instances will hide in OnNetworkSpawn and show after nodes connect
    }

    public override void OnNetworkSpawn()
    {
        // Hide visuals immediately for networked objects (prevents "fly-in" effect)
        SetRenderersVisible(false);

        nodeAId.OnValueChanged += (oldVal, newVal) => ConnectNodes();
        nodeBId.OnValueChanged += (oldVal, newVal) => ConnectNodes();
        ConnectNodes();

        // If nodes not found yet (late join), retry until connected
        if (nodeA == null || nodeB == null)
        {
            StartCoroutine(RetryConnectNodes());
        }
    }

    private System.Collections.IEnumerator RetryConnectNodes()
    {
        int maxRetries = 50; // ~5 seconds max
        int retries = 0;
        while ((nodeA == null || nodeB == null) && retries < maxRetries)
        {
            yield return new WaitForSeconds(0.1f);
            ConnectNodes();
            retries++;
        }

        if (nodeA == null || nodeB == null)
        {
            Debug.LogWarning($"[EdgeBehaviour] Failed to connect nodes after {maxRetries} retries. nodeAId={nodeAId.Value}, nodeBId={nodeBId.Value}");
        }
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

        // Show visuals when both nodes are connected
        // Nodes now have correct position immediately via initialLocalPosition
        if (nodeA != null && nodeB != null)
        {
            SetRenderersVisible(true);
        }
    }

    private void SetRenderersVisible(bool visible)
    {
        if (allRenderers == null) return;

        foreach (var renderer in allRenderers)
        {
            if (renderer != null)
                renderer.enabled = visible;
        }
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

    public void ShowDisplacement(float scale, Material displacedMaterial, float thickness = -1f)
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

        // Set thickness: use provided value or match original edge
        Vector3 baseScale = edgeTransform.localScale;
        if (thickness > 0)
        {
            baseScale.x = thickness;
            baseScale.z = thickness;
        }
        displacedEdge.transform.localScale = baseScale;

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
