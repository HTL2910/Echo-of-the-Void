using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using EchoOfTheVoid.Settings;

namespace EchoOfTheVoid.UI
{
    /// <summary>
    /// Settings screen: audio, accessibility, assist and key/button rebinding (spec 9.5, 9.6).
    /// Open with <see cref="Open"/>; every change is applied and saved immediately.
    /// </summary>
    public class SettingsMenu : MonoBehaviour
    {
        private static readonly PlayerAction[] Rows =
        {
            PlayerAction.MoveLeft, PlayerAction.MoveRight, PlayerAction.Jump, PlayerAction.Attack,
            PlayerAction.Dash, PlayerAction.Shift, PlayerAction.Resonance, PlayerAction.Interact,
            PlayerAction.Anchor, PlayerAction.Gravity
        };

        private static readonly float[] DamageSteps = { 1f, 0.75f, 0.5f };

        private Font _font;
        private Action _onClose;
        private RectTransform _panel;
        private readonly Dictionary<(PlayerAction, int, bool), Button> _bindingButtons = new Dictionary<(PlayerAction, int, bool), Button>();

        public RebindSession Session { get; } = new RebindSession();
        public bool IsListening => Session.IsListening;

        // Exposed for tests / the pause menu
        public Slider MasterSlider { get; private set; }
        public Slider MusicSlider { get; private set; }
        public Slider ShakeSlider { get; private set; }
        public Toggle ReduceFlashingToggle { get; private set; }
        public Toggle DisableHitstopToggle { get; private set; }
        public Button DamageButton { get; private set; }
        public Button ControlsButton { get; private set; }
        public Button BackButton { get; private set; }
        public Button ResetBindingsButton { get; private set; }

        public static SettingsMenu Open(Font font, Action onClose)
        {
            UiKit.EnsureEventSystem();
            var canvas = UiKit.CreateCanvas("SettingsCanvas", 500);
            var menu = canvas.gameObject.AddComponent<SettingsMenu>();
            menu._font = font;
            menu._onClose = onClose;
            menu.ShowMain();
            return menu;
        }

        public void Close()
        {
            Session.Cancel();
            _onClose?.Invoke();
            Destroy(gameObject);
        }

        // ------------------------------------------------------------------ main page
        public void ShowMain()
        {
            Session.Cancel();
            Rebuild(new Vector2(1100f, 960f));
            var s = SettingsService.Current;

            UiKit.CreateHeading(_panel, "SETTINGS", _font);
            MasterSlider = UiKit.CreateSlider(_panel, "Master volume", s.masterVolume, _font, v => { s.masterVolume = v; SettingsService.Apply(); });
            MusicSlider = UiKit.CreateSlider(_panel, "Music volume", s.musicVolume, _font, v => { s.musicVolume = v; SettingsService.Apply(); });
            UiKit.CreateSlider(_panel, "Effects volume", s.sfxVolume, _font, v => { s.sfxVolume = v; SettingsService.Apply(); });
            ShakeSlider = UiKit.CreateSlider(_panel, "Screen shake", s.screenShake, _font, v => { s.screenShake = v; SettingsService.Apply(); });

            ReduceFlashingToggle = UiKit.CreateToggle(_panel, "Reduce flashing", s.reduceFlashing, _font, v => { s.reduceFlashing = v; SettingsService.Apply(); });
            UiKit.CreateToggle(_panel, "Colorblind palette", s.colorblindPalette, _font, v => { s.colorblindPalette = v; SettingsService.Apply(); });
            DisableHitstopToggle = UiKit.CreateToggle(_panel, "Disable hit-stop", s.disableHitstop, _font, v => { s.disableHitstop = v; SettingsService.Apply(); });
            UiKit.CreateToggle(_panel, "Assist: extra coyote time", s.extendedCoyoteTime, _font, v => { s.extendedCoyoteTime = v; SettingsService.Apply(); });
            UiKit.CreateToggle(_panel, "Assist: longer invincibility", s.extendedIFrames, _font, v => { s.extendedIFrames = v; SettingsService.Apply(); });

            DamageButton = UiKit.CreateButton(_panel, DamageLabel(s.damageTakenMultiplier), _font, () =>
            {
                s.damageTakenMultiplier = NextDamageStep(s.damageTakenMultiplier);
                SettingsService.Apply();
                UiKit.SetButtonText(DamageButton, DamageLabel(s.damageTakenMultiplier));
            }, 56f);

            ControlsButton = UiKit.CreateButton(_panel, "Controls", _font, ShowControls, 56f);
            BackButton = UiKit.CreateButton(_panel, "Back", _font, Close, 56f);
        }

        public static string DamageLabel(float multiplier) => $"Assist: damage taken {Mathf.RoundToInt(multiplier * 100f)}%";

        public static float NextDamageStep(float current)
        {
            for (int i = 0; i < DamageSteps.Length; i++)
                if (Mathf.Approximately(DamageSteps[i], current)) return DamageSteps[(i + 1) % DamageSteps.Length];
            return DamageSteps[0];
        }

        // ------------------------------------------------------------------ controls page
        public void ShowControls()
        {
            Session.Cancel();
            Rebuild(new Vector2(1100f, 1010f));
            _bindingButtons.Clear();

            UiKit.CreateTitle(_panel, "CONTROLS", _font);
            UiKit.CreateBody(_panel, "Click button, then press the new key", _font, UiKit.Muted);
            var header = UiKit.CreateRow(_panel, 36f);
            UiKit.CreateLabel(header, "Action", 22, _font, UiKit.Muted, TextAnchor.MiddleLeft).GetComponent<LayoutElement>().preferredWidth = 260f;
            UiKit.CreateLabel(header, "Key 1", 22, _font, UiKit.Muted).GetComponent<LayoutElement>().preferredWidth = 200f;
            UiKit.CreateLabel(header, "Key 2", 22, _font, UiKit.Muted).GetComponent<LayoutElement>().preferredWidth = 200f;
            UiKit.CreateLabel(header, "Gamepad", 22, _font, UiKit.Muted).GetComponent<LayoutElement>().preferredWidth = 200f;

            foreach (var action in Rows)
            {
                var row = UiKit.CreateRow(_panel, 50f);
                UiKit.CreateLabel(row, action.ToString(), 24, _font, UiKit.TextColor, TextAnchor.MiddleLeft).GetComponent<LayoutElement>().preferredWidth = 260f;

                for (int slot = 0; slot < InputBindings.SlotsPerAction; slot++)
                {
                    int captured = slot;
                    var b = UiKit.CreateButton(row, "", _font, () => BeginKey(action, captured), 44f);
                    b.GetComponent<LayoutElement>().preferredWidth = 200f;
                    _bindingButtons[(action, slot, false)] = b;
                }

                bool stick = action == PlayerAction.MoveLeft || action == PlayerAction.MoveRight;
                var pad = UiKit.CreateButton(row, "", _font, stick ? (Action)null : () => BeginPad(action), 44f);
                pad.GetComponent<LayoutElement>().preferredWidth = 200f;
                pad.interactable = !stick;
                _bindingButtons[(action, 0, true)] = pad;
            }

            ResetBindingsButton = UiKit.CreateButton(_panel, "Reset to defaults", _font, () =>
            {
                InputBindings.Current.ResetToDefaults();
                InputBindings.Current.WriteTo(SettingsService.Current);
                SettingsService.Apply();
                RefreshBindingLabels();
            }, 52f);
            UiKit.CreateButton(_panel, "Back", _font, ShowMain, 52f);

            Session.Finished -= RefreshBindingLabels;
            Session.Finished += RefreshBindingLabels;
            RefreshBindingLabels();
        }

        public void BeginKey(PlayerAction action, int slot)
        {
            Session.BeginKey(action, slot);
            RefreshBindingLabels();
        }

        public void BeginPad(PlayerAction action)
        {
            Session.BeginPad(action);
            RefreshBindingLabels();
        }

        public void RefreshBindingLabels()
        {
            var bindings = InputBindings.Current;
            foreach (var pair in _bindingButtons)
            {
                var (action, slot, isPad) = pair.Key;
                if (pair.Value == null) continue;

                bool waiting = Session.IsListening && Session.Action == action && Session.ForGamepad == isPad && (isPad || Session.Slot == slot);
                string text;
                if (waiting) text = "Press...";
                else if (isPad)
                {
                    var button = bindings.GetPadButton(action);
                    text = button.HasValue ? button.Value.ToString() : "Stick";
                }
                else
                {
                    var keys = bindings.GetKeys(action);
                    text = slot < keys.Count ? keys[slot].ToString() : "-";
                }
                UiKit.SetButtonText(pair.Value, text);
            }
        }

        private void Update()
        {
            if (!Session.IsListening) return;

            if (!Session.ForGamepad)
            {
                var kb = Keyboard.current;
                if (kb == null) return;
                foreach (var control in kb.allKeys)
                {
                    if (control.wasPressedThisFrame) { Session.CommitKey(control.keyCode); return; }
                }
            }
            else
            {
                var pad = Gamepad.current;
                if (pad == null) return;
                foreach (var button in ListenableButtons)
                {
                    if (pad[button].wasPressedThisFrame) { Session.CommitPad(button); return; }
                }
            }
        }

        private static readonly GamepadButton[] ListenableButtons =
        {
            GamepadButton.South, GamepadButton.East, GamepadButton.West, GamepadButton.North,
            GamepadButton.LeftShoulder, GamepadButton.RightShoulder, GamepadButton.LeftTrigger, GamepadButton.RightTrigger
        };

        // ------------------------------------------------------------------ plumbing
        private void Rebuild(Vector2 size)
        {
            if (_panel != null) Destroy(_panel.gameObject);
            _panel = UiKit.CreatePanel(transform, "SettingsPanel", UiKit.Panel, size);
            UiKit.MakeVertical(_panel, 10f, 26);
        }
    }
}
