using UnityEngine;

public class GridLineRenderer : MonoBehaviour
{
    [Header("Grid Definition")]
    public Vector3 origin = Vector3.zero;
    public Vector3 xVec = Vector3.right; // X direction vector
    public float spacing = 0.1f; // 10cm between grid lines
    public Vector3 size = new Vector3(2f, 2f, 2f); // Extent in world units

    [Header("Visual Settings")]
    public Color gridColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    public float lineWidth = 0.002f;
    public Material lineMaterial;

    [Header("Proximity Settings")]
    public Transform cursor; // Your VR controller or cursor object
    public float revealRadius = 0.3f; // How far around cursor to show grid (0.8 opacity)
    public float falloffDistance = 0.2f; // How gradual the fade is
    [Range(0f, 1f)]
    public float minOpacity = 0.1f; // Opacity outside radius
    [Range(0f, 1f)]
    public float maxOpacity = 0.8f; // Opacity inside radius

    private GameObject gridParent;

    void Update()
    {
        if (gridParent != null && gridParent.activeSelf && cursor != null)
        {
            UpdateCursorPosition(cursor.position);
        }
    }

    public void ShowGrid()
    {
        if (gridParent != null)
        {
            gridParent.SetActive(true);
            return;
        }

        gridParent = new GameObject("3D_Grid");
        gridParent.transform.SetParent(transform);

        // Prepare material - use the proximity shader
        if (lineMaterial == null)
        {
            lineMaterial = new Material(Shader.Find("Custom/ProximityGrid"));
            lineMaterial.color = gridColor;
        }

        // Set shader properties
        lineMaterial.SetFloat("_RevealRadius", revealRadius);
        lineMaterial.SetFloat("_FalloffDistance", falloffDistance);
        lineMaterial.SetFloat("_MinOpacity", minOpacity);
        lineMaterial.SetFloat("_MaxOpacity", maxOpacity);

        // Calculate basis vectors
        Vector3 xAxis = xVec.normalized;
        Vector3 zAxis = Vector3.forward; // Always world Z
        Vector3 yAxis = Vector3.Cross(zAxis, xAxis).normalized; // Y perpendicular to X and Z

        // Calculate number of grid lines needed
        int numLinesX = Mathf.CeilToInt(size.x / spacing) + 1;
        int numLinesY = Mathf.CeilToInt(size.y / spacing) + 1;
        int numLinesZ = Mathf.CeilToInt(size.z / spacing) + 1;

        // Draw lines parallel to X axis
        for (int y = 0; y < numLinesY; y++)
        {
            for (int z = 0; z < numLinesZ; z++)
            {
                Vector3 start = origin + (y * spacing) * yAxis + (z * spacing) * zAxis;
                Vector3 end = start + size.x * xAxis;
                CreateLineRenderer(start, end);
            }
        }

        // Draw lines parallel to Y axis
        for (int x = 0; x < numLinesX; x++)
        {
            for (int z = 0; z < numLinesZ; z++)
            {
                Vector3 start = origin + (x * spacing) * xAxis + (z * spacing) * zAxis;
                Vector3 end = start + size.y * yAxis;
                CreateLineRenderer(start, end);
            }
        }

        // Draw lines parallel to Z axis
        for (int x = 0; x < numLinesX; x++)
        {
            for (int y = 0; y < numLinesY; y++)
            {
                Vector3 start = origin + (x * spacing) * xAxis + (y * spacing) * yAxis;
                Vector3 end = start + size.z * zAxis;
                CreateLineRenderer(start, end);
            }
        }
    }

    public void UpdateCursorPosition(Vector3 cursorPos)
    {
        if (lineMaterial != null)
        {
            lineMaterial.SetVector("_CursorPosition", cursorPos);
        }
    }

    public void UpdateProximitySettings()
    {
        if (lineMaterial != null)
        {
            lineMaterial.SetFloat("_RevealRadius", revealRadius);
            lineMaterial.SetFloat("_FalloffDistance", falloffDistance);
            lineMaterial.SetFloat("_MinOpacity", minOpacity);
            lineMaterial.SetFloat("_MaxOpacity", maxOpacity);
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

    private void CreateLineRenderer(Vector3 start, Vector3 end)
    {
        GameObject lineObj = new GameObject("GridLine");
        lineObj.transform.SetParent(gridParent.transform);

        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.material = lineMaterial;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        lr.useWorldSpace = true;
    }
}