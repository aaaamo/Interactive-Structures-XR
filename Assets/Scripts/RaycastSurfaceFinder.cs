//using Meta.XR;
//using Meta.XR.MRUtilityKit;
//using UnityEngine;

//public class RaycastSurfaceFinder : MonoBehaviour
//{
//    [Header("Assignments")]
//    public Transform rightControllerAnchor;
//    public GameObject surfaceAnchor;
//    public EnvironmentRaycastManager raycastManager;
//    public GridPointRenderer gridPointRenderer;

//    [Header("Debug Info")]
//    public Vector3 originAnchor;
//    public Vector3 directionAnchor;

//    private bool originSet = false;
//    private bool directionSet = false;

//    // Internal references to the actual spawned objects
//    private GameObject originVisual;
//    private GameObject directionVisual;

//    public void SetAnchor()
//    {
//        if (rightControllerAnchor == null || raycastManager == null)
//        {
//            Debug.LogWarning("RayCaster: Missing controller anchor or raycast manager.");
//            return;
//        }

//        var ray = new Ray(rightControllerAnchor.position, rightControllerAnchor.forward);

//        // Raycast against the environment (Depth/Scene)
//        if (raycastManager.Raycast(ray, out var hit))
//        {
//            // STATE 1: Start new cycle (First click OR Resetting after a finished pair)
//            if (originSet == false || directionSet == true)
//            {
//                originAnchor = hit.point;
//                directionAnchor = Vector3.zero; // Clear old data

//                originSet = true;
//                directionSet = false;

//                // Visuals: Show Origin, Hide Direction
//                UpdateMarker(ref originVisual, originAnchor, true);
//                UpdateMarker(ref directionVisual, Vector3.zero, false);

//                Debug.Log("RayCaster: Origin set. Waiting for direction...");
//            }
//            // STATE 2: Finishing the pair (Second click)
//            else
//            {
//                directionAnchor = hit.point;
//                directionSet = true;

//                // Visuals: Show Direction (Origin stays visible)
//                UpdateMarker(ref directionVisual, directionAnchor, true);

//                SetGrid();
//                Debug.Log("RayCaster: Direction set. Grid updated.");
//            }
//        }
//        else
//        {
//            Debug.LogWarning("RayCaster: No surface hit detected.");
//        }
//    }

//    private void SetGrid()
//    {
//        if (gridPointRenderer != null && originSet && directionSet)
//        {
//            gridPointRenderer.origin = originAnchor;

//            // Calculate direction and project to XZ plane (flatten Y)
//            Vector3 direction = directionAnchor - originAnchor;
//            Vector3 projected = new Vector3(direction.x, 0, direction.z).normalized;

//            gridPointRenderer.xVec = projected;
//            gridPointRenderer.RefreshGrid();
//        }
//    }

//    // Helper: Instantiates prefab if needed, moves it, and toggles visibility
//    private void UpdateMarker(ref GameObject markerInstance, Vector3 position, bool isVisible)
//    {
//        if (surfaceAnchor == null) return;

//        // 1. Create it if it doesn't exist yet
//        if (markerInstance == null)
//        {
//            markerInstance = Instantiate(surfaceAnchor);
//            // Optional: Remove collider if it blocks your raycast
//            if (markerInstance.GetComponent<Collider>())
//                Destroy(markerInstance.GetComponent<Collider>());
//        }

//        // 2. Set Visibility
//        markerInstance.SetActive(isVisible);

//        // 3. Set Position if visible
//        if (isVisible)
//        {
//            markerInstance.transform.position = position;
//            markerInstance.transform.rotation = Quaternion.identity;
//        }
//    }
//}

using Meta.XR;
using Meta.XR.MRUtilityKit;
using UnityEngine;

public class RaycastSurfaceFinder : MonoBehaviour
{
    [Header("Assignments")]
    public Transform rightControllerAnchor;
    // IMPORTANT: The material on this prefab must have its Rendering Mode set to 'Transparent' or 'Fade'
    public GameObject surfaceAnchor;
    public EnvironmentRaycastManager raycastManager;
    public GridPointRenderer gridPointRenderer;

    [Header("Debug Info")]
    public Vector3 originAnchor;
    public Vector3 directionAnchor;

    private bool originSet = false;
    private bool directionSet = false;

    // Internal references to the spawned objects
    private GameObject originVisual;
    private GameObject directionVisual;
    private GameObject previewVisual;

    void Update()
    {
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        if (rightControllerAnchor == null || raycastManager == null) return;

        var ray = new Ray(rightControllerAnchor.position, rightControllerAnchor.forward);

        if (raycastManager.Raycast(ray, out var hit))
        {
            // Mark this as true so it gets transparency applied
            UpdateMarker(ref previewVisual, hit.point, true, isPreview: true);
        }
        else
        {
            UpdateMarker(ref previewVisual, Vector3.zero, false, isPreview: true);
        }
    }

    public void SetAnchor()
    {
        if (rightControllerAnchor == null || raycastManager == null)
        {
            Debug.LogWarning("RayCaster: Missing controller anchor or raycast manager.");
            return;
        }

        var ray = new Ray(rightControllerAnchor.position, rightControllerAnchor.forward);

        if (raycastManager.Raycast(ray, out var hit))
        {
            // STATE 1: Start new cycle
            if (originSet == false || directionSet == true)
            {
                originAnchor = hit.point;
                directionAnchor = Vector3.zero;
                originSet = true;
                directionSet = false;

                // isPreview is false by default
                UpdateMarker(ref originVisual, originAnchor, true);
                UpdateMarker(ref directionVisual, Vector3.zero, false);

                Debug.Log("RayCaster: Origin set. Waiting for direction...");
            }
            // STATE 2: Finishing the pair
            else
            {
                directionAnchor = hit.point;
                directionSet = true;

                // isPreview is false by default
                UpdateMarker(ref directionVisual, directionAnchor, true);

                SetGrid();
                Debug.Log("RayCaster: Direction set. Grid updated.");
            }
        }
    }

    private void SetGrid()
    {
        if (gridPointRenderer != null && originSet && directionSet)
        {
            gridPointRenderer.origin = originAnchor;
            Vector3 direction = directionAnchor - originAnchor;
            Vector3 projected = new Vector3(direction.x, 0, direction.z).normalized;
            gridPointRenderer.xVec = projected;
            gridPointRenderer.isGridSet = true; // Mark grid as explicitly set by user
            gridPointRenderer.isSnapEnabled = true; // Enable snap when grid is set
            gridPointRenderer.RefreshGrid();
        }
    }

    // MODIFIED HELPER: Now accepts an optional isPreview flag
    private void UpdateMarker(ref GameObject markerInstance, Vector3 position, bool isVisible, bool isPreview = false)
    {
        if (surfaceAnchor == null) return;

        // 1. Initialization (Runs ONCE when the object is first created)
        if (markerInstance == null)
        {
            markerInstance = Instantiate(surfaceAnchor);

            if (markerInstance.GetComponent<Collider>())
                Destroy(markerInstance.GetComponent<Collider>());

            // --- NEW: Handle Transparency ---
            // If this is the preview marker, make it semi-transparent.
            // We do this here so it only happens once, rather than every frame in Update.
            if (isPreview)
            {
                Renderer rend = markerInstance.GetComponentInChildren<Renderer>();
                if (rend != null)
                {
                    // Accessing .material (instead of .sharedMaterial) automatically creates 
                    // a unique instance of the material for this specific object.
                    Color customColor = rend.material.color;
                    customColor.a = 0.2f; // Set alpha to 20%
                    rend.material.color = customColor;
                }
            }
        }

        // 2. Set Visibility
        markerInstance.SetActive(isVisible);

        // 3. Set Position if visible
        if (isVisible)
        {
            markerInstance.transform.position = position;
            markerInstance.transform.rotation = Quaternion.identity;
        }
    }
}