using UnityEngine;
using Unity.Netcode;

public class LoadBehaviour : NetworkBehaviour
{
    public NodeBehaviour node;
    public Vector3 direction = Vector3.down; // default down
    public float magnitude = 1f;

    public NetworkVariable<Vector3> directionNet = new NetworkVariable<Vector3>(Vector3.down);
    public NetworkVariable<float> magnitudeNet = new NetworkVariable<float>(1f);

    private float offset = 0.01f;
    private float scale = 0.012f;

    [Header("Visual")]
    public Transform arrow; // assign arrow prefab child

    public override void OnNetworkSpawn()
    {
        RegisterWithNode();

        directionNet.OnValueChanged += (oldVal, newVal) => { direction = newVal; UpdateArrow(); };
        magnitudeNet.OnValueChanged += (oldVal, newVal) => { magnitude = newVal; UpdateArrow(); };

        direction = directionNet.Value;
        magnitude = magnitudeNet.Value;
        UpdateArrow();
    }

    void OnTransformParentChanged()
    {
        RegisterWithNode();
    }

    void RegisterWithNode()
    {
        if (transform.parent != null)
        {
            NodeBehaviour parentNode = transform.parent.GetComponent<NodeBehaviour>();

            // If parent changed, unregister from old node
            if (node != null && node != parentNode && node.loads.Contains(this))
            {
                node.loads.Remove(this);
            }

            node = parentNode;
            // Register with new node
            if (node != null && !node.loads.Contains(this))
            {
                node.loads.Add(this);
                Debug.Log($"[LoadBehaviour] Registered load to node. Node now has {node.loads.Count} loads");
            }
        }
    }

    void OnDestroy()
    {
        // Unregister from node when destroyed
        if (node != null && node.loads.Contains(this))
        {
            node.loads.Remove(this);
        }
    }

    void Awake()
    {
        if (arrow == null && transform.childCount > 0)
            arrow = transform.GetChild(0);
    }

    public void SetDirection(Vector3 dir)
    {
        direction = dir.normalized;
        UpdateArrow();
    }

    public void SetMagnitude(float mag)
    {
        magnitude = mag;
        UpdateArrow();
    }

    public void UpdateArrow()
    {
        if (arrow == null) return;
        Vector3 localX = Vector3.Cross(direction, new Vector3(Random.value, Random.value, Random.value));

        this.transform.rotation = Quaternion.LookRotation(localX, direction);
        arrow.localScale = new Vector3(scale, magnitude - offset, scale);
        arrow.localPosition = new Vector3(0, 0.5f * magnitude + offset, 0);
    }

    public Vector3 GetForceVector()
    {
        return direction * magnitude;
    }

    public Vector3 EndPoint()
    {
        return node.transform.position + direction.normalized * magnitude;
    }

    [ServerRpc(RequireOwnership = false)]
    public void UpdateLoadServerRpc(Vector3 newDirection, float newMagnitude)
    {
        directionNet.Value = newDirection;
        magnitudeNet.Value = newMagnitude;
    }
}
