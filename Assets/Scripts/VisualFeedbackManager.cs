using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages visual feedback for interactive elements (hover, selection, highlights)
/// </summary>
public class VisualFeedbackManager : MonoBehaviour
{
    [Header("Highlight Materials")]
    public Material hoverMaterial;
    public Material selectedMaterial;
    public Material errorMaterial;
    public Material validMaterial;

    [Header("Hover Colors")]
    public Color hoverColorNode = new Color(1f, 1f, 0f, 1f);        // Yellow
    public Color hoverColorEdge = new Color(1f, 0.8f, 0f, 1f);      // Orange
    public Color hoverColorLoad = new Color(0.5f, 1f, 0.5f, 1f);    // Light green
    public Color deleteHoverColor = new Color(1f, 0f, 0f, 1f);      // Red
    public Color grabHoverColor = new Color(0.3f, 0.7f, 1f, 1f);    // Blue

    [Header("Outline Settings")]
    public float outlineWidth = 0.015f;

    // Track currently highlighted objects
    private GameObject currentlyHovered;
    private HashSet<GameObject> currentlySelected = new HashSet<GameObject>();
    private Dictionary<GameObject, Material[]> originalMaterials = new Dictionary<GameObject, Material[]>();

    // Singleton
    private static VisualFeedbackManager _instance;
    public static VisualFeedbackManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<VisualFeedbackManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("VisualFeedbackManager");
                    _instance = go.AddComponent<VisualFeedbackManager>();
                }
            }
            return _instance;
        }
    }

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Highlight an object on hover
    /// </summary>
    public void HighlightHover(GameObject obj, Color highlightColor)
    {
        if (obj == null) return;

        // Clear previous hover
        ClearHover();

        currentlyHovered = obj;

        // Use SimpleHighlight component for reliable highlighting
        SimpleHighlight highlight = obj.GetComponent<SimpleHighlight>();
        if (highlight == null)
        {
            highlight = obj.AddComponent<SimpleHighlight>();
        }
        highlight.Highlight(highlightColor);
    }

    /// <summary>
    /// Clear hover highlight
    /// </summary>
    public void ClearHover()
    {
        if (currentlyHovered != null)
        {
            SimpleHighlight highlight = currentlyHovered.GetComponent<SimpleHighlight>();
            if (highlight != null)
            {
                highlight.ClearHighlight();
            }
            currentlyHovered = null;
        }
    }

    /// <summary>
    /// Mark object as selected (persistent highlight)
    /// </summary>
    public void SetSelected(GameObject obj, Color selectionColor)
    {
        if (obj == null) return;

        if (!currentlySelected.Contains(obj))
        {
            currentlySelected.Add(obj);

            SimpleHighlight highlight = obj.GetComponent<SimpleHighlight>();
            if (highlight == null)
            {
                highlight = obj.AddComponent<SimpleHighlight>();
            }
            highlight.Highlight(selectionColor);
        }
    }

    /// <summary>
    /// Clear selection
    /// </summary>
    public void ClearSelection(GameObject obj = null)
    {
        if (obj == null)
        {
            // Clear all selections
            foreach (var selected in currentlySelected)
            {
                SimpleHighlight highlight = selected.GetComponent<SimpleHighlight>();
                if (highlight != null)
                {
                    highlight.ClearHighlight();
                }
            }
            currentlySelected.Clear();
        }
        else if (currentlySelected.Contains(obj))
        {
            currentlySelected.Remove(obj);

            SimpleHighlight highlight = obj.GetComponent<SimpleHighlight>();
            if (highlight != null)
            {
                highlight.ClearHighlight();
            }
        }
    }

    /// <summary>
    /// Highlight multiple connected objects (for Grab mode)
    /// </summary>
    public void HighlightConnectedStructure(HashSet<NodeBehaviour> nodes, Color highlightColor)
    {
        foreach (var node in nodes)
        {
            if (node != null)
            {
                SimpleHighlight highlight = node.gameObject.GetComponent<SimpleHighlight>();
                if (highlight == null)
                {
                    highlight = node.gameObject.AddComponent<SimpleHighlight>();
                }
                highlight.Highlight(highlightColor);
            }
        }
    }

    /// <summary>
    /// Clear connected structure highlights
    /// </summary>
    public void ClearConnectedHighlight(HashSet<NodeBehaviour> nodes)
    {
        foreach (var node in nodes)
        {
            if (node != null)
            {
                SimpleHighlight highlight = node.gameObject.GetComponent<SimpleHighlight>();
                if (highlight != null)
                {
                    highlight.ClearHighlight();
                }
            }
        }
    }

    /// <summary>
    /// Apply outline effect using emission or color tint
    /// </summary>
    private void ApplyOutlineEffect(GameObject obj, Color color)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer == null) return;

        // Store original materials if not already stored
        if (!originalMaterials.ContainsKey(obj))
        {
            originalMaterials[obj] = renderer.materials;
        }

        // Create a copy of materials and apply highlight
        Material[] mats = renderer.materials;
        for (int i = 0; i < mats.Length; i++)
        {
            mats[i] = new Material(mats[i]);

            // Try emission first (works with Standard shader)
            if (mats[i].HasProperty("_EmissionColor"))
            {
                mats[i].EnableKeyword("_EMISSION");
                mats[i].SetColor("_EmissionColor", color * 2f);
                mats[i].globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }

            // Fallback: brighten the color
            if (mats[i].HasProperty("_Color"))
            {
                Color originalColor = mats[i].GetColor("_Color");
                Color highlightColor = Color.Lerp(originalColor, color, 0.5f);
                // Make it brighter
                highlightColor *= 1.5f;
                highlightColor.a = originalColor.a;
                mats[i].SetColor("_Color", highlightColor);
            }
        }
        renderer.materials = mats;
    }

    /// <summary>
    /// Remove outline effect
    /// </summary>
    private void RemoveOutlineEffect(GameObject obj)
    {
        if (obj == null) return;

        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer == null) return;

        // Restore original materials
        if (originalMaterials.ContainsKey(obj))
        {
            renderer.materials = originalMaterials[obj];
            originalMaterials.Remove(obj);
        }
    }

    /// <summary>
    /// Add a pulsing effect to an object
    /// </summary>
    public void AddPulseEffect(GameObject obj, Color color, float speed = 2f)
    {
        PulseEffect pulse = obj.GetComponent<PulseEffect>();
        if (pulse == null)
            pulse = obj.AddComponent<PulseEffect>();
        pulse.pulseColor = color;
        pulse.pulseSpeed = speed;
        pulse.enabled = true;
    }

    /// <summary>
    /// Remove pulse effect
    /// </summary>
    public void RemovePulseEffect(GameObject obj)
    {
        PulseEffect pulse = obj.GetComponent<PulseEffect>();
        if (pulse != null)
            pulse.enabled = false;
    }
}

/// <summary>
/// Component for pulsing emission effect
/// </summary>
public class PulseEffect : MonoBehaviour
{
    public Color pulseColor = Color.yellow;
    public float pulseSpeed = 2f;
    private Renderer rend;
    private Material[] materials;

    void OnEnable()
    {
        rend = GetComponent<Renderer>();
        if (rend != null)
        {
            materials = rend.materials;
            foreach (var mat in materials)
            {
                mat.EnableKeyword("_EMISSION");
            }
        }
    }

    void Update()
    {
        if (materials == null) return;

        float emission = Mathf.PingPong(Time.time * pulseSpeed, 1f);
        Color emissionColor = pulseColor * emission;

        foreach (var mat in materials)
        {
            mat.SetColor("_EmissionColor", emissionColor);
        }
    }

    void OnDisable()
    {
        if (materials != null)
        {
            foreach (var mat in materials)
            {
                mat.SetColor("_EmissionColor", Color.black);
            }
        }
    }
}
