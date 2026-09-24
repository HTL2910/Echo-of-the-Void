using System.IO;
using NUnit.Framework;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using EchoOfTheVoid.Player;
using EchoOfTheVoid.Settings;

namespace EchoOfTheVoid.Tests
{
    public class SettingsTests
    {
        private string _dir;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "eotv_settings_test_" + System.Guid.NewGuid().ToString("N"));
            SettingsService.DirectoryOverride = _dir;
            SettingsService.Reload();
        }

        [TearDown]
        public void TearDown()
        {
            SettingsService.DirectoryOverride = null;
            SettingsService.Reload();
            InputBindings.RefreshFromSettings();
            if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
        }

        [Test]
        public void NoFile_GivesTheDefaults()
        {
            Assert.AreEqual(1f, SettingsService.Current.masterVolume);
            Assert.AreEqual(1f, SettingsService.Current.screenShake);
            Assert.IsFalse(SettingsService.Current.reduceFlashing);
            Assert.AreEqual(1f, SettingsService.Current.damageTakenMultiplier);
        }

        [Test]
        public void ChangesSurviveARestart_AndAreClamped()
        {
            var s = SettingsService.Current;
            s.musicVolume = 0.35f;
            s.reduceFlashing = true;
            s.screenShake = 5f;            // out of range
            s.damageTakenMultiplier = 0f;  // out of range
            int notified = 0;
            System.Action onChanged = () => notified++;
            SettingsService.Changed += onChanged;
            SettingsService.Apply();
            SettingsService.Changed -= onChanged;
            Assert.AreEqual(1, notified);

            SettingsService.Reload(); // "restart the game"
            Assert.AreEqual(0.35f, SettingsService.Current.musicVolume, 0.0001f);
            Assert.IsTrue(SettingsService.Current.reduceFlashing);
            Assert.AreEqual(1f, SettingsService.Current.screenShake, "Clamped to 0..1");
            Assert.AreEqual(0.25f, SettingsService.Current.damageTakenMultiplier, "Assist damage never drops below 25%");
        }

        [Test]
        public void CorruptFile_FallsBackToDefaults_WithoutThrowing()
        {
            Directory.CreateDirectory(_dir);
            File.WriteAllText(SettingsService.FilePath, "{{{ not json");
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
            SettingsService.Reload();
            Assert.AreEqual(0.8f, SettingsService.Current.musicVolume, 0.0001f);
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = false;
        }
    }

    public class InputBindingsTests
    {
        [Test]
        public void Defaults_MatchTheSpec()
        {
            var b = new InputBindings();
            CollectionAssert.AreEqual(new[] { Key.LeftShift, Key.RightShift }, b.GetKeys(PlayerAction.Shift));
            CollectionAssert.AreEqual(new[] { Key.K, Key.LeftCtrl }, b.GetKeys(PlayerAction.Dash));
            CollectionAssert.AreEqual(new[] { Key.E }, b.GetKeys(PlayerAction.Interact));
            Assert.AreEqual(GamepadButton.RightShoulder, b.GetPadButton(PlayerAction.Shift));
            Assert.AreEqual(GamepadButton.East, b.GetPadButton(PlayerAction.Dash));
            Assert.AreEqual(GamepadButton.North, b.GetPadButton(PlayerAction.Interact));
            Assert.IsNull(b.GetPadButton(PlayerAction.MoveLeft), "Movement is the stick");
        }

        [Test]
        public void Rebinding_TakesTheKeyAwayFromTheActionThatHadIt()
        {
            var b = new InputBindings();
            b.SetKey(PlayerAction.Jump, 0, Key.K);   // K was Dash's first key

            Assert.AreEqual(Key.K, b.GetKeys(PlayerAction.Jump)[0]);
            CollectionAssert.DoesNotContain(b.GetKeys(PlayerAction.Dash), Key.K);
            CollectionAssert.Contains(b.GetKeys(PlayerAction.Dash), Key.LeftCtrl, "Dash keeps its other key");
        }

        [Test]
        public void Rebinding_NeverLeavesAnActionWithoutAKey()
        {
            var b = new InputBindings();
            // Interact has only E. Give E to Jump: Interact must get Jump's old key back (a swap)
            Key oldJump = b.GetKeys(PlayerAction.Jump)[0];
            b.SetKey(PlayerAction.Jump, 0, Key.E);

            Assert.AreEqual(Key.E, b.GetKeys(PlayerAction.Jump)[0]);
            Assert.AreEqual(1, b.GetKeys(PlayerAction.Interact).Count);
            Assert.AreEqual(oldJump, b.GetKeys(PlayerAction.Interact)[0]);
        }

        [Test]
        public void Rebinding_ToTheOtherSlotOfTheSameAction_Swaps()
        {
            var b = new InputBindings();
            b.SetKey(PlayerAction.Jump, 0, Key.W); // W was Jump slot 1

            CollectionAssert.AreEqual(new[] { Key.W, Key.Space }, b.GetKeys(PlayerAction.Jump));
        }

        [Test]
        public void GamepadRebinding_SwapsWithTheActionThatHadTheButton()
        {
            var b = new InputBindings();
            b.SetPadButton(PlayerAction.Jump, GamepadButton.East); // East was Dash

            Assert.AreEqual(GamepadButton.East, b.GetPadButton(PlayerAction.Jump));
            Assert.AreEqual(GamepadButton.South, b.GetPadButton(PlayerAction.Dash), "Dash gets Jump's old button");
            b.SetPadButton(PlayerAction.MoveLeft, GamepadButton.South); // the stick cannot be rebound
            Assert.IsNull(b.GetPadButton(PlayerAction.MoveLeft));
        }

        [Test]
        public void Bindings_RoundTripThroughSettings()
        {
            var b = new InputBindings();
            b.SetKey(PlayerAction.Attack, 0, Key.F);
            b.SetPadButton(PlayerAction.Shift, GamepadButton.LeftShoulder);

            var data = new SettingsData();
            b.WriteTo(data);
            string json = UnityEngine.JsonUtility.ToJson(data);
            var restored = InputBindings.FromSettings(UnityEngine.JsonUtility.FromJson<SettingsData>(json));

            Assert.AreEqual(Key.F, restored.GetKeys(PlayerAction.Attack)[0]);
            Assert.AreEqual(GamepadButton.LeftShoulder, restored.GetPadButton(PlayerAction.Shift));
            CollectionAssert.AreEqual(new[] { Key.K, Key.LeftCtrl }, restored.GetKeys(PlayerAction.Dash), "Untouched actions keep their defaults");
        }

        [Test]
        public void DevicePlayerInput_WithNoDevices_ReturnsAnEmptyFrame()
        {
            var frame = new DevicePlayerInput().Poll();
            Assert.AreEqual(0f, frame.Move);
            Assert.IsFalse(frame.JumpPressed);
            Assert.IsFalse(frame.ShiftPressed);
        }
    }
}

namespace EchoOfTheVoid.Tests
{
    using System.Collections;
    using UnityEngine;
    using UnityEngine.TestTools;
    using EchoOfTheVoid.Combat;
    using EchoOfTheVoid.Core;
    using EchoOfTheVoid.Feedback;

    /// <summary>Accessibility and assist options must actually change the game (spec 9.6).</summary>
    public class SettingsEffectTests
    {
        private string _dir;
        private TestWorld _world;
        private GameObject _extra;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "eotv_effects_test_" + System.Guid.NewGuid().ToString("N"));
            SettingsService.DirectoryOverride = _dir;
            SettingsService.Reload();
        }

        [TearDown]
        public void TearDown()
        {
            _world?.Dispose();
            _world = null;
            if (_extra != null) Object.Destroy(_extra);
            if (ScreenFader.Instance != null) Object.Destroy(ScreenFader.Instance.gameObject);
            GameFlow.IsPaused = false;
            Time.timeScale = 1f;
            AudioListener.volume = 1f;
            SettingsService.DirectoryOverride = null;
            SettingsService.Reload();
            if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
        }

        [UnityTest]
        public IEnumerator AssistDamage_ScalesHitsDown_ButNeverToZero()
        {
            _world = TestWorld.Create(new Vector2(0f, 1.5f));
            SettingsService.Current.damageTakenMultiplier = 0.5f;
            yield return new WaitForSeconds(0.4f);

            _world.Stats.TakeDamage(new DamageInfo(20, Vector2.zero, Vector2.zero, RealmType.Prime));
            Assert.AreEqual(90, _world.Stats.CurrentHealth, "20 damage at 50% is 10");

            yield return new WaitForSeconds(1.4f); // i-frames over
            SettingsService.Current.damageTakenMultiplier = 0.25f;
            _world.Stats.TakeDamage(new DamageInfo(1, Vector2.zero, Vector2.zero, RealmType.Prime));
            Assert.AreEqual(89, _world.Stats.CurrentHealth, "A hit always costs at least 1");
        }

        [UnityTest]
        public IEnumerator DisabledHitstop_DoesNotFreezeTime()
        {
            _extra = new GameObject("HitStop");
            var hitStop = _extra.AddComponent<HitStopManager>();
            SettingsService.Current.disableHitstop = true;

            hitStop.TriggerHitStop(0.3f, 0f);
            yield return null;
            Assert.AreEqual(1f, Time.timeScale, "No freeze when the option is on");

            SettingsService.Current.disableHitstop = false;
            hitStop.TriggerHitStop(0.2f, 0f);
            yield return null;
            Assert.AreEqual(0f, Time.timeScale, "Normal hitstop freezes time");
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.AreEqual(1f, Time.timeScale);
        }

        [UnityTest]
        public IEnumerator HitstopEnding_DoesNotUnpauseAPausedGame()
        {
            _extra = new GameObject("HitStop");
            var hitStop = _extra.AddComponent<HitStopManager>();

            hitStop.TriggerHitStop(0.1f, 0f);
            GameFlow.IsPaused = true; // the player pauses during the freeze
            yield return new WaitForSecondsRealtime(0.3f);

            Assert.AreEqual(0f, Time.timeScale, "The pause must survive the end of the hitstop");
        }

        [UnityTest]
        public IEnumerator ScreenShakeSlider_ScalesOrDisablesShake()
        {
            _extra = new GameObject("Shake");
            var shake = _extra.AddComponent<CameraShakeManager>();

            SettingsService.Current.screenShake = 0f;
            shake.ShakeHeavy();
            yield return null;
            yield return null;
            Assert.AreEqual(Vector3.zero, shake.ShakeOffset, "Shake off");

            SettingsService.Current.screenShake = 1f;
            float peak = 0f;
            shake.ShakeHeavy();
            for (int i = 0; i < 12; i++) { yield return null; peak = Mathf.Max(peak, shake.ShakeOffset.magnitude); }
            Assert.Greater(peak, 0.05f, "Shake on");
        }

        [UnityTest]
        public IEnumerator ReduceFlashing_CapsTheRoomFlash()
        {
            SettingsService.Current.reduceFlashing = true;
            ScreenFader.Flash(0.15f);
            yield return null;
            Assert.LessOrEqual(ScreenFader.Instance.Alpha, 0.26f, "Photosensitivity option limits the flash");

            Object.Destroy(ScreenFader.Instance.gameObject);
            yield return null;
            SettingsService.Current.reduceFlashing = false;
            ScreenFader.Flash(0.15f);
            yield return null;
            Assert.Greater(ScreenFader.Instance.Alpha, 0.6f);
        }

        [UnityTest]
        public IEnumerator MusicAndMasterVolume_FollowTheSliders()
        {
            SettingsService.Current.musicVolume = 0.5f;
            SettingsService.Current.masterVolume = 0.4f;
            SettingsService.Apply();
            Assert.AreEqual(0.4f, AudioListener.volume, 0.0001f, "Master volume drives the global listener volume");

            _extra = new GameObject("Music");
            var music = _extra.AddComponent<MusicLayerController>();
            music.Configure(AudioClip.Create("p", 44100, 1, 44100, false), AudioClip.Create("e", 44100, 1, 44100, false));
            yield return null;
            yield return null;

            var prime = _extra.transform.Find("Stem_Prime").GetComponent<AudioSource>();
            Assert.AreEqual(0.6f * 0.5f, prime.volume, 0.001f, "Stem volume = base 0.6 x music slider 0.5");
        }

        [UnityTest]
        public IEnumerator AssistExtendedCoyote_AllowsJumpAfterLeaving()
        {
            _world = TestWorld.Create(new Vector2(0f, 1.5f));
            SettingsService.Current.extendedCoyoteTime = true;
            yield return new WaitForSeconds(0.1f);

            // Leave ground
            _world.Input.SimulateMove(1f);
            yield return new WaitForSeconds(0.05f);
            _world.Input.SimulateJump(true);
            _world.Input.SimulateJump(false);

            // Should jump because coyote extends to 0.2s
            yield return new WaitForSeconds(0.05f);
            float groundedHeight = _world.Controller.transform.position.y;

            yield return new WaitForSeconds(0.08f);
            _world.Input.SimulateJump(true);
            _world.Input.SimulateJump(false);
            yield return null;

            float afterSecondJump = _world.Controller.transform.position.y;
            Assert.Greater(afterSecondJump, groundedHeight, "Second jump with extended coyote succeeded");
        }

        [UnityTest]
        public IEnumerator AssistExtendedIFrames_IncreasesInvulnerabilityDuration()
        {
            _world = TestWorld.Create(new Vector2(0f, 1.5f));
            SettingsService.Current.extendedIFrames = true;
            yield return new WaitForSeconds(0.4f);

            // Take damage to trigger i-frames
            _world.Stats.TakeDamage(new DamageInfo(20, Vector2.zero, Vector2.zero, RealmType.Prime));
            Assert.IsTrue(_world.Stats.IsInvulnerable);

            // Normal duration is 0.8s, extended is 1.2s (0.8 * 1.5)
            yield return new WaitForSeconds(0.9f);
            Assert.IsTrue(_world.Stats.IsInvulnerable, "Extended i-frames last longer than 0.8s");

            yield return new WaitForSeconds(0.4f);
            Assert.IsFalse(_world.Stats.IsInvulnerable, "Extended i-frames expire after 1.2s");
        }

        [UnityTest]
        public IEnumerator AssistDefaultSettings_NoExtensions()
        {
            _world = TestWorld.Create(new Vector2(0f, 1.5f));
            SettingsService.Current.extendedCoyoteTime = false;
            SettingsService.Current.extendedIFrames = false;
            yield return new WaitForSeconds(0.1f);

            // Take damage, normal i-frames
            _world.Stats.TakeDamage(new DamageInfo(20, Vector2.zero, Vector2.zero, RealmType.Prime));
            yield return new WaitForSeconds(0.85f);
            Assert.IsFalse(_world.Stats.IsInvulnerable, "Normal i-frames expire after 0.8s");
        }
    }
}
