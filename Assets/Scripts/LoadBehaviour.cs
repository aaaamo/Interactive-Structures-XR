using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class LoadBehaviour : NetworkBehaviour
{
    [Header("Data")]
    public NodeBehaviour node;
    public Vector3 direction = Vector3.down;
    public float magnitude = 1f;

    [Header("Network Variables")]
    public NetworkVariable<Vector3> directionNet = new NetworkVariable<Vector3>(Vector3.down);
    public NetworkVariable<float> magnitudeNet = new NetworkVariable<float>(1f);

    [Header("Visual Settings")]
    public Transform arrow; // Assign the visual child (arrow prefab)
    [SerializeField] private float offset = 0.01f;
    [SerializeField] private float scale = 0.012f;

    private void Awake()
    {
        // Automatically find the arrow child if not assigned
        if (arrow == null && transform.childCount > 0)
        {
            arrow = transform.GetChild(0);
        }
    }

    public override void OnNetworkSpawn()
    {
        // 1. Initially hide renderers to prevent the "floating at origin" glitch
        SetRenderersVisible(false);

        // 2. Subscribe to value changes
        directionNet.OnValueChanged += OnLoadDataChanged;
        magnitudeNet.OnValueChanged += OnLoadDataChanged;

        // 3. Initialize local values with current networked data (for late-joiners)
        direction = directionNet.Value;
        magnitude = magnitudeNet.Value;

        // 4. Attempt to register with parent node immediately
        CheckAndRegister();
    }

    public override void OnNetworkDespawn()
    {
        // Unsubscribe to prevent memory leaks
        directionNet.OnValueChanged -= OnLoadDataChanged;
        magnitudeNet.OnValueChanged -= OnLoadDataChanged;

        if (node != null && node.loads.Contains(this))
        {
            node.loads.Remove(this);
        }
    }

    private void OnLoadDataChanged(Vector3 oldVal, Vector3 newVal) => UpdateVisuals();
    private void OnLoadDataChanged(float oldVal, float newVal) => UpdateVisuals();

    private void UpdateVisuals()
    {
        direction = directionNet.Value;
        magnitude = magnitudeNet.Value;
        UpdateArrow();
    }

    private void CheckAndRegister()
    {
        if (transform.parent != null)
        {
            // Successfully parented
            transform.localPosition = Vector3.zero;
            RegisterWithNode();
            UpdateArrow();
            SetRenderersVisible(true);
        }
        else
        {
            // Not parented yet (common in Netcode spawn sequence)
            StartCoroutine(RetryRegisterWithNode());
        }
    }

    // This fires when Netcode catches up and assigns the parent on the client
    private void OnTransformParentChanged()
    {
        Debug.Log($"[Load] Parent changed to: {transform.parent?.name}");
        CheckAndRegister();
    }

    private void RegisterWithNode()
    {
        if (transform.parent != null)
        {
            NodeBehaviour parentNode = transform.parent.GetComponent<NodeBehaviour>();

            // Handle unregistering from previous parent if necessary
            if (node != null && node != parentNode)
            {
                node.loads.Remove(this);
            }

            node = parentNode;

            if (node != null && !node.loads.Contains(this))
            {
                node.loads.Add(this);
            }
        }
    }

    private IEnumerator RetryRegisterWithNode()
    {
        int retries = 0;
        while (transform.parent == null && retries < 50)
        {
            yield return new WaitForSeconds(0.1f);
            retries++;
        }

        if (transform.parent != null)
        {
            CheckAndRegister();
        }
    }

    public void UpdateArrow()
    {
        if (arrow == null || node == null) return;

        // Use deterministic cross product to avoid client-side rotation mismatch
        Vector3 refVector = Mathf.Abs(Vector3.Dot(direction, Vector3.up)) < 0.99f ? Vector3.up : Vector3.right;
        Vector3 localX = Vector3.Cross(direction, refVector).normalized;

        // Update rotation of this object so its Y axis aligns with the load direction
        transform.localRotation = Quaternion.LookRotation(localX, direction);

        arrow.localScale = new Vector3(scale, magnitude - offset, scale);
        arrow.localPosition = new Vector3(0, 0.5f * magnitude + offset, 0);
    }

    private void SetRenderersVisible(bool visible)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (var r in renderers) r.enabled = visible;
    }

    [ServerRpc(RequireOwnership = false)]
    public void UpdateLoadServerRpc(Vector3 newDirection, float newMagnitude)
    {
        directionNet.Value = newDirection.normalized;
        magnitudeNet.Value = newMagnitude;
    }

    public Vector3 EndPoint()
    {
        Vector3 worldDirection = node.transform.TransformDirection(direction);
        return node.transform.position + worldDirection.normalized * magnitude;
    }
}