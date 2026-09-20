using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using EchoOfTheVoid.Settings;

namespace EchoOfTheVoid.Player
{
    /// <summary>
    /// Keyboard + gamepad input using the player's bindings (<see cref="InputBindings"/>, spec 2.3).
    /// Reality Shift is Left Shift / RB; E is Interact (D10).
    /// </summary>
    public class DevicePlayerInput : IPlayerInput
    {
        public PlayerInputFrame Poll()
        {
            var f = new PlayerInputFrame();

#if ENABLE_INPUT_SYSTEM
            var bindings = InputBindings.Current;

            var kb = Keyboard.current;
            if (kb != null)
            {
                if (Held(kb, bindings.GetKeys(PlayerAction.MoveLeft))) f.Move -= 1f;
                if (Held(kb, bindings.GetKeys(PlayerAction.MoveRight))) f.Move += 1f;

                var jump = bindings.GetKeys(PlayerAction.Jump);
                f.JumpPressed |= Pressed(kb, jump);
                f.JumpReleased |= Released(kb, jump);
                f.ShiftPressed |= Pressed(kb, bindings.GetKeys(PlayerAction.Shift));
                f.AttackPressed |= Pressed(kb, bindings.GetKeys(PlayerAction.Attack));
                f.DashPressed |= Pressed(kb, bindings.GetKeys(PlayerAction.Dash));
                f.ResonancePressed |= Pressed(kb, bindings.GetKeys(PlayerAction.Resonance));
                f.InteractPressed |= Pressed(kb, bindings.GetKeys(PlayerAction.Interact));
            }

            var pad = Gamepad.current;
            if (pad != null)
            {
                float stickX = pad.leftStick.x.ReadValue();
                if (Mathf.Abs(stickX) > 0.15f) f.Move = stickX;

                f.JumpPressed |= PadPressed(pad, bindings, PlayerAction.Jump);
                f.JumpReleased |= PadReleased(pad, bindings, PlayerAction.Jump);
                f.ShiftPressed |= PadPressed(pad, bindings, PlayerAction.Shift);
                f.AttackPressed |= PadPressed(pad, bindings, PlayerAction.Attack);
                f.DashPressed |= PadPressed(pad, bindings, PlayerAction.Dash);
                f.ResonancePressed |= PadPressed(pad, bindings, PlayerAction.Resonance);
                f.InteractPressed |= PadPressed(pad, bindings, PlayerAction.Interact);
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

#if ENABLE_INPUT_SYSTEM
        private static bool Held(Keyboard kb, IReadOnlyList<Key> keys)
        {
            for (int i = 0; i < keys.Count; i++) if (kb[keys[i]].isPressed) return true;
            return false;
        }

        private static bool Pressed(Keyboard kb, IReadOnlyList<Key> keys)
        {
            for (int i = 0; i < keys.Count; i++) if (kb[keys[i]].wasPressedThisFrame) return true;
            return false;
        }

        private static bool Released(Keyboard kb, IReadOnlyList<Key> keys)
        {
            for (int i = 0; i < keys.Count; i++) if (kb[keys[i]].wasReleasedThisFrame) return true;
            return false;
        }

        private static bool PadPressed(Gamepad pad, InputBindings bindings, PlayerAction action)
        {
            var button = bindings.GetPadButton(action);
            return button.HasValue && pad[button.Value].wasPressedThisFrame;
        }

        private static bool PadReleased(Gamepad pad, InputBindings bindings, PlayerAction action)
        {
            var button = bindings.GetPadButton(action);
            return button.HasValue && pad[button.Value].wasReleasedThisFrame;
        }
#endif
    }
}
