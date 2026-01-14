using UnityEngine;
using TMPro;

/// <summary>
/// Simple keyboard and VR button shortcuts for save/load
/// No UI needed - just quick shortcuts!
/// </summary>
public class SaveLoadShortcuts : MonoBehaviour
{
    [Header("References")]
    public SaveLoadManager saveLoadManager;

    [Header("Feedback")]
    public TextMeshPro feedbackText; // Optional: shows save/load messages
    public float feedbackDuration = 2f;

    private float feedbackTimer = 0f;
    private string lastSavedFile = "";

    void Update()
    {
        // Hide feedback message after duration
        if (feedbackTimer > 0)
        {
            feedbackTimer -= Time.deltaTime;
            if (feedbackTimer <= 0 && feedbackText != null)
            {
                feedbackText.gameObject.SetActive(false);
            }
        }

        // ========== KEYBOARD SHORTCUTS (for testing in editor) ==========

        // S = Quick Save
        if (Input.GetKeyDown(KeyCode.S))
        {
            QuickSave();
        }

        // L = Load last save
        if (Input.GetKeyDown(KeyCode.L))
        {
            LoadLastSave();
        }

        // ========== VR SHORTCUTS ==========

        // Left Grip + A = Quick Save
        if (OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.LTouch) &&
            OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.LTouch))
        {
            QuickSave();
        }

        // Left Grip + B = Load last save
        if (OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.LTouch) &&
            OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.LTouch))
        {
            LoadLastSave();
        }

        // Left Grip + X = Load example bridge
        if (OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.LTouch) &&
            OVRInput.GetDown(OVRInput.Button.Three, OVRInput.Controller.LTouch))
        {
            LoadExampleBridge();
        }
    }

    /// <summary>
    /// Quick save with timestamp
    /// </summary>
    void QuickSave()
    {
        if (saveLoadManager == null)
        {
            Debug.LogError("[SHORTCUTS] SaveLoadManager not assigned!");
            ShowFeedback("ERROR: No SaveLoadManager!", Color.red);
            return;
        }

        string fileName = "QuickSave_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        bool success = saveLoadManager.SaveStructure(fileName);

        if (success)
        {
            lastSavedFile = fileName;
            ShowFeedback($"SAVED: {fileName}", Color.green);
            Debug.Log($"[SHORTCUTS] Saved: {fileName}");
        }
        else
        {
            ShowFeedback("SAVE FAILED!", Color.red);
        }
    }

    /// <summary>
    /// Load the last saved file
    /// </summary>
    void LoadLastSave()
    {
        if (saveLoadManager == null)
        {
            Debug.LogError("[SHORTCUTS] SaveLoadManager not assigned!");
            ShowFeedback("ERROR: No SaveLoadManager!", Color.red);
            return;
        }

        // Try to load last saved file
        if (!string.IsNullOrEmpty(lastSavedFile))
        {
            bool success = saveLoadManager.LoadStructure(lastSavedFile);
            if (success)
            {
                ShowFeedback($"LOADED: {lastSavedFile}", Color.cyan);
                Debug.Log($"[SHORTCUTS] Loaded: {lastSavedFile}");
                return;
            }
        }

        // Otherwise, load most recent file
        var files = saveLoadManager.GetSavedStructures();
        if (files.Count > 0)
        {
            string mostRecent = files[files.Count - 1]; // Last in list is most recent
            bool success = saveLoadManager.LoadStructure(mostRecent);
            if (success)
            {
                lastSavedFile = mostRecent;
                ShowFeedback($"LOADED: {mostRecent}", Color.cyan);
                Debug.Log($"[SHORTCUTS] Loaded most recent: {mostRecent}");
            }
            else
            {
                ShowFeedback("LOAD FAILED!", Color.red);
            }
        }
        else
        {
            ShowFeedback("NO SAVES FOUND!", Color.yellow);
            Debug.LogWarning("[SHORTCUTS] No saved files found!");
        }
    }

    /// <summary>
    /// Load the example bridge
    /// </summary>
    void LoadExampleBridge()
    {
        ExampleStructures examples = FindObjectOfType<ExampleStructures>();
        if (examples != null)
        {
            examples.LoadSimpleBridge();
            ShowFeedback("LOADED: Example Bridge", Color.cyan);
            Debug.Log("[SHORTCUTS] Loaded example bridge");
        }
        else
        {
            // Try to load from file directly
            if (saveLoadManager != null)
            {
                bool success = saveLoadManager.LoadStructure("SimpleTrussBridge");
                if (success)
                {
                    ShowFeedback("LOADED: Example Bridge", Color.cyan);
                }
                else
                {
                    ShowFeedback("Example bridge not found!", Color.red);
                    Debug.LogError("[SHORTCUTS] Example bridge file not found!");
                }
            }
        }
    }

    /// <summary>
    /// Show feedback message
    /// </summary>
    void ShowFeedback(string message, Color color)
    {
        if (feedbackText != null)
        {
            feedbackText.text = message;
            feedbackText.color = color;
            feedbackText.gameObject.SetActive(true);
            feedbackTimer = feedbackDuration;
        }

        // Also trigger haptic feedback
        if (color == Color.green || color == Color.cyan)
        {
            HapticFeedback.Trigger(HapticFeedback.HapticType.Success);
        }
        else if (color == Color.red)
        {
            HapticFeedback.Trigger(HapticFeedback.HapticType.Error);
        }
        else
        {
            HapticFeedback.Trigger(HapticFeedback.HapticType.Medium);
        }
    }

    /// <summary>
    /// Show help text
    /// </summary>
    void OnGUI()
    {
        if (Application.isEditor)
        {
            GUILayout.BeginArea(new Rect(10, Screen.height - 150, 300, 150));
            GUILayout.Label("=== SAVE/LOAD SHORTCUTS ===");
            GUILayout.Label("S key: Quick Save");
            GUILayout.Label("L key: Load Last Save");
            GUILayout.Label("");
            GUILayout.Label("VR: Left Grip + A = Save");
            GUILayout.Label("VR: Left Grip + B = Load");
            GUILayout.Label("VR: Left Grip + X = Example Bridge");
            GUILayout.EndArea();
        }
    }
}
