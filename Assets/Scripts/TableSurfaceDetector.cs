using UnityEngine;
using System.Collections.Generic;
using Meta.XR.MRUtilityKit;
using System.Reflection;

public class TableSurfaceDetector : MonoBehaviour
{
    [Header("Grid Reference")]
    public GridPointRenderer gridRenderer;
    
    [Header("Detection Settings")]
    public bool autoDetectOnStart = true;
    public float minTableArea = 0.2f; // Minimum table size in square meters
    public bool useDefaultPositionIfNoScene = true; // For testing without scene data
    
    private MRUKAnchor detectedTable;
    private bool tableFound = false;

    void Start()
    {
        if (autoDetectOnStart)
        {
            // Wait for MRUK to initialize
            if (MRUK.Instance != null && MRUK.Instance.IsInitialized)
            {
                DetectTable();
            }
            else if (MRUK.Instance != null)
            {
                MRUK.Instance.RegisterSceneLoadedCallback(OnSceneLoaded);
            }
            else
            {
                // MRUK not available (e.g., in editor without XR Sim)
                Debug.LogWarning("[TABLE] MRUK not available. Using default grid position.");
                if (useDefaultPositionIfNoScene)
                {
                    Invoke(nameof(PlaceGridAtDefaultPosition), 0.5f);
                }
            }
        }
    }

    void OnSceneLoaded()
    {
        Debug.Log("[TABLE] Scene loaded callback triggered");
        DetectTable();
    }

    public void DetectTable()
    {
        Debug.LogWarning("[TABLE] DETECTING TABLE!");

        if (MRUK.Instance == null || !MRUK.Instance.IsInitialized)
        {
            Debug.LogWarning("[TABLE] MRUK not initialized! Using default position for development.");
            if (useDefaultPositionIfNoScene)
            {
                PlaceGridAtDefaultPosition();
            }
            return;
        }

        MRUKRoom currentRoom = MRUK.Instance.GetCurrentRoom();
        
        if (currentRoom == null)
        {
            Debug.LogWarning("[TABLE] No room detected. Make sure you've run Space Setup on your Quest.");
            if (useDefaultPositionIfNoScene)
            {
                PlaceGridAtDefaultPosition();
            }
            return;
        }

        Debug.Log($"[TABLE] Room found! Searching for tables...");

        // Find all tables in the room
        List<MRUKAnchor> tables = CollectAnchorsByLabel(currentRoom, MRUKAnchor.SceneLabels.TABLE);
        
        Debug.Log($"[TABLE] Found {tables.Count} tables in room");

        // Filter by minimum area
        List<MRUKAnchor> validTables = new List<MRUKAnchor>();
        foreach (var table in tables)
        {
            if (table.PlaneRect.HasValue)
            {
                Rect rect = table.PlaneRect.Value;
                float area = rect.width * rect.height;
                
                if (area >= minTableArea)
                {
                    validTables.Add(table);
                    Debug.Log($"[TABLE] Valid table found: {rect.width:F2}m x {rect.height:F2}m (area: {area:F2}m²)");
                }
            }
        }

        if (validTables.Count > 0)
        {
            // Use the largest table
            detectedTable = GetLargestTable(validTables);
            PlaceGridOnTable(detectedTable);
            tableFound = true;
        }
        else
        {
            Debug.LogWarning("[TABLE] No valid tables found. Checking for other surfaces...");
            
            // Try other surfaces as fallback (desks, couches, etc.)
            List<MRUKAnchor> otherSurfaces = CollectAnchorsByLabel(currentRoom, MRUKAnchor.SceneLabels.OTHER);
            
            if (otherSurfaces.Count > 0)
            {
                detectedTable = GetLargestTable(otherSurfaces);
                PlaceGridOnTable(detectedTable);
                tableFound = true;
            }
            else
            {
                Debug.LogWarning("[TABLE] No surfaces found. Using default position.");
                PlaceGridAtDefaultPosition();
            }
        }
    }

    MRUKAnchor GetLargestTable(List<MRUKAnchor> tables)
    {
        MRUKAnchor largest = tables[0];
        float maxArea = 0f;

        foreach (var table in tables)
        {
            if (table.PlaneRect.HasValue)
            {
                Rect rect = table.PlaneRect.Value;
                float area = rect.width * rect.height;
                
                if (area > maxArea)
                {
                    maxArea = area;
                    largest = table;
                }
            }
        }

        return largest;
    }

    void PlaceGridOnTable(MRUKAnchor tableAnchor)
    {
        Debug.LogWarning("[TABLE] PLACING ON TABLE!");
        if (gridRenderer == null)
        {
            Debug.LogWarning("[TABLE] Grid renderer not assigned!");
            return;
        }

        // Get table dimensions
        Vector2 tableDimensions = Vector2.one;
        if (tableAnchor.PlaneRect.HasValue)
        {
            Rect rect = tableAnchor.PlaneRect.Value;
            tableDimensions = new Vector2(rect.width, rect.height);
            Debug.Log($"[TABLE] Table dimensions: {tableDimensions.x:F2}m x {tableDimensions.y:F2}m");
        }

        // Position grid renderer's transform at table surface
        //gridRenderer.transform.position = tableAnchor.transform.position;
        //gridRenderer.transform.rotation = tableAnchor.transform.rotation;

        // Update grid origin and size to match table
        //gridRenderer.origin = Vector3.zero; // Local to grid renderer
        gridRenderer.size = new Vector3(tableDimensions.x, 0.5f, tableDimensions.y); // Keep some Y height

        // Set X axis to align with table's local X
        //gridRenderer.xVec = Vector3.right; // Will be in grid renderer's local space
        gridRenderer.origin = tableAnchor.transform.position;
        gridRenderer.xVec = tableAnchor.transform.right;

        Debug.Log($"[TABLE] Grid successfully placed on table surface at {tableAnchor.transform.position}");
        
        // Show the grid
        gridRenderer.ShowGrid();
    }

    void PlaceGridAtDefaultPosition()
    {
        Debug.LogWarning("[TABLE] PLACING ON DEFAULT!");
        if (gridRenderer == null)
        {
            Debug.LogWarning("[TABLE] Grid renderer not assigned!");
            return;
        }

        // Place grid 0.5m in front of user at waist height
        Transform centerEye = Camera.main.transform;
        Vector3 forward = centerEye.forward;
        forward.y = 0; // Keep horizontal
        forward.Normalize();

        gridRenderer.transform.position = centerEye.position + forward * 0.5f - Vector3.up * 0.3f;
        gridRenderer.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);

        // Set default grid size
        gridRenderer.origin = Vector3.zero;
        gridRenderer.size = new Vector3(1f, 0.3f, 1f);
        gridRenderer.xVec = Vector3.right;

        Debug.Log("[TABLE] No table found - grid placed at default position");
        
        // Show the grid
        gridRenderer.ShowGrid();
    }

    // Allow manual re-detection during runtime
    void Update()
    {
        // Example: Use A button on right controller to re-detect
        if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch))
        {
            Debug.Log("[TABLE] Manual table detection triggered");
            DetectTable();
        }
    }

    public bool IsTableDetected()
    {
        return tableFound;
    }

    public Vector3 GetTablePosition()
    {
        return detectedTable != null ? detectedTable.transform.position : Vector3.zero;
    }

    public Vector2 GetTableDimensions()
    {
        if (detectedTable != null && detectedTable.PlaneRect.HasValue)
        {
            Rect rect = detectedTable.PlaneRect.Value;
            return new Vector2(rect.width, rect.height);
        }
        return Vector2.zero;
    }

    public MRUKAnchor GetDetectedTable()
    {
        return detectedTable;
    }

    // Optional: Visualize all detected tables for debugging
    public void VisualizeAllTables()
    {
        if (MRUK.Instance == null || !MRUK.Instance.IsInitialized) return;

        MRUKRoom currentRoom = MRUK.Instance.GetCurrentRoom();
        if (currentRoom == null) return;

        List<MRUKAnchor> tables = CollectAnchorsByLabel(currentRoom, MRUKAnchor.SceneLabels.TABLE);

        Debug.Log($"[TABLE] === All Detected Tables ({tables.Count}) ===");
        foreach (var table in tables)
        {
            if (table.PlaneRect.HasValue)
            {
                Rect rect = table.PlaneRect.Value;
                Debug.Log($"[TABLE] Table at {table.transform.position}: {rect.width:F2}m x {rect.height:F2}m");
            }
        }
    }

    // Helper: Collect anchors that match a given scene label.
    // Uses reflection to find a property/field on MRUKAnchor instances that stores the label.
    private List<MRUKAnchor> CollectAnchorsByLabel(MRUKRoom room, MRUKAnchor.SceneLabels label)
    {
        List<MRUKAnchor> result = new List<MRUKAnchor>();

        // Get all MRUKAnchor components in the scene
        MRUKAnchor[] allAnchors = Object.FindObjectsOfType<MRUKAnchor>();

        foreach (var anchor in allAnchors)
        {
            // Try to find a property or field on the anchor that is of the enum type MRUKAnchor.SceneLabels
            var anchorType = anchor.GetType();
            object labelValue = null;

            // Try common property names first
            PropertyInfo prop = anchorType.GetProperty("SceneLabel") ?? anchorType.GetProperty("sceneLabel")
                                     ?? anchorType.GetProperty("Label") ?? anchorType.GetProperty("label");
            if (prop != null && prop.PropertyType == typeof(MRUKAnchor.SceneLabels))
            {
                labelValue = prop.GetValue(anchor);
            }
            else
            {
                // Try fields as fallback
                FieldInfo field = anchorType.GetField("SceneLabel") ?? anchorType.GetField("sceneLabel")
                                      ?? anchorType.GetField("Label") ?? anchorType.GetField("label");
                if (field != null && field.FieldType == typeof(MRUKAnchor.SceneLabels))
                {
                    labelValue = field.GetValue(anchor);
                }
            }

            if (labelValue != null && (MRUKAnchor.SceneLabels)labelValue == label)
            {
                result.Add(anchor);
            }
        }

        return result;
    }
}