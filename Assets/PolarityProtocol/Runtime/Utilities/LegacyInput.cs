using UnityEngine;

namespace PolarityProtocol.Utilities
{
    public static class LegacyInput
    {
        public static Vector2 Move => new(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        public static Vector2 Look
        {
            get
            {
                float controllerX = SafeAxis("Right Stick Horizontal");
                float controllerY = SafeAxis("Right Stick Vertical");
                return new Vector2(
                    Input.GetAxisRaw("Mouse X") + controllerX * 2.2f,
                    Input.GetAxisRaw("Mouse Y") + controllerY * 2.2f);
            }
        }

        public static bool AttackPressed =>
            Input.GetMouseButtonDown(0) ||
            Input.GetKeyDown(KeyCode.JoystickButton5) ||
            Input.GetKeyDown(KeyCode.JoystickButton0);

        public static bool PlaceAnchorPressed =>
            Input.GetMouseButtonDown(1) ||
            Input.GetKeyDown(KeyCode.JoystickButton4);

        public static bool TogglePolarityPressed =>
            Input.GetKeyDown(KeyCode.Q) ||
            Input.GetKeyDown(KeyCode.JoystickButton2);

        public static bool RecallPressed =>
            Input.GetKeyDown(KeyCode.R) ||
            Input.GetKeyDown(KeyCode.JoystickButton3);

        public static bool DashPressed =>
            Input.GetKeyDown(KeyCode.Space) ||
            Input.GetKeyDown(KeyCode.JoystickButton1);

        public static bool SprintHeld =>
            Input.GetKey(KeyCode.LeftShift) ||
            Input.GetKey(KeyCode.RightShift) ||
            Input.GetKey(KeyCode.JoystickButton8);

        public static bool PausePressed =>
            Input.GetKeyDown(KeyCode.Escape) ||
            Input.GetKeyDown(KeyCode.JoystickButton7);

        public static bool DebugPressed => Input.GetKeyDown(KeyCode.F3);

        private static float SafeAxis(string axis)
        {
            try
            {
                return Input.GetAxisRaw(axis);
            }
            catch
            {
                return 0f;
            }
        }
    }
}

