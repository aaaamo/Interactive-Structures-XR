using UnityEngine;

/// <summary>
/// Utility class for managing haptic feedback on Oculus controllers
/// </summary>
public static class HapticFeedback
{
    public enum HapticType
    {
        Light,      // Subtle feedback for hover/selection
        Medium,     // Standard feedback for actions
        Strong,     // Feedback for important actions or errors
        Success,    // Confirmation feedback
        Error       // Error or invalid action feedback
    }

    /// <summary>
    /// Trigger haptic feedback on the right controller
    /// </summary>
    public static void Trigger(HapticType type, OVRInput.Controller controller = OVRInput.Controller.RTouch)
    {
        float frequency, amplitude;

        switch (type)
        {
            case HapticType.Light:
                frequency = 0.3f;
                amplitude = 0.2f;
                break;
            case HapticType.Medium:
                frequency = 0.5f;
                amplitude = 0.5f;
                break;
            case HapticType.Strong:
                frequency = 1.0f;
                amplitude = 0.8f;
                break;
            case HapticType.Success:
                frequency = 0.7f;
                amplitude = 0.6f;
                break;
            case HapticType.Error:
                frequency = 0.2f;
                amplitude = 1.0f;
                break;
            default:
                frequency = 0.5f;
                amplitude = 0.5f;
                break;
        }

        OVRInput.SetControllerVibration(frequency, amplitude, controller);
        // Auto-stop after a short duration
        CoroutineRunner.Instance.StopVibrationAfter(0.1f, controller);
    }

    /// <summary>
    /// Trigger a custom haptic pulse
    /// </summary>
    public static void CustomPulse(float frequency, float amplitude, float duration, OVRInput.Controller controller = OVRInput.Controller.RTouch)
    {
        OVRInput.SetControllerVibration(frequency, amplitude, controller);
        CoroutineRunner.Instance.StopVibrationAfter(duration, controller);
    }

    /// <summary>
    /// Stop all vibration on a controller
    /// </summary>
    public static void Stop(OVRInput.Controller controller = OVRInput.Controller.RTouch)
    {
        OVRInput.SetControllerVibration(0, 0, controller);
    }
}

/// <summary>
/// Simple MonoBehaviour singleton for running coroutines from static context
/// </summary>
public class CoroutineRunner : MonoBehaviour
{
    private static CoroutineRunner _instance;
    public static CoroutineRunner Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("CoroutineRunner");
                _instance = go.AddComponent<CoroutineRunner>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    public void StopVibrationAfter(float delay, OVRInput.Controller controller)
    {
        StartCoroutine(StopVibrationCoroutine(delay, controller));
    }

    private System.Collections.IEnumerator StopVibrationCoroutine(float delay, OVRInput.Controller controller)
    {
        yield return new WaitForSeconds(delay);
        OVRInput.SetControllerVibration(0, 0, controller);
    }
}
