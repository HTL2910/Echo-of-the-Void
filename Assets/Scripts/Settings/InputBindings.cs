using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace EchoOfTheVoid.Settings
{
    public enum PlayerAction
    {
        MoveLeft, MoveRight, Jump, Attack, Dash, Shift, Resonance, Interact, Anchor, Gravity
    }

    /// <summary>
    /// Which keys / gamepad buttons trigger each action (spec 2.3). Defaults match the spec; players can rebind and the
    /// result is stored in <see cref="SettingsData"/>. An input is never bound to two actions at once: assigning one
    /// that is taken swaps it away from the other action.
    /// </summary>
    public class InputBindings
    {
        public const int SlotsPerAction = 2;

        private readonly Dictionary<PlayerAction, List<Key>> _keys = new Dictionary<PlayerAction, List<Key>>();
        private readonly Dictionary<PlayerAction, GamepadButton?> _pad = new Dictionary<PlayerAction, GamepadButton?>();

        /// <summary>The bindings the game is using right now.</summary>
        public static InputBindings Current { get; private set; } = FromSettings(SettingsService.Current);

        public InputBindings()
        {
            ResetToDefaults();
        }

        public static void RefreshFromSettings() => Current = FromSettings(SettingsService.Current);

        public static InputBindings FromSettings(SettingsData data)
        {
            var b = new InputBindings();
            foreach (var entry in data.keyboardBindings)
            {
                if (!Enum.TryParse(entry.action, out PlayerAction action)) continue;
                var list = new List<Key>();
                foreach (var name in entry.inputs)
                    if (Enum.TryParse(name, out Key key) && key != Key.None && !list.Contains(key)) list.Add(key);
                if (list.Count > 0) b._keys[action] = list;
            }
            foreach (var entry in data.gamepadBindings)
            {
                if (!Enum.TryParse(entry.action, out PlayerAction action) || entry.inputs.Count == 0) continue;
                if (Enum.TryParse(entry.inputs[0], out GamepadButton button)) b._pad[action] = button;
            }
            return b;
        }

        public void WriteTo(SettingsData data)
        {
            data.keyboardBindings = new List<BindingEntry>();
            foreach (var pair in _keys)
            {
                var entry = new BindingEntry { action = pair.Key.ToString() };
                foreach (var key in pair.Value) entry.inputs.Add(key.ToString());
                data.keyboardBindings.Add(entry);
            }
            data.gamepadBindings = new List<BindingEntry>();
            foreach (var pair in _pad)
            {
                if (!pair.Value.HasValue) continue;
                data.gamepadBindings.Add(new BindingEntry { action = pair.Key.ToString(), inputs = new List<string> { pair.Value.Value.ToString() } });
            }
        }

        public IReadOnlyList<Key> GetKeys(PlayerAction action) => _keys[action];

        public GamepadButton? GetPadButton(PlayerAction action) => _pad.TryGetValue(action, out var b) ? b : null;

        /// <summary>
        /// Assign <paramref name="key"/> to slot <paramref name="slot"/> of <paramref name="action"/>. A key belongs to one
        /// action only: if another action owns it, that action loses it (and, if that would leave it with nothing,
        /// receives the key this slot used to hold, i.e. a swap).
        /// </summary>
        public void SetKey(PlayerAction action, int slot, Key key)
        {
            if (key == Key.None || slot < 0 || slot >= SlotsPerAction) return;

            var mine = _keys[action];
            int existing = mine.IndexOf(key);
            if (existing == slot) return;

            Key? replaced = slot < mine.Count ? mine[slot] : (Key?)null;

            if (existing >= 0)
            {
                // Already on another slot of this action: swap the two slots
                if (replaced.HasValue) mine[existing] = replaced.Value; else mine.RemoveAt(existing);
            }
            else
            {
                foreach (var pair in _keys)
                {
                    if (pair.Key == action) continue;
                    if (pair.Value.Remove(key) && pair.Value.Count == 0 && replaced.HasValue) pair.Value.Add(replaced.Value);
                }
            }

            if (slot < mine.Count) mine[slot] = key; else mine.Add(key);
        }

        /// <summary>Gamepad buttons are one per action; taking a used button swaps it with this action's old one.</summary>
        public void SetPadButton(PlayerAction action, GamepadButton button)
        {
            if (action == PlayerAction.MoveLeft || action == PlayerAction.MoveRight) return; // the stick is fixed

            GamepadButton? previous = GetPadButton(action);
            foreach (var other in new List<PlayerAction>(_pad.Keys))
            {
                if (other != action && _pad[other] == button) _pad[other] = previous;
            }
            _pad[action] = button;
        }

        public void ResetToDefaults()
        {
            _keys.Clear();
            _keys[PlayerAction.MoveLeft] = new List<Key> { Key.A, Key.LeftArrow };
            _keys[PlayerAction.MoveRight] = new List<Key> { Key.D, Key.RightArrow };
            _keys[PlayerAction.Jump] = new List<Key> { Key.Space, Key.W };
            _keys[PlayerAction.Attack] = new List<Key> { Key.J, Key.Z };
            _keys[PlayerAction.Dash] = new List<Key> { Key.K, Key.LeftCtrl };
            _keys[PlayerAction.Shift] = new List<Key> { Key.LeftShift, Key.RightShift };
            _keys[PlayerAction.Resonance] = new List<Key> { Key.U, Key.L };
            _keys[PlayerAction.Interact] = new List<Key> { Key.E };
            _keys[PlayerAction.Anchor] = new List<Key> { Key.F };
            _keys[PlayerAction.Gravity] = new List<Key> { Key.Q };

            _pad.Clear();
            _pad[PlayerAction.Jump] = GamepadButton.South;
            _pad[PlayerAction.Attack] = GamepadButton.West;
            _pad[PlayerAction.Dash] = GamepadButton.East;
            _pad[PlayerAction.Shift] = GamepadButton.RightShoulder;
            _pad[PlayerAction.Resonance] = GamepadButton.RightTrigger;
            _pad[PlayerAction.Interact] = GamepadButton.North;
            _pad[PlayerAction.Anchor] = GamepadButton.LeftShoulder;
            _pad[PlayerAction.Gravity] = GamepadButton.LeftTrigger;
        }
    }
}
