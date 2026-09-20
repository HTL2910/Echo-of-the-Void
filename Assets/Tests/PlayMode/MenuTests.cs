using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using UnityEngine.UI;
using EchoOfTheVoid.Core;
using EchoOfTheVoid.Save;
using EchoOfTheVoid.Settings;
using EchoOfTheVoid.UI;

namespace EchoOfTheVoid.Tests
{
    public class MenuTests
    {
        private string _dir;
        private readonly List<string> _loadedScenes = new List<string>();
        private System.Action<string> _originalLoader;
        private GameObject _host;
        private TestWorld _world;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "eotv_menu_test_" + System.Guid.NewGuid().ToString("N"));
            SaveService.DirectoryOverride = _dir;
            SettingsService.DirectoryOverride = _dir;
            SettingsService.Reload();
            InputBindings.RefreshFromSettings();
            GameSession.Reset();

            _loadedScenes.Clear();
            _originalLoader = GameFlow.SceneLoader;
            GameFlow.SceneLoader = name => _loadedScenes.Add(name);
        }

        [TearDown]
        public void TearDown()
        {
            _world?.Dispose();
            _world = null;
            if (_host != null) Object.Destroy(_host);
            foreach (var go in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)) Object.Destroy(go.gameObject);
            foreach (var es in Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None)) Object.Destroy(es.gameObject);

            GameFlow.SceneLoader = _originalLoader;
            GameFlow.IsPaused = false;
            Time.timeScale = 1f;
            SaveService.DirectoryOverride = null;
            SettingsService.DirectoryOverride = null;
            SettingsService.Reload();
            InputBindings.RefreshFromSettings();
            GameSession.Reset();
            if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
        }

        // ---------------------------------------------------------------- main menu
        [UnityTest]
        public IEnumerator MainMenu_WithoutASave_DisablesContinue_AndNewGameLoadsTheFirstLevel()
        {
            _host = new GameObject("Menu");
            var menu = _host.AddComponent<MainMenuController>();
            yield return null;

            Assert.IsFalse(menu.ContinueButton.interactable, "Nothing to continue");
            Assert.IsTrue(menu.NewGameButton.interactable);

            menu.NewGameButton.onClick.Invoke();
            CollectionAssert.AreEqual(new[] { GameFlow.FirstGameplayScene }, _loadedScenes);
            Assert.IsNull(GameSession.PendingLoad, "A new game has nothing to restore");
        }

        [UnityTest]
        public IEnumerator MainMenu_WithASave_ContinuesInTheSavedScene()
        {
            SaveService.Save(new SaveData { sceneName = "Zone1_Room07", checkpointX = 3f, checkpointY = 2f, currentHealth = 60 }, 0);
            _host = new GameObject("Menu");
            var menu = _host.AddComponent<MainMenuController>();
            yield return null;

            Assert.IsTrue(menu.ContinueButton.interactable);
            menu.ContinueButton.onClick.Invoke();

            CollectionAssert.AreEqual(new[] { "Zone1_Room07" }, _loadedScenes);
            Assert.IsNotNull(GameSession.PendingLoad, "The save is queued for SaveBootstrap");
            Assert.AreEqual(60, GameSession.PendingLoad.currentHealth);
        }

        [UnityTest]
        public IEnumerator MainMenu_SettingsOpensAndBackReturns()
        {
            _host = new GameObject("Menu");
            var menu = _host.AddComponent<MainMenuController>();
            yield return null;

            menu.SettingsButton.onClick.Invoke();
            yield return null;
            var settings = Object.FindFirstObjectByType<SettingsMenu>();
            Assert.IsNotNull(settings, "The settings screen opens");
            Assert.IsFalse(menu.NewGameButton.gameObject.activeInHierarchy, "The main menu hides behind it");

            settings.BackButton.onClick.Invoke();
            yield return null;
            Assert.IsTrue(menu.NewGameButton.gameObject.activeInHierarchy, "Back returns to the main menu");
        }

        // ---------------------------------------------------------------- pause
        [UnityTest]
        public IEnumerator Pause_FreezesTime_BlocksKaelsInput_AndResumes()
        {
            _world = TestWorld.Create(new Vector2(0f, 1.5f));
            _host = new GameObject("Pause");
            var pause = _host.AddComponent<PauseController>();
            yield return new WaitForSeconds(0.6f);

            pause.Toggle();
            yield return null;
            Assert.IsTrue(pause.IsOpen);
            Assert.IsTrue(GameFlow.IsPaused);
            Assert.AreEqual(0f, Time.timeScale);

            _world.Input.PressAttack();
            yield return null;
            yield return null;
            Assert.IsFalse(_world.Controller.IsAttacking, "Kael ignores input while the game is paused");

            pause.ResumeButton.onClick.Invoke();
            yield return null;
            Assert.IsFalse(pause.IsOpen);
            Assert.IsFalse(GameFlow.IsPaused);
            Assert.AreEqual(1f, Time.timeScale);
        }

        [UnityTest]
        public IEnumerator Pause_QuitToMenu_ReturnsToTheMainMenuScene_AndUnpauses()
        {
            _host = new GameObject("Pause");
            var pause = _host.AddComponent<PauseController>();
            pause.Open();
            yield return null;

            pause.QuitToMenuButton.onClick.Invoke();

            CollectionAssert.AreEqual(new[] { GameFlow.MainMenuScene }, _loadedScenes);
            Assert.AreEqual(1f, Time.timeScale, "The menu must not inherit a frozen game");
            Assert.IsFalse(GameFlow.IsPaused);
        }

        // ---------------------------------------------------------------- settings screen
        [UnityTest]
        public IEnumerator SettingsScreen_ChangesAreAppliedAndSaved()
        {
            var settings = SettingsMenu.Open(null, null);
            yield return null;

            settings.ReduceFlashingToggle.isOn = true;
            settings.MusicSlider.value = 0.25f;
            settings.DamageButton.onClick.Invoke(); // 100% -> 75%
            yield return null;

            Assert.IsTrue(SettingsService.Current.reduceFlashing);
            Assert.AreEqual(0.25f, SettingsService.Current.musicVolume, 0.001f);
            Assert.AreEqual(0.75f, SettingsService.Current.damageTakenMultiplier, 0.001f);

            SettingsService.Reload(); // restart: the file has them
            Assert.IsTrue(SettingsService.Current.reduceFlashing);
            Assert.AreEqual(0.75f, SettingsService.Current.damageTakenMultiplier, 0.001f);
        }

        [Test]
        public void DamageAssist_CyclesThroughTheThreeSteps()
        {
            Assert.AreEqual(0.75f, SettingsMenu.NextDamageStep(1f));
            Assert.AreEqual(0.5f, SettingsMenu.NextDamageStep(0.75f));
            Assert.AreEqual(1f, SettingsMenu.NextDamageStep(0.5f));
            Assert.AreEqual("Assist: damage taken 75%", SettingsMenu.DamageLabel(0.75f));
        }

        [UnityTest]
        public IEnumerator ControlsScreen_RebindsAKey_AndShowsTheWaitingState()
        {
            var settings = SettingsMenu.Open(null, null);
            yield return null;
            settings.ControlsButton.onClick.Invoke();
            yield return null;

            settings.BeginKey(PlayerAction.Jump, 0);
            Assert.IsTrue(settings.IsListening);
            var label = FindLabelOnButton(settings, "Press...");
            Assert.IsNotNull(label, "The slot being rebound shows 'Press...'");

            Assert.IsTrue(settings.Session.CommitKey(Key.F));
            yield return null;

            Assert.IsFalse(settings.IsListening);
            Assert.AreEqual(Key.F, InputBindings.Current.GetKeys(PlayerAction.Jump)[0]);
            Assert.IsNull(FindLabelOnButton(settings, "Press..."));

            SettingsService.Reload();
            InputBindings.RefreshFromSettings();
            Assert.AreEqual(Key.F, InputBindings.Current.GetKeys(PlayerAction.Jump)[0], "The new binding survives a restart");
        }

        [UnityTest]
        public IEnumerator ControlsScreen_EscapeCancelsAndResetRestoresDefaults()
        {
            var settings = SettingsMenu.Open(null, null);
            yield return null;
            settings.ControlsButton.onClick.Invoke();
            yield return null;

            InputBindings.Current.SetKey(PlayerAction.Attack, 0, Key.G);
            settings.BeginKey(PlayerAction.Dash, 0);
            Assert.IsFalse(settings.Session.CommitKey(Key.Escape), "Escape is not a binding");
            Assert.IsFalse(settings.IsListening, "Escape cancels");
            Assert.AreEqual(Key.K, InputBindings.Current.GetKeys(PlayerAction.Dash)[0], "Unchanged");

            settings.ResetBindingsButton.onClick.Invoke();
            Assert.AreEqual(Key.J, InputBindings.Current.GetKeys(PlayerAction.Attack)[0], "Reset restored the default");
        }

        private static Text FindLabelOnButton(SettingsMenu settings, string text)
        {
            foreach (var label in settings.GetComponentsInChildren<Text>())
                if (label.text == text && label.GetComponentInParent<Button>() != null) return label;
            return null;
        }
    }
}
