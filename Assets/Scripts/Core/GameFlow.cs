using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using EchoOfTheVoid.Save;


namespace EchoOfTheVoid.Core
{
    /// <summary>Scene flow and pause state shared by the menus and the game (spec 9.2).</summary>
    public static class GameFlow
    {
        public const string MainMenuScene           = "MainMenu";
        /// <summary>Campaign entry point. Falls back to Prototype_Level1 until 'Build All 20 Levels' has been run.</summary>
        public const string FirstGameplayScene        = "Level_01";
        public const string LegacyPrototypeScene      = "Prototype_Level1";

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
            // Use Level_01 if it's in Build Settings; otherwise fall back to legacy prototype
            string target = IsSceneInBuild(LevelProgression.SceneName(1))
                ? LevelProgression.SceneName(1)
                : LegacyPrototypeScene;
            Load(target);
        }

        /// <returns>false if the slot has no valid save.</returns>
        public static bool ContinueGame(int slot = 0)
        {
            if (!GameSession.TryContinue(slot)) return false;

            string scene = GameSession.Current.sceneName;
            // Migrate legacy save / fallback when campaign scenes not yet generated
            if (string.IsNullOrEmpty(scene) || scene == LegacyPrototypeScene
                || !IsSceneInBuild(scene))
            {
                scene = IsSceneInBuild(LevelProgression.SceneName(1))
                    ? LevelProgression.SceneName(1)
                    : LegacyPrototypeScene;
            }

            OnGameLoaded?.Invoke(GameSession.Current);
            Load(scene);
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

        /// <summary>
        /// Returns true if the scene name exists in the current Build Settings.
        /// Works in both Editor and runtime builds.
        /// </summary>
        public static bool IsSceneInBuild(string sceneName)
        {
            int count = SceneManager.sceneCountInBuildSettings;
            for (int i = 0; i < count; i++)
            {
                string path = SceneUtility.GetScenePathByBuildIndex(i);
                // path is like "Assets/Scenes/Level_01.unity" — compare by filename
                string name = System.IO.Path.GetFileNameWithoutExtension(path);
                if (string.Equals(name, sceneName, System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
    }
}

