using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using EchoOfTheVoid.Save;

namespace EchoOfTheVoid.Core
{
    /// <summary>Scene flow and pause state shared by the menus and the game (spec 9.2).</summary>
    public static class GameFlow
    {
        public const string MainMenuScene = "MainMenu";
        public const string FirstGameplayScene = "Prototype_Level1";

        private static bool _isPaused;

        public static event Action OnGamePaused;
        public static event Action OnGameResumed;
        public static event Action<SaveData> OnGameLoaded;

        /// <summary>True while the pause menu is open. Anything that restores Time.timeScale must respect it.</summary>
        public static bool IsPaused
        {
            get => _isPaused;
            set
            {
                if (_isPaused == value) return;
                _isPaused = value;
                if (value) OnGamePaused?.Invoke();
                else OnGameResumed?.Invoke();
            }
        }

        /// <summary>Tests replace this to observe scene loads without needing the scenes in the build.</summary>
        public static Action<string> SceneLoader = name => SceneManager.LoadScene(name);

        public static float RestingTimeScale => IsPaused ? 0f : 1f;

        public static void StartNewGame(int slot = 0)
        {
            GameSession.StartNewGame(slot);
            OnGameLoaded?.Invoke(GameSession.Current);
            Load(FirstGameplayScene);
        }

        /// <returns>false if the slot has no valid save.</returns>
        public static bool ContinueGame(int slot = 0)
        {
            if (!GameSession.TryContinue(slot)) return false;

            string scene = GameSession.Current.sceneName;
            OnGameLoaded?.Invoke(GameSession.Current);
            Load(string.IsNullOrEmpty(scene) ? FirstGameplayScene : scene);
            return true;
        }

        public static void QuitToMenu()
        {
            IsPaused = false;
            Time.timeScale = 1f;
            Load(MainMenuScene);
        }

        public static void QuitApplication()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private static void Load(string scene)
        {
            IsPaused = false;
            Time.timeScale = 1f;
            SceneLoader(scene);
        }
    }
}
