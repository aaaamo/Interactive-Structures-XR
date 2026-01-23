using System.Collections.Generic;
using UnityEngine;

public class GridPointRenderer : MonoBehaviour
{
    [Header("Grid Definition")]
    public Vector3 origin = Vector3.zero;
    public Vector3 xVec = Vector3.right; // X direction vector
    public float spacing = 0.1f; // 10cm between grid points
    public Vector3 size = new Vector3(10, 10, 10);

    [Header("Visual Settings")]
    public Color gridColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    public float pointSize = 0.005f; // Size of each point (5mm)
    public Material pointMaterial;

    [Header("Proximity Settings")]
    public Transform cursor; // Your VR controller or cursor object
    public float revealRadius = 0.3f; // How far around cursor to show grid (0.8 opacity)
    public float falloffDistance = 0.2f; // How gradual the fade is
    [Range(0f, 1f)]
    public float minOpacity = 0.1f; // Opacity outside radius
    [Range(0f, 1f)]
    public float maxOpacity = 0.8f; // Opacity inside radius

    public WorldCoordinateManager worldCoordinateManager;
    private Transform ParentWorld => worldCoordinateManager?.parentWorld;

    private GameObject gridParent;
    public bool isActive;
    public bool isGridSet = false; // True only after user explicitly sets the grid anchor
    public bool isSnapEnabled = false; // Can be toggled on/off by user

    void Update()
    {
        // Continuously update the shader with the cursor position
        if (gridParent != null && gridParent.activeSelf && cursor != null)
        {
            UpdateCursorPosition(cursor.position);
        }
        isActive = gridParent != null && gridParent.activeSelf;
    }

    /// <summary>
    /// Call this after changing origin or xVec to update the visuals immediately.
    /// Only refreshes if grid is set.
    /// </summary>
    public void RefreshGrid()
    {
        if (!isGridSet) return;

        DestroyGrid(); // Clear old points
        ShowGrid();    // Build new points with current settings
    }

    public void ToggleGrid()
    {
        // Only toggle if grid is set
        if (!isGridSet) return;

        if (gridParent != null && gridParent.activeSelf)
        {
            HideGrid();
        }
        else
        {
            ShowGrid();
        }
    }

    public void ShowGrid()
    {
        // Only show grid if it has been explicitly set by user
        if (!isGridSet)
        {
            Debug.Log("[GridPointRenderer] Grid not set yet. Cannot show grid.");
            return;
        }

        // If grid exists, just enable it. Use RefreshGrid() to force rebuild.
        if (gridParent != null)
        {
            gridParent.SetActive(true);
            return;
        }

        gridParent = new GameObject("3D_Grid_Points");

        // Parent to ParentWorld if set, otherwise to this transform
        if (ParentWorld != null)
            gridParent.transform.SetParent(ParentWorld);
        else
            gridParent.transform.SetParent(transform);

        // Prepare material - use the proximity shader
        if (pointMaterial == null)
        {
            // Fallback if no material assigned
            Shader s = Shader.Find("Custom/ProximityGrid");
            if (s == null) s = Shader.Find("Standard");
            pointMaterial = new Material(s);
            pointMaterial.color = gridColor;
        }

        UpdateProximitySettings(); // Apply initial shader settings

        // Generate the points
        foreach (Vector3 pos in CalculateGridPoints())
        {
            CreatePoint(pos);
        }
    }

    public void HideGrid()
    {
        if (gridParent != null)
        {
            gridParent.SetActive(false);
        }
    }

    public void DestroyGrid()
    {
        if (gridParent != null)
        {
            Destroy(gridParent);
            gridParent = null;
        }
    }

    // -- MATH HELPERS --

    private void GetBasisVectors(out Vector3 xAxis, out Vector3 yAxis, out Vector3 zAxis)
    {
        xAxis = xVec.normalized;
        yAxis = Vector3.up;
        zAxis = Vector3.Cross(xAxis, yAxis).normalized;
    }

    private List<Vector3> CalculateGridPoints()
    {
        List<Vector3> points = new List<Vector3>();

        GetBasisVectors(out Vector3 xAxis, out Vector3 yAxis, out Vector3 zAxis);

        int numPointsX = Mathf.CeilToInt(size.x) + 1;
        int numPointsY = Mathf.CeilToInt(size.y) + 1;
        int numPointsZ = Mathf.CeilToInt(size.z) + 1;

        for (int x = 0; x < numPointsX; x++)
        {
            for (int y = 0; y < numPointsY; y++)
            {
                for (int z = 0; z < numPointsZ; z++)
                {
                    Vector3 position = origin
                        + (x * spacing) * xAxis
                        + (y * spacing) * yAxis
                        + (z * spacing) * zAxis;

                    points.Add(position);
                }
            }
        }
        return points;
    }

    private void CreatePoint(Vector3 position)
    {
        GameObject pointObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        pointObj.transform.SetParent(gridParent.transform);
        pointObj.transform.position = position;
        pointObj.transform.localScale = Vector3.one * pointSize;

        // Optimization: Remove collider to improve Raycast performance elsewhere
        Collider col = pointObj.GetComponent<Collider>();
        if (col != null) Destroy(col);

        Renderer renderer = pointObj.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = pointMaterial;
        }
    }

    public void UpdateCursorPosition(Vector3 cursorPos)
    {
        if (pointMaterial != null)
        {
            pointMaterial.SetVector("_CursorPosition", cursorPos);
        }
    }

    public void UpdateProximitySettings()
    {
        if (pointMaterial != null)
        {
            pointMaterial.SetFloat("_RevealRadius", revealRadius);
            pointMaterial.SetFloat("_FalloffDistance", falloffDistance);
            pointMaterial.SetFloat("_MinOpacity", minOpacity);
            pointMaterial.SetFloat("_MaxOpacity", maxOpacity);
            pointMaterial.color = gridColor;
        }
    }

    // Optimization: Mathematical Snapping (O(1)) instead of List Iteration (O(N))
    public Vector3 GetClosestGridPoint(Vector3 position)
    {
        GetBasisVectors(out Vector3 xAxis, out Vector3 yAxis, out Vector3 zAxis);

        // Convert world position to local grid space relative to origin
        Vector3 dir = position - origin;

        // Project dir onto our axes to get local coordinates
        float localX = Vector3.Dot(dir, xAxis);
        float localY = Vector3.Dot(dir, yAxis);
        float localZ = Vector3.Dot(dir, zAxis);

        // Snap local coordinates to spacing
        float snappedX = Mathf.Round(localX / spacing) * spacing;
        float snappedY = Mathf.Round(localY / spacing) * spacing;
        float snappedZ = Mathf.Round(localZ / spacing) * spacing;

        // Clamp to size bounds (optional, keeps point inside the drawn grid)
        // snappedX = Mathf.Clamp(snappedX, 0, Mathf.Ceil(size.x / spacing) * spacing);
        // ... repeat for Y and Z

        // Convert back to world space
        Vector3 closest = origin + (snappedX * xAxis) + (snappedY * yAxis) + (snappedZ * zAxis);

        // Safety check: if too far from any point, return original
        if (Vector3.SqrMagnitude(closest - position) > (spacing * spacing))
        {
            return position;
        }

        return closest;
    }
}