using UnityEngine;
using UnityEngine.SceneManagement;
using EchoOfTheVoid.Save;

namespace EchoOfTheVoid.Core
{
    /// <summary>
    /// Runtime singleton that tracks the current level and handles level-to-level progression.
    /// Lives on a DontDestroyOnLoad GameObject; created automatically on first access.
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        public static LevelManager Instance { get; private set; }

        // ── Current state ────────────────────────────────────────────────────────
        public int   CurrentLevelIndex { get; private set; } = 1;
        public LevelDef CurrentLevel   => LevelProgression.Get(CurrentLevelIndex);
        public bool  IsLoadingLevel    { get; private set; }

        // ── Events ───────────────────────────────────────────────────────────────
        public static event System.Action<LevelDef> OnLevelStarted;
        public static event System.Action<LevelDef> OnLevelCompleted;

        // ── Bootstrap ────────────────────────────────────────────────────────────
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureExists()
        {
            // Already exists (e.g. domain reload off)
            if (Instance != null) return;
            var go = new GameObject("[LevelManager]");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<LevelManager>();
            SceneManager.sceneLoaded += Instance.OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (Instance == this) Instance = null;
        }

        // ── Scene loaded hook ────────────────────────────────────────────────────
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Infer level index from scene name "Level_XX"
            if (scene.name.StartsWith("Level_") && int.TryParse(scene.name.Substring(6), out int idx))
            {
                CurrentLevelIndex = idx;
                IsLoadingLevel = false;
                OnLevelStarted?.Invoke(CurrentLevel);

                // Persist scene name into save
                if (GameSession.Current != null)
                    GameSession.Current.sceneName = scene.name;
            }
        }

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>Load a specific level by 1-based index. Saves current session progress.</summary>
        public void LoadLevel(int levelIndex)
        {
            if (IsLoadingLevel) return;
            var def = LevelProgression.Get(levelIndex);
            if (def == null)
            {
                Debug.LogWarning($"[LevelManager] Level index {levelIndex} out of range (1-{LevelProgression.TotalLevels})");
                return;
            }

            IsLoadingLevel = true;
            CurrentLevelIndex = levelIndex;

            string sceneName = LevelProgression.SceneName(levelIndex);
            Debug.Log($"[LevelManager] Loading Level {levelIndex}: {def.DisplayName} → scene '{sceneName}'");

            GameFlow.SceneLoader(sceneName);
        }

        /// <summary>Called by LevelGoalTrigger when the player reaches the exit rift.</summary>
        public void CompleteCurrentLevel()
        {
            var def = CurrentLevel;
            if (def == null) return;

            OnLevelCompleted?.Invoke(def);

            // Persist completion
            GameSession.MarkBossDefeated($"LevelComplete_{CurrentLevelIndex}");
            SaveService.TrySave(GameSession.ActiveSlot, GameSession.Current);

            int next = CurrentLevelIndex + 1;
            if (next > LevelProgression.TotalLevels)
            {
                Debug.Log("[LevelManager] All 20 levels complete — CREDITS");
                GameFlow.SceneLoader(GameFlow.MainMenuScene);
            }
            else
            {
                LoadLevel(next);
            }
        }

        /// <summary>Restart the current level from the beginning.</summary>
        public void RestartCurrentLevel() => LoadLevel(CurrentLevelIndex);

        /// <summary>Go to the previous level (used by debug/dev tools).</summary>
        public void LoadPreviousLevel()
        {
            if (CurrentLevelIndex > 1) LoadLevel(CurrentLevelIndex - 1);
        }

        /// <summary>Check if a level has been completed (saved in session).</summary>
        public bool IsLevelCompleted(int levelIndex) =>
            GameSession.IsBossDefeated($"LevelComplete_{levelIndex}");

        // ── Convenience static shorthands ────────────────────────────────────────
        public static void GoNext()     => Instance?.CompleteCurrentLevel();
        public static void Restart()    => Instance?.RestartCurrentLevel();
        public static void GoTo(int i)  => Instance?.LoadLevel(i);
    }
}
