using System;

namespace UnityEngine.InputSystem.Experimental.Device
{
    // Auto-generated from native header of device interface
    public struct Gamepad
    {
        public static InputBindingSource<Vector2> leftStick = new(Usages.Gamepad.leftStick);
        public static InputBindingSource<Vector2> rightStick = new(Usages.Gamepad.rightStick);
        public static InputBindingSource<Button> buttonSouth => new(Usages.Gamepad.buttonSouth);
        public static InputBindingSource<bool> buttonEast = new(Usages.Gamepad.buttonEast);
        public static OutputBindingTarget<float> rumbleHaptic = new(Usages.Gamepad.rumbleHaptic); // TODO Move to HapticDevice
    }
}