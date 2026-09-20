using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using EchoOfTheVoid.Core;

namespace EchoOfTheVoid.UI
{
    /// <summary>Pause menu (Esc / Start): Resume, Settings, Quit to Menu. Freezes time and blocks Kael's input while open.</summary>
    public class PauseController : MonoBehaviour
    {
        [SerializeField] private Font font;

        private Canvas _canvas;
        private SettingsMenu _settings;

        public bool IsOpen { get; private set; }
        public Button ResumeButton { get; private set; }
        public Button SettingsButton { get; private set; }
        public Button QuitToMenuButton { get; private set; }

        public void SetFont(Font value) => font = value;

        private void Update()
        {
            if (_settings != null && _settings.IsListening) return; // Esc cancels a rebind instead

            bool pressed = false;
            var kb = Keyboard.current;
            var pad = Gamepad.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) pressed = true;
            if (pad != null && pad.startButton.wasPressedThisFrame) pressed = true;

            if (pressed && _settings == null) Toggle();
        }

        public void Toggle()
        {
            if (IsOpen) Resume(); else Open();
        }

        public void Open()
        {
            if (IsOpen) return;
            IsOpen = true;
            GameFlow.IsPaused = true;
            Time.timeScale = 0f;

            UiKit.EnsureEventSystem();
            _canvas = UiKit.CreateCanvas("PauseCanvas", 400);
            var panel = UiKit.CreatePanel(_canvas.transform, "Pause", UiKit.Panel, new Vector2(620f, 520f));
            UiKit.MakeVertical(panel, 18f, 34);

            UiKit.CreateLabel(panel, "PAUSED", 52, font, UiKit.Accent, height: 80f);
            ResumeButton = UiKit.CreateButton(panel, "Resume", font, Resume);
            SettingsButton = UiKit.CreateButton(panel, "Settings", font, OpenSettings);
            QuitToMenuButton = UiKit.CreateButton(panel, "Quit to Menu", font, () => { Close(); GameFlow.QuitToMenu(); });
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(ResumeButton.gameObject);
        }

        public void Resume()
        {
            if (!IsOpen) return;
            Close();
        }

        private void OpenSettings()
        {
            _canvas.gameObject.SetActive(false);
            _settings = SettingsMenu.Open(font, () =>
            {
                _settings = null;
                if (_canvas != null) _canvas.gameObject.SetActive(true);
            });
        }

        private void Close()
        {
            IsOpen = false;
            GameFlow.IsPaused = false;
            Time.timeScale = 1f;
            if (_canvas != null) Destroy(_canvas.gameObject);
            if (_settings != null) _settings.Close();
        }

        private void OnDestroy()
        {
            if (IsOpen)
            {
                GameFlow.IsPaused = false;
                Time.timeScale = 1f;
            }
        }
    }
}
