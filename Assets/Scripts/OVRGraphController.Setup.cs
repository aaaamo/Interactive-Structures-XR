using System.Collections;
using UnityEngine;

/// <summary>
/// Setup mode partial: trigger handler for all four Setup tools
/// and per-frame visual feedback.
/// </summary>
public partial class OVRGraphController
{
    // ------------------------------------------------------------------ //
    // Setup mode trigger dispatch
    // ------------------------------------------------------------------ //

    void HandleSetupTrigger(bool isConnected)
    {
        bool triggerPressed = OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch);
        if (!triggerPressed || triggerHeldLastFrame)
        {
            triggerHeldLastFrame = triggerPressed;
            return;
        }
        triggerHeldLastFrame = triggerPressed;

        switch (currentSetupTool)
        {
            case SetupTool.World:
                worldCoordinateManager?.SetAnchor();
                HapticFeedback.Trigger(HapticFeedback.HapticType.Medium);
                break;

            case SetupTool.Grid:
                surfaceFinder?.SetAnchor();
                HapticFeedback.Trigger(HapticFeedback.HapticType.Medium);
                break;

            case SetupTool.Network:
                // NetworkConnect shows its own UI whenever SetupTool.Network is active.
                // No trigger action needed — the UI handles input internally.
                break;

            case SetupTool.Import:
                if (!isConnected)
                {
                    modeDisplayUI?.ShowMessage("Connect to network first", Color.yellow);
                    return;
                }
                SaveLoadManager saveLoad = FindObjectOfType<SaveLoadManager>();
                if (saveLoad != null)
                {
                    saveLoad.LoadStructure("SimpleTrussBridge", (success) =>
                    {
                        string msg = success ? "Loaded: SimpleTrussBridge" : "No structure file found";
                        modeDisplayUI?.ShowMessage(msg, success ? Color.green : Color.yellow);
                        // Delay one frame so all spawned NetworkObjects finish initialising
                        if (success) { StartCoroutine(OnStructureChangedNextFrame()); }
                    });
                }
                else
                {
                    modeDisplayUI?.ShowMessage("Setup SaveLoadManager first", Color.yellow);
                }
                break;
        }
    }

    IEnumerator OnStructureChangedNextFrame()
    {
        yield return null; // wait one frame
        optimizeVisualizer?.OnStructureChanged();
    }

    // ------------------------------------------------------------------ //
    // Setup visual feedback (per-frame)
    // ------------------------------------------------------------------ //

    void UpdateSetupVisualFeedback()
    {
        if (hoveredObject != null)
        {
            VisualFeedbackManager.Instance?.ClearHover();
            hoveredObject = null;
        }
        // Individual setup tools (World, Grid, Network, Import) manage their
        // own preview visuals via UpdatePreviewVisibility().
    }
}
