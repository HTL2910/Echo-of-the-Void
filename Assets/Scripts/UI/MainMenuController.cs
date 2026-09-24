using UnityEngine;
using UnityEngine.UI;
using EchoOfTheVoid.Core;
using EchoOfTheVoid.Save;

namespace EchoOfTheVoid.UI
{
    /// <summary>Title screen: Continue, New Game, Settings, Quit (spec 9.2). Builds its own UI.</summary>
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Font font;
        [SerializeField] private int saveSlot = 0;

        private Canvas _canvas;
        private SettingsMenu _settings;

        public Button ContinueButton { get; private set; }
        public Button NewGameButton { get; private set; }
        public Button SettingsButton { get; private set; }
        public Button QuitButton { get; private set; }

        public void SetFont(Font value) => font = value;

        private void Start()
        {
            UiKit.EnsureEventSystem();
            Time.timeScale = 1f;
            GameFlow.IsPaused = false;
            Build();
        }

        private void Build()
        {
            if (_canvas != null) Destroy(_canvas.gameObject);
            _canvas = UiKit.CreateCanvas("MainMenuCanvas", 100);

            var panel = UiKit.CreatePanel(_canvas.transform, "Menu", UiKit.PanelDark, new Vector2(800f, 820f));
            UiKit.MakeVertical(panel, 24f, 48);

            UiKit.CreateHeading(panel, "ECHO OF THE VOID", font);
            UiKit.CreateBody(panel, "Two realities. One Voidweaver.", font, UiKit.Muted);

            bool hasSave = SaveService.TryLoad(saveSlot, out _);
            ContinueButton = UiKit.CreateButton(panel, "Continue", font, OnContinue);
            ContinueButton.interactable = hasSave;
            NewGameButton = UiKit.CreateButton(panel, "New Game", font, OnNewGame);
            SettingsButton = UiKit.CreateButton(panel, "Settings", font, OnSettings);
            QuitButton = UiKit.CreateButton(panel, "Quit", font, GameFlow.QuitApplication);

            // Keyboard / gamepad start on the most useful entry
            var first = hasSave ? ContinueButton : NewGameButton;
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(first.gameObject);
        }

        public void OnContinue()
        {
            if (!GameFlow.ContinueGame(saveSlot)) Build(); // the save vanished: refresh the buttons
        }

        public void OnNewGame() => GameFlow.StartNewGame(saveSlot);

        public void OnSettings()
        {
            if (_settings != null) return;
            _canvas.gameObject.SetActive(false);
            _settings = SettingsMenu.Open(font, () =>
            {
                _settings = null;
                if (_canvas != null) _canvas.gameObject.SetActive(true);
            });
        }
    }
}
