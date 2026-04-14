using UnityEngine;

/// <summary>
/// Controls the visual appearance of the controller marker based on current mode
/// </summary>
public class MarkerController : MonoBehaviour
{
    [Header("Mode Colors")]
    public Color setupColor    = new Color(1f,  0.9f, 0.3f,  0.8f);  // Yellow-Gold
    public Color buildColor    = new Color(0.2f, 1f,  0.2f,  0.8f);  // Green
    public Color optimizeColor = new Color(1f,  0.85f, 0f,   0.8f);  // Gold

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
            case OVRGraphController.Mode.Setup:    return setupColor;
            case OVRGraphController.Mode.Build:    return buildColor;
            case OVRGraphController.Mode.Optimize: return optimizeColor;
            default:                               return Color.white;
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
