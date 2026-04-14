using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Renders a compact 2D line graph of compliance history in world space.
/// Attach as a child GameObject of the ModeDisplayUI and wire in Inspector.
///
/// Layout (local space, Z forward):
///   - Past segment  : green→red gradient (low→high compliance)
///   - Future segment: gray  (only in Rollback tool)
///   - Cursor bar    : gold  (only in Rollback tool)
///   - Best marker   : cyan horizontal tick above the best data point
/// </summary>
public class ComplianceGraphUI : MonoBehaviour
{
    [Header("Layout (world / local units)")]
    public Vector2 graphSize   = new Vector2(0.30f, 0.055f);
    public float   lineWidth   = 0.0022f;
    public float   cursorWidth = 0.0030f;

    [Header("Colors")]
    public Color pastLowColor  = new Color(0.30f, 0.85f, 0.30f, 1f);  // green — low compliance
    public Color pastHighColor = new Color(0.90f, 0.20f, 0.20f, 1f);  // red   — high compliance
    public Color futureColor   = new Color(0.50f, 0.50f, 0.50f, 1f);  // gray
    public Color cursorColor   = new Color(1.00f, 0.88f, 0.00f, 1f);  // gold
    public Color bestColor     = new Color(0.20f, 0.85f, 1.00f, 1f);  // cyan

    [Tooltip("Best-mark line thickness (world units)")]
    public float bestMarkWidth = 0.006f;

    [Header("Best Marker")]
    [Tooltip("Horizontal half-span of the tick, as fraction of graph width")]
    public float bestMarkTickFraction   = 0.025f;
    [Tooltip("Vertical offset above data point, as fraction of graph height")]
    public float bestMarkOffsetFraction = 0.12f;

    [Header("Material (assign an unlit/vertex-color mat; auto-created if null)")]
    public Material graphMaterial;

    // ------------------------------------------------------------------ //
    private LineRenderer  _past;
    private LineRenderer  _future;
    private LineRenderer  _cursor;
    private LineRenderer  _bestMark;
    private Material      _mat;
    private RectTransform _rt;

    // ------------------------------------------------------------------ //
    // Lifecycle
    // ------------------------------------------------------------------ //

    void Awake()
    {
        _rt  = GetComponent<RectTransform>();

        _mat = graphMaterial != null
            ? new Material(graphMaterial)
            : BuildFallbackMaterial();

        _past     = MakeLR("Past",     pastLowColor, lineWidth);
        _future   = MakeLR("Future",   futureColor,  lineWidth);
        _cursor   = MakeLR("Cursor",   cursorColor,  cursorWidth);
        _bestMark = MakeLR("BestMark", bestColor,    bestMarkWidth);

        _cursor.positionCount   = 2;
        _bestMark.positionCount = 2;
    }

    void OnDestroy()
    {
        if (_mat != null) { Destroy(_mat); }
    }

    // ------------------------------------------------------------------ //
    // Public API
    // ------------------------------------------------------------------ //

    public void SetVisible(bool visible) { gameObject.SetActive(visible); }

    /// <summary>
    /// Redraw the graph.
    /// showCursor=true when in Rollback tool: splits line at cursor and draws cursor bar.
    /// </summary>
    public void UpdateGraph(
        IReadOnlyList<StructureSnapshot> history,
        int cursorIndex, int bestIndex, bool showCursor)
    {
        if (history == null || history.Count == 0)
        {
            _past.positionCount   = 0;
            _future.positionCount = 0;
            _cursor.enabled       = false;
            _bestMark.enabled     = false;
            return;
        }

        // ── Dimensions: RectTransform local pixels, or graphSize fallback ──
        float w, h;
        if (_rt != null)
        {
            w = Mathf.Abs(_rt.rect.width);
            h = Mathf.Abs(_rt.rect.height);
            if (w < 1e-3f || h < 1e-3f) { w = graphSize.x; h = graphSize.y; }
        }
        else { w = graphSize.x; h = graphSize.y; }

        float hw   = w * 0.5f;
        float hh   = h * 0.5f;
        float tick  = w * bestMarkTickFraction;
        float tickY = h * bestMarkOffsetFraction;
        int   count = history.Count;

        // ── Y normalisation (double to avoid float cancellation) ──────────
        double minC = double.MaxValue, maxC = double.MinValue;
        for (int i = 0; i < count; i++)
        {
            double c = history[i].compliance;
            if (c < minC) { minC = c; }
            if (c > maxC) { maxC = c; }
        }
        double range = maxC - minC;
        bool   flat  = !(range > 0.0);
        if (flat) { minC -= 0.5; range = 1.0; }  // center all points at y=0

        // ── Single history point → dot at centre ─────────────────────────
        if (count == 1)
        {
            _past.positionCount = 1;
            ApplyGradient(_past, history, 0, 0, minC, range);
            _past.SetPosition(0, Vector3.zero);
            _future.positionCount = 0;
            _cursor.enabled       = false;
            DrawBestMark(bestIndex, count, w, hw, 0f, tick, tickY, null);
            return;
        }

        // ── Pt helper ─────────────────────────────────────────────────────
        System.Func<int, Vector3> Pt = idx =>
        {
            float x = (float)idx / (count - 1) * w - hw;
            float y = (float)((history[idx].compliance - minC) / range * h) - hh;
            return new Vector3(x, y, 0f);
        };

        // ── Lines ─────────────────────────────────────────────────────────
        if (!showCursor)
        {
            // No cursor: full compliance-gradient past line
            _past.positionCount = count;
            ApplyGradient(_past, history, 0, count - 1, minC, range);
            for (int i = 0; i < count; i++) { _past.SetPosition(i, Pt(i)); }
            _future.positionCount = 0;
            _cursor.enabled       = false;
        }
        else
        {
            // Past segment (0 … cursorIndex) — color-coded
            if (cursorIndex > 0)
            {
                _past.positionCount = cursorIndex + 1;
                ApplyGradient(_past, history, 0, cursorIndex, minC, range);
                for (int i = 0; i <= cursorIndex; i++) { _past.SetPosition(i, Pt(i)); }
            }
            else { _past.positionCount = 0; }

            // Future segment (cursorIndex … count-1) — gray
            if (cursorIndex < count - 1)
            {
                int futurePts = count - cursorIndex;
                _future.positionCount = futurePts;
                for (int i = 0; i < futurePts; i++) { _future.SetPosition(i, Pt(cursorIndex + i)); }
            }
            else { _future.positionCount = 0; }

            // Cursor bar — always shown when showCursor=true
            float cx = (float)cursorIndex / (count - 1) * w - hw;
            _cursor.SetPosition(0, new Vector3(cx, -hh, -0.001f));
            _cursor.SetPosition(1, new Vector3(cx,  hh, -0.001f));
            _cursor.enabled = true;
        }

        // ── Best marker ───────────────────────────────────────────────────
        DrawBestMark(bestIndex, count, w, hw, 0f, tick, tickY, Pt);
    }

    // ------------------------------------------------------------------ //
    // Helpers
    // ------------------------------------------------------------------ //

    void DrawBestMark(int bestIndex, int count, float w, float hw, float flatY,
                      float tick, float tickY,
                      System.Func<int, Vector3> Pt)
    {
        if (bestIndex < 0 || bestIndex >= count) { _bestMark.enabled = false; return; }
        Vector3 pt = (Pt != null) ? Pt(bestIndex)
                                  : new Vector3((count > 1 ? (float)bestIndex / (count - 1) * w - hw : 0f), flatY, 0f);
        _bestMark.SetPosition(0, new Vector3(pt.x - tick, pt.y + tickY, -0.001f));
        _bestMark.SetPosition(1, new Vector3(pt.x + tick, pt.y + tickY, -0.001f));
        _bestMark.enabled = true;
    }

    /// <summary>
    /// Sets a green→red compliance gradient on the LineRenderer.
    /// Keys are clamped to Unity's 8-key limit.
    /// </summary>
    void ApplyGradient(LineRenderer lr,
                       IReadOnlyList<StructureSnapshot> history,
                       int startIdx, int endIdx,
                       double minC, double range)
    {
        int n = endIdx - startIdx + 1;
        if (n <= 0) { return; }

        int maxKeys = Mathf.Min(n, 8);
        var colorKeys = new GradientColorKey[maxKeys];
        var alphaKeys = new GradientAlphaKey[maxKeys];

        for (int k = 0; k < maxKeys; k++)
        {
            float kFrac = (maxKeys > 1) ? (float)k / (maxKeys - 1) : 0f;
            int   idx   = startIdx + Mathf.RoundToInt(kFrac * (n - 1));
            float t     = (float)((history[idx].compliance - minC) / range);
            colorKeys[k] = new GradientColorKey(Color.Lerp(pastLowColor, pastHighColor, t), kFrac);
            alphaKeys[k] = new GradientAlphaKey(1f, kFrac);
        }

        var grad = new Gradient();
        grad.SetKeys(colorKeys, alphaKeys);
        lr.colorGradient = grad;
    }

    LineRenderer MakeLR(string n, Color c, float w)
    {
        var go = new GameObject(n);
        go.transform.SetParent(transform, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace     = false;
        lr.positionCount     = 0;
        lr.startWidth        = w;
        lr.endWidth          = w;
        lr.startColor        = c;
        lr.endColor          = c;
        lr.material          = _mat;
        lr.numCapVertices    = 3;
        lr.shadowCastingMode = ShadowCastingMode.Off;
        lr.receiveShadows    = false;
        return lr;
    }

    static Material BuildFallbackMaterial()
    {
        string[] candidates =
        {
            "Sprites/Default",
            "Universal Render Pipeline/Particles/Unlit",
            "Particles/Standard Unlit",
            "Hidden/Internal-Colored",
        };
        foreach (string name in candidates)
        {
            Shader s = Shader.Find(name);
            if (s != null) { return new Material(s); }
        }
        Debug.LogWarning("[ComplianceGraphUI] No suitable shader found. Assign graphMaterial in Inspector.");
        Shader fallback = Shader.Find("Diffuse") ?? Shader.Find("Standard");
        return fallback != null ? new Material(fallback) : new Material(Shader.Find("Hidden/Internal-Colored"));
    }
}
