using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace EchoOfTheVoid.Player
{
    /// <summary>Keyboard + gamepad bindings (spec 2.3). Reality Shift is Left Shift / RB; E is reserved for Interact (D10).</summary>
    public class DevicePlayerInput : IPlayerInput
    {
        public PlayerInputFrame Poll()
        {
            var f = new PlayerInputFrame();

#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) f.Move -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) f.Move += 1f;

                f.JumpPressed |= kb.spaceKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame;
                f.JumpReleased |= kb.spaceKey.wasReleasedThisFrame || kb.wKey.wasReleasedThisFrame;
                f.ShiftPressed |= kb.leftShiftKey.wasPressedThisFrame || kb.rightShiftKey.wasPressedThisFrame;
                f.AttackPressed |= kb.jKey.wasPressedThisFrame || kb.zKey.wasPressedThisFrame;
                f.DashPressed |= kb.kKey.wasPressedThisFrame || kb.leftCtrlKey.wasPressedThisFrame;
                f.ResonancePressed |= kb.uKey.wasPressedThisFrame || kb.lKey.wasPressedThisFrame;
                f.InteractPressed |= kb.eKey.wasPressedThisFrame;
            }

            var pad = Gamepad.current;
            if (pad != null)
            {
                float stickX = pad.leftStick.x.ReadValue();
                if (Mathf.Abs(stickX) > 0.15f) f.Move = stickX;

                f.JumpPressed |= pad.buttonSouth.wasPressedThisFrame;
                f.JumpReleased |= pad.buttonSouth.wasReleasedThisFrame;
                f.ShiftPressed |= pad.rightShoulder.wasPressedThisFrame;                                   // RB
                f.AttackPressed |= pad.buttonWest.wasPressedThisFrame;                                      // X
                f.DashPressed |= pad.buttonEast.wasPressedThisFrame;                                        // B
                f.ResonancePressed |= pad.rightTrigger.wasPressedThisFrame;                                 // RT (spec 2.3)
                f.InteractPressed |= pad.buttonNorth.wasPressedThisFrame;                                   // Y
            }
#else
            f.Move = Input.GetAxisRaw("Horizontal");
            f.JumpPressed = Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space);
            f.JumpReleased = Input.GetButtonUp("Jump") || Input.GetKeyUp(KeyCode.Space);
            f.ShiftPressed = Input.GetKeyDown(KeyCode.LeftShift);
            f.AttackPressed = Input.GetKeyDown(KeyCode.J) || Input.GetKeyDown(KeyCode.Z);
            f.DashPressed = Input.GetKeyDown(KeyCode.K) || Input.GetKeyDown(KeyCode.LeftControl);
            f.ResonancePressed = Input.GetKeyDown(KeyCode.U) || Input.GetKeyDown(KeyCode.L);
            f.InteractPressed = Input.GetKeyDown(KeyCode.E);
#endif
            f.Move = Mathf.Clamp(f.Move, -1f, 1f);
            return f;
        }
    }
}
