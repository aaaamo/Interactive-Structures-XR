using UnityEngine;

namespace InteractiveStructures.Controllers
{
    /// <summary>
    /// Handles VR controller input for mode switching and grid adjustments
    /// </summary>
    public class InputHandler : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float thumbstickThreshold = 0.7f;
        [SerializeField] private float modeSwitchCooldown = 0.3f;

        private float lastThumbTime = 0f;

        public bool IsTriggerPressed => OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch);
        public bool IsTriggerDown => OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch);
        public bool IsGripDown => OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch);

        /// <summary>
        /// Check for mode switch input
        /// </summary>
        /// <returns>1 for next mode, -1 for previous mode, 0 for no change</returns>
        public int GetModeSwitchInput()
        {
            if (Time.time - lastThumbTime < modeSwitchCooldown)
                return 0;

            Vector2 thumbstick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.RTouch);

            if (thumbstick.x > thumbstickThreshold)
            {
                lastThumbTime = Time.time;
                return 1; // Next mode
            }
            else if (thumbstick.x < -thumbstickThreshold)
            {
                lastThumbTime = Time.time;
                return -1; // Previous mode
            }

            return 0;
        }

        /// <summary>
        /// Get left thumbstick input for grid/analyze adjustments
        /// </summary>
        public Vector2 GetLeftThumbstick()
        {
            return OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.LTouch);
        }

        /// <summary>
        /// Check if left thumbstick button was pressed
        /// </summary>
        public bool GetLeftThumbstickPress()
        {
            return OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.LTouch);
        }

        /// <summary>
        /// Check thumbstick direction for grid axis cycling
        /// </summary>
        /// <returns>1 for right, -1 for left, 0 for no input</returns>
        public int GetHorizontalInput()
        {
            if (Time.time - lastThumbTime < modeSwitchCooldown)
                return 0;

            Vector2 thumbstick = GetLeftThumbstick();

            if (thumbstick.x > thumbstickThreshold)
            {
                lastThumbTime = Time.time;
                return 1;
            }
            else if (thumbstick.x < -thumbstickThreshold)
            {
                lastThumbTime = Time.time;
                return -1;
            }

            return 0;
        }

        /// <summary>
        /// Get vertical input value for grid adjustments
        /// </summary>
        public float GetVerticalInput()
        {
            Vector2 thumbstick = GetLeftThumbstick();
            if (Mathf.Abs(thumbstick.y) > thumbstickThreshold)
            {
                lastThumbTime = Time.time;
                return thumbstick.y;
            }
            return 0f;
        }
    }
}
