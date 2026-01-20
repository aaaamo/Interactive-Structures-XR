using UnityEngine;

/// <summary>
/// Controls the visual appearance of the controller marker based on current mode
/// </summary>
public class MarkerController : MonoBehaviour
{
    [Header("Mode Colors")]
    public Color addNodeColor = new Color(0.2f, 1f, 0.2f, 0.8f);      // Green
    public Color addEdgeColor = new Color(0.2f, 0.6f, 1f, 0.8f);      // Blue
    public Color addLoadColor = new Color(1f, 0.6f, 0.2f, 0.8f);      // Orange
    public Color toggleSupportColor = new Color(0.8f, 0.8f, 0.2f, 0.8f); // Yellow
    public Color moveColor = new Color(0.6f, 0.3f, 1f, 0.8f);         // Purple
    public Color deleteColor = new Color(1f, 0.2f, 0.2f, 0.8f);       // Red
    public Color grabColor = new Color(0.3f, 0.9f, 1f, 0.8f);         // Cyan
    public Color analyzeColor = new Color(1f, 1f, 1f, 0.8f);          // White
    public Color gridColor = new Color(0.7f, 0.7f, 0.7f, 0.5f);       // Gray
    public Color importColor = new Color(0.5f, 0.9f, 1f, 0.8f);       // Light Cyan

    [Header("Invalid Action Color")]
    public Color invalidColor = new Color(1f, 0f, 0f, 0.5f);          // Transparent red

    private Renderer markerRenderer;
    private Material markerMaterial;
    private Color currentColor;

    void Awake()
    {
        markerRenderer = GetComponent<Renderer>();
        if (markerRenderer != null)
        {
            // Create instance of material to avoid modifying the shared material
            markerMaterial = markerRenderer.material;
        }
    }

    /// <summary>
    /// Update marker color based on current mode
    /// </summary>
    public void SetModeColor(OVRGraphController.Mode mode)
    {
        Color targetColor = GetColorForMode(mode);
        SetColor(targetColor);
    }

    /// <summary>
    /// Set marker to valid action color (slightly brighter)
    /// </summary>
    public void SetValidAction()
    {
        if (markerMaterial != null)
        {
            markerMaterial.color = currentColor * 1.3f;
        }
    }

    /// <summary>
    /// Set marker to invalid action color
    /// </summary>
    public void SetInvalidAction()
    {
        SetColor(invalidColor);
    }

    /// <summary>
    /// Reset to current mode color
    /// </summary>
    public void ResetToModeColor(OVRGraphController.Mode mode)
    {
        SetModeColor(mode);
    }

    /// <summary>
    /// Set marker color directly
    /// </summary>
    public void SetColor(Color color)
    {
        currentColor = color;
        if (markerMaterial != null)
        {
            markerMaterial.color = color;
        }
    }

    /// <summary>
    /// Get color for a specific mode
    /// </summary>
    private Color GetColorForMode(OVRGraphController.Mode mode)
    {
        switch (mode)
        {
            case OVRGraphController.Mode.AddNode:
                return addNodeColor;
            case OVRGraphController.Mode.AddEdge:
                return addEdgeColor;
            case OVRGraphController.Mode.AddLoad:
                return addLoadColor;
            case OVRGraphController.Mode.ToggleSupport:
                return toggleSupportColor;
            case OVRGraphController.Mode.Move:
                return moveColor;
            case OVRGraphController.Mode.Delete:
                return deleteColor;
            case OVRGraphController.Mode.Grab:
                return grabColor;
            case OVRGraphController.Mode.Analyze:
                return analyzeColor;
            case OVRGraphController.Mode.Grid:
                return gridColor;
            case OVRGraphController.Mode.Import:
                return importColor;
            default:
                return Color.white;
        }
    }

    /// <summary>
    /// Pulse the marker (for feedback)
    /// </summary>
    public void Pulse(float duration = 0.2f)
    {
        StartCoroutine(PulseCoroutine(duration));
    }

    private System.Collections.IEnumerator PulseCoroutine(float duration)
    {
        Vector3 originalScale = transform.localScale;
        Vector3 targetScale = originalScale * 1.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.localScale = Vector3.Lerp(originalScale, targetScale, Mathf.Sin(t * Mathf.PI));
            yield return null;
        }

        transform.localScale = originalScale;
    }
}
