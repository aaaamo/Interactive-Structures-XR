using UnityEngine;
using TMPro;

/// <summary>
/// Floating mode-selection panel that appears when the user presses Y (left controller).
/// Shows Setup / Build / Optimize as selectable options.
/// The user navigates with right thumbstick X and confirms with right trigger.
///
/// Attach to a world-space Canvas GameObject.
/// Wire the three modeButtons TextMeshPro fields in the Inspector (Setup, Build, Optimize).
/// </summary>
public class ModePanelUI : MonoBehaviour
{
    [Header("References")]
    public GameObject panelRoot;
    public TextMeshPro[] modeButtons; // [0]=Setup, [1]=Build, [2]=Optimize

    [Header("Colors")]
    public Color normalColor   = new Color(0.7f, 0.7f, 0.7f);
    public Color selectedColor = Color.white;
    public Color cursorColor   = new Color(1f, 0.85f, 0f); // gold

    // ------------------------------------------------------------------ //
    // State
    // ------------------------------------------------------------------ //

    private int _cursorIndex;

    private GameObject PanelRoot => panelRoot != null ? panelRoot : gameObject;
    public bool IsVisible => PanelRoot.activeSelf;

    // ------------------------------------------------------------------ //
    // Public API
    // ------------------------------------------------------------------ //

    /// <summary>Open the panel and highlight the currently active mode.</summary>
    public void Show(OVRGraphController.Mode currentMode)
    {
        _cursorIndex = (int)currentMode;
        PanelRoot.SetActive(true);
        RefreshDisplay(currentMode);
    }

    /// <summary>Close the panel.</summary>
    public void Hide()
    {
        PanelRoot.SetActive(false);
    }

    /// <summary>Move the cursor by delta (-1 = left, +1 = right).</summary>
    public void MoveCursor(int delta)
    {
        int count = System.Enum.GetValues(typeof(OVRGraphController.Mode)).Length;
        _cursorIndex = (_cursorIndex + delta + count) % count;
        RefreshButtonColors(-1); // -1 = no active mode highlight during navigation
    }

    /// <summary>Returns the mode currently under the cursor.</summary>
    public OVRGraphController.Mode GetSelected()
    {
        return (OVRGraphController.Mode)_cursorIndex;
    }

    // ------------------------------------------------------------------ //
    // Display
    // ------------------------------------------------------------------ //

    private void RefreshDisplay(OVRGraphController.Mode activeMode)
    {
        RefreshButtonColors((int)activeMode);
    }

    private void RefreshButtonColors(int activeIndex)
    {
        if (modeButtons == null) { return; }
        for (int i = 0; i < modeButtons.Length; i++)
        {
            if (modeButtons[i] == null) { continue; }
            if (i == _cursorIndex)
            {
                modeButtons[i].color    = cursorColor;
                modeButtons[i].fontStyle = FontStyles.Bold;
            }
            else if (i == activeIndex)
            {
                modeButtons[i].color    = selectedColor;
                modeButtons[i].fontStyle = FontStyles.Normal;
            }
            else
            {
                modeButtons[i].color    = normalColor;
                modeButtons[i].fontStyle = FontStyles.Normal;
            }
        }
    }

    private void Awake()
    {
        // Start hidden — PanelRoot falls back to gameObject if panelRoot not assigned
        PanelRoot.SetActive(false);
    }
}
