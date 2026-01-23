using Meta.XR;
using UnityEngine;

/// <summary>
/// Manages the ParentWorld coordinate system setup using a two-point anchor system.
/// First point sets the origin, second point defines the X-axis direction.
/// </summary>
public class WorldCoordinateManager : MonoBehaviour
{
    [Header("Assignments")]
    public Transform rightControllerAnchor;
    public EnvironmentRaycastManager raycastManager;
    public GameObject anchorPrefab;  // Visual marker prefab (same as surfaceAnchor in RaycastSurfaceFinder)
    public GameObject coordinatePrefab;  // Coordinate axes visual prefab

    [Header("Debug Info")]
    public Vector3 worldOrigin;
    public Vector3 worldDirection;  // Second point for X-axis direction

    [Header("ParentWorld (Assign in Inspector)")]
    [Tooltip("Pre-existing ParentWorld object. All structure objects will be children of this.")]
    public Transform parentWorld;

    // Internal state
    private bool originSet = false;
    private bool directionSet = false;

    // Visual markers
    private GameObject originVisual;
    private GameObject directionVisual;
    private GameObject previewVisual;
    private GameObject coordinateVisual;  // Coordinate axes visual instance

    // Flag to control preview visibility (set by OVRGraphController based on mode)
    private bool visualsEnabled = true;

    // Computed basis vectors
    public Vector3 XAxis { get; private set; } = Vector3.right;
    public Vector3 YAxis { get; private set; } = Vector3.up;
    public Vector3 ZAxis { get; private set; } = Vector3.forward;

    // Public property to check if world setup is complete
    public bool IsWorldSet => originSet && directionSet;

    void Update()
    {
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        if (rightControllerAnchor == null || raycastManager == null) return;

        // Don't update preview if visuals are disabled (not in SetupWorld/Grid mode)
        if (!visualsEnabled)
        {
            if (previewVisual != null)
                previewVisual.SetActive(false);
            return;
        }

        var ray = new Ray(rightControllerAnchor.position, rightControllerAnchor.forward);

        if (raycastManager.Raycast(ray, out var hit))
        {
            UpdateMarker(ref previewVisual, hit.point, true, isPreview: true);
        }
        else
        {
            UpdateMarker(ref previewVisual, Vector3.zero, false, isPreview: true);
        }
    }

    /// <summary>
    /// Called when trigger is pressed in SetupWorld mode.
    /// Two-state state machine: first sets origin, second sets direction.
    /// </summary>
    public void SetAnchor()
    {
        if (rightControllerAnchor == null || raycastManager == null)
        {
            Debug.LogWarning("[WorldCoordinateManager] Missing controller anchor or raycast manager.");
            return;
        }

        var ray = new Ray(rightControllerAnchor.position, rightControllerAnchor.forward);

        if (raycastManager.Raycast(ray, out var hit))
        {
            // STATE 1: Start new cycle (origin not set, or previous cycle complete)
            if (!originSet || directionSet)
            {
                worldOrigin = hit.point;
                worldDirection = Vector3.zero;
                originSet = true;
                directionSet = false;

                UpdateMarker(ref originVisual, worldOrigin, true);
                UpdateMarker(ref directionVisual, Vector3.zero, false);

                Debug.Log("[WorldCoordinateManager] Origin set. Waiting for direction point...");
                HapticFeedback.Trigger(HapticFeedback.HapticType.Medium);
            }
            // STATE 2: Completing the pair (origin set, direction not yet set)
            else
            {
                worldDirection = hit.point;
                directionSet = true;

                UpdateMarker(ref directionVisual, worldDirection, true);

                UpdateParentWorldTransform();
                Debug.Log("[WorldCoordinateManager] Direction set. ParentWorld transform updated.");
                HapticFeedback.Trigger(HapticFeedback.HapticType.Success);
            }
        }
    }

    /// <summary>
    /// Updates the ParentWorld transform based on the two anchor points.
    /// ParentWorld must be assigned in Inspector.
    /// </summary>
    private void UpdateParentWorldTransform()
    {
        if (!originSet || !directionSet) return;

        if (parentWorld == null)
        {
            Debug.LogError("[WorldCoordinateManager] ParentWorld is not assigned! Please assign it in the Inspector.");
            return;
        }

        // Compute basis vectors
        Vector3 direction = worldDirection - worldOrigin;
        Vector3 projectedDirection = new Vector3(direction.x, 0, direction.z);

        if (projectedDirection.sqrMagnitude < 0.0001f)
        {
            // Direction too short or vertical, use default X axis
            projectedDirection = Vector3.right;
            Debug.LogWarning("[WorldCoordinateManager] Direction too short or vertical, using default X axis.");
        }

        XAxis = projectedDirection.normalized;
        YAxis = Vector3.up;
        ZAxis = Vector3.Cross(XAxis, YAxis).normalized;

        // Update transform - all children will move with it
        parentWorld.position = worldOrigin;
        parentWorld.rotation = Quaternion.LookRotation(ZAxis, YAxis);

        // Show coordinate visual aligned with parentWorld
        UpdateCoordinateVisual();

        Debug.Log("[WorldCoordinateManager] ParentWorld transform updated. All children moved with it.");
    }

    /// <summary>
    /// Updates the coordinate visual to match parentWorld transform.
    /// Only shows when both origin and direction are set.
    /// </summary>
    private void UpdateCoordinateVisual()
    {
        if (coordinatePrefab == null) return;

        // Only show when world setup is complete
        if (!originSet || !directionSet)
        {
            if (coordinateVisual != null)
                coordinateVisual.SetActive(false);
            return;
        }

        // Create coordinate visual if needed
        if (coordinateVisual == null)
        {
            coordinateVisual = Instantiate(coordinatePrefab);
            coordinateVisual.name = "WorldCoordinateVisual";
        }

        // Position and orient to match parentWorld
        coordinateVisual.SetActive(true);
        coordinateVisual.transform.position = parentWorld.position;
        coordinateVisual.transform.rotation = parentWorld.rotation;
    }

    /// <summary>
    /// Helper method to create/update visual markers.
    /// </summary>
    private void UpdateMarker(ref GameObject markerInstance, Vector3 position, bool isVisible, bool isPreview = false)
    {
        if (anchorPrefab == null) return;

        // Initialization (runs once when object is first created)
        if (markerInstance == null)
        {
            markerInstance = Instantiate(anchorPrefab);

            if (markerInstance.GetComponent<Collider>())
                Destroy(markerInstance.GetComponent<Collider>());

            // Make preview marker semi-transparent
            if (isPreview)
            {
                Renderer rend = markerInstance.GetComponentInChildren<Renderer>();
                if (rend != null)
                {
                    Color customColor = rend.material.color;
                    customColor.a = 0.2f;
                    rend.material.color = customColor;
                }
            }
        }

        // Set visibility
        markerInstance.SetActive(isVisible);

        // Set position if visible
        if (isVisible)
        {
            markerInstance.transform.position = position;
            markerInstance.transform.rotation = Quaternion.identity;
        }
    }

    /// <summary>
    /// Resets the world setup state for reconfiguration.
    /// </summary>
    public void ResetWorldSetup()
    {
        originSet = false;
        directionSet = false;

        UpdateMarker(ref originVisual, Vector3.zero, false);
        UpdateMarker(ref directionVisual, Vector3.zero, false);

        // Hide coordinate visual
        if (coordinateVisual != null)
            coordinateVisual.SetActive(false);

        Debug.Log("[WorldCoordinateManager] World setup reset. Ready for new configuration.");
    }

    /// <summary>
    /// Gets the local position relative to ParentWorld.
    /// </summary>
    public Vector3 WorldToLocal(Vector3 worldPosition)
    {
        if (parentWorld == null) return worldPosition;
        return parentWorld.InverseTransformPoint(worldPosition);
    }

    /// <summary>
    /// Gets the world position from local ParentWorld position.
    /// </summary>
    public Vector3 LocalToWorld(Vector3 localPosition)
    {
        if (parentWorld == null) return localPosition;
        return parentWorld.TransformPoint(localPosition);
    }

    /// <summary>
    /// Sets visibility of all visual markers (originVisual, directionVisual, previewVisual).
    /// Called by OVRGraphController to show/hide based on mode.
    /// </summary>
    public void SetVisualsActive(bool active)
    {
        visualsEnabled = active;

        if (originVisual != null)
            originVisual.SetActive(active && originSet);
        if (directionVisual != null)
            directionVisual.SetActive(active && directionSet);
        if (previewVisual != null)
            previewVisual.SetActive(active);
    }
}
