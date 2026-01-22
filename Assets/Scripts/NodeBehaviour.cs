using UnityEngine;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;

public class NodeBehaviour : NetworkBehaviour
{
    public List<EdgeBehaviour> connectedEdges = new List<EdgeBehaviour>();
    public List<LoadBehaviour> loads = new List<LoadBehaviour>();

    public NetworkVariable<bool> isSupportNet = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public bool isSupport
    {
        get => isSupportNet.Value;
        set { if (IsOwner) isSupportNet.Value = value; }
    }

    public GameObject supportVisual;
    public GameObject freeVisual;

    public Vector3 displacementVector;
    private GameObject displacedVisual;

    [Header("UI")]
    public TextMeshPro nodeLabel;

    private Transform mainCameraTransform;
    private Vector3 lastPosition;
    private int lastLoadCount;

    public override void OnNetworkSpawn()
    {
        isSupportNet.OnValueChanged += (oldVal, newVal) =>
        {
            ApplyVisualState();
            UpdateTextContent();
        };
        ApplyVisualState();
    }

    void Awake()
    {
        if (connectedEdges == null)
            connectedEdges = new List<EdgeBehaviour>();

        // Cache camera for billboarding
        if (Camera.main != null) mainCameraTransform = Camera.main.transform;

        // Initialize state trackers
        lastPosition = transform.position;
        lastLoadCount = (loads != null) ? loads.Count : 0;

        ApplyVisualState();
        UpdateTextContent();
    }

    void Update()
    {
        if (nodeLabel != null && mainCameraTransform != null)
        {
            Vector3 cameraEuler = mainCameraTransform.rotation.eulerAngles;
            nodeLabel.transform.rotation = Quaternion.Euler(0, cameraEuler.y, 0);
        }

        bool hasMoved = Vector3.Distance(transform.position, lastPosition) > 0.001f;
        bool loadChanged = (loads != null && loads.Count != lastLoadCount);

        if (hasMoved || loadChanged)
        {
            UpdateTextContent();

            lastPosition = transform.position;
            lastLoadCount = (loads != null) ? loads.Count : 0;
        }
    }

    public void UpdateTextContent()
    {
        if (nodeLabel == null) return;

        string posText = $"Pos: {transform.position.x:F1}, {transform.position.y:F1}, {transform.position.z:F1}";
        string loadText = (loads != null && loads.Count > 0) ? $"\nLoads: {loads.Count}" : "";

        nodeLabel.text = $"{posText}{loadText}";
    }

    public void ToggleSupport()
    {
        if (IsOwner)
        {
            isSupport = !isSupport;
        }
        else
        {
            ToggleSupportServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void ToggleSupportServerRpc()
    {
        isSupportNet.Value = !isSupportNet.Value;
    }

    [ServerRpc(RequireOwnership = false)]
    public void MoveServerRpc(Vector3 newPosition)
    {
        transform.position = newPosition;
    }

    private void ApplyVisualState()
    {
        bool support = isSupportNet.Value;
        if (freeVisual != null) freeVisual.SetActive(!support);
        if (supportVisual != null) supportVisual.SetActive(support);
    }

    public void ShowDisplacement(float scale, Material displacedMaterial)
    {
        if (displacedVisual == null)
        {
            // Clone whichever visual is currently active
            GameObject sourceVisual = isSupport ? supportVisual : freeVisual;

            displacedVisual = Instantiate(sourceVisual, transform);
            displacedVisual.transform.SetParent(transform.parent); // Same parent as original node

            // Apply displaced material to all renderers
            Renderer[] renderers = displacedVisual.GetComponentsInChildren<Renderer>();
            foreach (Renderer r in renderers)
            {
                r.material = displacedMaterial;
            }
        }

        displacedVisual.transform.position = transform.position + displacementVector * scale;
        displacedVisual.SetActive(true);
    }

    public void HideDisplacement()
    {
        if (displacedVisual != null)
        {
            displacedVisual.SetActive(false);
        }
    }

    public void CleanupDisplacement()
    {
        if (displacedVisual != null)
        {
            Destroy(displacedVisual);
            displacedVisual = null;
        }
    }
}