using UnityEngine;

/// <summary>
/// Simple, reliable highlighting using color tinting
/// Works with any material/shader
/// </summary>
public class SimpleHighlight : MonoBehaviour
{
    private Renderer rend;
    private Material[] originalMaterials;
    private Material[] highlightMaterials;
    private Color highlightColor = Color.yellow;
    private bool isHighlighted = false;

    void Awake()
    {
        rend = GetComponent<Renderer>();
        if (rend != null)
        {
            originalMaterials = rend.materials;
        }
    }

    /// <summary>
    /// Apply highlight
    /// </summary>
    public void Highlight(Color color)
    {
        if (rend == null || originalMaterials == null) return;

        highlightColor = color;
        isHighlighted = true;

        // Create highlight materials
        highlightMaterials = new Material[originalMaterials.Length];
        for (int i = 0; i < originalMaterials.Length; i++)
        {
            highlightMaterials[i] = new Material(originalMaterials[i]);

            // Tint the color
            if (highlightMaterials[i].HasProperty("_Color"))
            {
                Color originalColor = originalMaterials[i].GetColor("_Color");
                Color tintedColor = originalColor * color * 2f;
                tintedColor.a = originalColor.a;
                highlightMaterials[i].SetColor("_Color", tintedColor);
            }
        }

        rend.materials = highlightMaterials;
    }

    /// <summary>
    /// Remove highlight
    /// </summary>
    public void ClearHighlight()
    {
        if (rend == null || originalMaterials == null) return;
        if (!isHighlighted) return;

        rend.materials = originalMaterials;
        isHighlighted = false;

        // Clean up highlight materials
        if (highlightMaterials != null)
        {
            foreach (var mat in highlightMaterials)
            {
                if (mat != null)
                    Destroy(mat);
            }
            highlightMaterials = null;
        }
    }

    void OnDestroy()
    {
        // Clean up highlight materials
        if (highlightMaterials != null)
        {
            foreach (var mat in highlightMaterials)
            {
                if (mat != null)
                    Destroy(mat);
            }
        }
    }
}
