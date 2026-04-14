using UnityEngine;

/// <summary>
/// Optimize mode partial: trigger handler for compliance history rollback
/// and exaggeration-factor display via right thumbstick (handled in HandleToolSwitch).
/// </summary>
public partial class OVRGraphController
{
    // ------------------------------------------------------------------ //
    // Optimize mode trigger dispatch
    // ------------------------------------------------------------------ //

    void HandleOptimizeTrigger()
    {
        bool triggerPressed = OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch);
        bool bPressed       = OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch);

        if (currentOptimizeTool == OptimizeTool.Rollback)
        {
            // B button: jump cursor to the latest snapshot
            if (bPressed && optimizeSession != null)
            {
                optimizeSession.ResetCursorToLatest();
                optimizeVisualizer?.ShowRollbackPreview(optimizeSession.GetCursorSnapshot());
                modeDisplayUI?.UpdateGraph(optimizeSession.History, optimizeSession.CursorIndex, optimizeSession.BestIndex, true);
                UpdateModeText();
            }

            // Trigger: restore structure to the snapshot at the current cursor position
            if (triggerPressed && !triggerHeldLastFrame && optimizeSession != null)
            {
                optimizeVisualizer?.ClearRollbackPreview();
                optimizeSession.RollbackToCursor();
                modeDisplayUI?.UpdateGraph(optimizeSession.History, optimizeSession.CursorIndex, optimizeSession.BestIndex, true);
                HapticFeedback.Trigger(HapticFeedback.HapticType.Medium);
            }
        }
        else // OptimizeTool.Move
        {
            // B button: no-op in Move tool (grip drag is the main interaction)
            // Nothing triggered by trigger in Move mode either
        }

        triggerHeldLastFrame = triggerPressed;
    }
}
