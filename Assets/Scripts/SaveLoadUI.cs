using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// UI for save/load operations
/// </summary>
public class SaveLoadUI : MonoBehaviour
{
    [Header("References")]
    public SaveLoadManager saveLoadManager;
    public GameObject saveLoadPanel;
    public Transform panelTransform;

    [Header("UI Elements")]
    public TextMeshPro titleText;
    public TextMeshPro instructionText;
    public TextMeshPro fileListText;
    public TMP_InputField fileNameInput; // For typing new name (desktop)

    [Header("Settings")]
    public Vector3 panelOffset = new Vector3(0f, 0.1f, 1.2f);
    public int maxFilesDisplay = 8;

    private bool isPanelVisible = false;
    private List<string> savedFiles = new List<string>();
    private int selectedFileIndex = 0;
    private bool isInSaveMode = true; // true = save, false = load

    private string currentInputFileName = "MyStructure";

    void Start()
    {
        if (saveLoadPanel != null)
            saveLoadPanel.SetActive(false);
    }

    void Update()
    {
        if (isPanelVisible)
        {
            HandleInput();
            PositionPanel();
        }

        // Toggle with Y button (left controller)
        if (OVRInput.GetDown(OVRInput.Button.Four, OVRInput.Controller.LTouch))
        {
            TogglePanel();
        }
    }

    /// <summary>
    /// Toggle save/load panel visibility
    /// </summary>
    public void TogglePanel()
    {
        isPanelVisible = !isPanelVisible;

        if (isPanelVisible)
        {
            ShowPanel(true); // Default to save mode
        }
        else
        {
            HidePanel();
        }

        HapticFeedback.Trigger(HapticFeedback.HapticType.Light);
    }

    /// <summary>
    /// Show the save/load panel
    /// </summary>
    public void ShowPanel(bool saveMode)
    {
        isInSaveMode = saveMode;
        isPanelVisible = true;

        if (saveLoadPanel != null)
            saveLoadPanel.SetActive(true);

        RefreshFileList();
        UpdateUI();
    }

    /// <summary>
    /// Hide the panel
    /// </summary>
    public void HidePanel()
    {
        isPanelVisible = false;

        if (saveLoadPanel != null)
            saveLoadPanel.SetActive(false);
    }

    /// <summary>
    /// Handle VR input for navigation
    /// </summary>
    void HandleInput()
    {
        Vector2 leftThumb = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.LTouch);

        // Navigate file list
        if (leftThumb.y > 0.7f && Time.frameCount % 10 == 0)
        {
            selectedFileIndex = Mathf.Max(0, selectedFileIndex - 1);
            UpdateUI();
            HapticFeedback.Trigger(HapticFeedback.HapticType.Light);
        }
        else if (leftThumb.y < -0.7f && Time.frameCount % 10 == 0)
        {
            selectedFileIndex = Mathf.Min(savedFiles.Count - 1, selectedFileIndex + 1);
            UpdateUI();
            HapticFeedback.Trigger(HapticFeedback.HapticType.Light);
        }

        // Toggle between save/load mode
        if (OVRInput.GetDown(OVRInput.Button.Three, OVRInput.Controller.LTouch)) // X button
        {
            isInSaveMode = !isInSaveMode;
            UpdateUI();
            HapticFeedback.Trigger(HapticFeedback.HapticType.Medium);
        }

        // Confirm action (trigger)
        if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch))
        {
            ConfirmAction();
        }

        // Cancel (grip)
        if (OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.LTouch))
        {
            HidePanel();
        }
    }

    /// <summary>
    /// Confirm save or load action
    /// </summary>
    void ConfirmAction()
    {
        if (isInSaveMode)
        {
            // Save
            bool success = saveLoadManager.SaveStructure(currentInputFileName);
            if (success)
            {
                RefreshFileList();
                UpdateUI();
            }
        }
        else
        {
            // Load
            if (savedFiles.Count > 0 && selectedFileIndex >= 0 && selectedFileIndex < savedFiles.Count)
            {
                string fileName = savedFiles[selectedFileIndex];
                bool success = saveLoadManager.LoadStructure(fileName);
                if (success)
                {
                    HidePanel();
                }
            }
        }
    }

    /// <summary>
    /// Refresh the list of saved files
    /// </summary>
    void RefreshFileList()
    {
        savedFiles = saveLoadManager.GetSavedStructures();
        selectedFileIndex = Mathf.Clamp(selectedFileIndex, 0, savedFiles.Count - 1);
    }

    /// <summary>
    /// Update UI text
    /// </summary>
    void UpdateUI()
    {
        if (titleText != null)
        {
            titleText.text = isInSaveMode ? "<color=#88FF88>SAVE STRUCTURE</color>" : "<color=#88DDFF>LOAD STRUCTURE</color>";
        }

        if (instructionText != null)
        {
            if (isInSaveMode)
            {
                instructionText.text = @"<b>CONTROLS:</b>
<color=#FFFF88>X Button:</color> Switch to Load mode
<color=#FFFF88>Trigger:</color> Save current structure
<color=#FFFF88>Grip:</color> Cancel

<b>File name:</b> " + currentInputFileName + ".json";
            }
            else
            {
                instructionText.text = @"<b>CONTROLS:</b>
<color=#FFFF88>Thumbstick Up/Down:</color> Select file
<color=#FFFF88>X Button:</color> Switch to Save mode
<color=#FFFF88>Trigger:</color> Load selected
<color=#FFFF88>Grip:</color> Cancel";
            }
        }

        if (fileListText != null)
        {
            if (savedFiles.Count == 0)
            {
                fileListText.text = "<color=#888888>No saved structures found</color>";
            }
            else
            {
                string fileList = "<b>SAVED STRUCTURES:</b>\n\n";
                int startIndex = Mathf.Max(0, selectedFileIndex - maxFilesDisplay / 2);
                int endIndex = Mathf.Min(savedFiles.Count, startIndex + maxFilesDisplay);

                for (int i = startIndex; i < endIndex; i++)
                {
                    if (i == selectedFileIndex && !isInSaveMode)
                    {
                        fileList += $"<color=#88FF88>► {savedFiles[i]}</color>\n";
                    }
                    else
                    {
                        fileList += $"  {savedFiles[i]}\n";
                    }
                }

                fileListText.text = fileList;
            }
        }
    }

    /// <summary>
    /// Position panel in front of user
    /// </summary>
    void PositionPanel()
    {
        if (panelTransform == null || Camera.main == null) return;

        Vector3 cameraPos = Camera.main.transform.position;
        Vector3 cameraForward = Camera.main.transform.forward;
        cameraForward.y = 0;
        cameraForward.Normalize();

        Vector3 targetPos = cameraPos
            + cameraForward * panelOffset.z
            + Vector3.up * panelOffset.y
            + Camera.main.transform.right * panelOffset.x;

        panelTransform.position = Vector3.Lerp(panelTransform.position, targetPos, Time.deltaTime * 5f);

        Vector3 lookDir = cameraPos - panelTransform.position;
        lookDir.y = 0;
        if (lookDir.magnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(-lookDir);
            panelTransform.rotation = Quaternion.Slerp(panelTransform.rotation, targetRotation, Time.deltaTime * 5f);
        }
    }

    /// <summary>
    /// Set file name for saving (called from input field or code)
    /// </summary>
    public void SetFileName(string fileName)
    {
        currentInputFileName = fileName;
        UpdateUI();
    }

    /// <summary>
    /// Quick save with default name
    /// </summary>
    public void QuickSave()
    {
        string fileName = "QuickSave_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        saveLoadManager.SaveStructure(fileName);
        RefreshFileList();
    }
}
