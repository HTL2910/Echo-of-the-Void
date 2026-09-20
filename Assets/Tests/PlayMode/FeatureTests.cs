using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using EchoOfTheVoid.Combat;
using EchoOfTheVoid.Core;
using EchoOfTheVoid.Environment;
using EchoOfTheVoid.Player;
using EchoOfTheVoid.Save;

namespace EchoOfTheVoid.Tests
{
    public class SaveServiceTests
    {
        private string _dir;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "eotv_save_test_" + System.Guid.NewGuid().ToString("N"));
            SaveService.DirectoryOverride = _dir;
        }

        [TearDown]
        public void TearDown()
        {
            SaveService.DirectoryOverride = null;
            if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
        }

        [Test]
        public void SaveThenLoad_RoundTripsProgress()
        {
            var data = new SaveData { checkpointId = "station_a", checkpointX = 3.5f, checkpointY = -1.25f, checkpointRealm = 1, maxHealth = 120, currentHealth = 80 };
            data.collectedIds.Add("heart_01");
            data.bossDefeated.Add("sentinel_01");

            Assert.IsTrue(SaveService.Save(data, 1));
            Assert.IsTrue(SaveService.TryLoad(1, out var loaded));

            Assert.AreEqual("station_a", loaded.checkpointId);
            Assert.AreEqual(3.5f, loaded.checkpointX);
            Assert.AreEqual(1, loaded.checkpointRealm);
            Assert.AreEqual(120, loaded.maxHealth);
            CollectionAssert.AreEqual(new[] { "heart_01" }, loaded.collectedIds);
            CollectionAssert.AreEqual(new[] { "sentinel_01" }, loaded.bossDefeated);
            Assert.AreEqual(SaveService.CurrentVersion, loaded.version);
        }

        [Test]
        public void SavingTwice_KeepsTheNewestAndLeavesNoTempFile()
        {
            SaveService.Save(new SaveData { currentHealth = 10 });
            SaveService.Save(new SaveData { currentHealth = 77 });

            Assert.IsTrue(SaveService.TryLoad(0, out var loaded));
            Assert.AreEqual(77, loaded.currentHealth);
            Assert.IsFalse(File.Exists(SaveService.PathFor(0) + ".tmp"));
        }

        [Test]
        public void MissingCorruptOrTooNewSaves_AreRejectedWithoutThrowing()
        {
            Assert.IsFalse(SaveService.TryLoad(0, out _), "missing file");

            Directory.CreateDirectory(_dir);
            File.WriteAllText(SaveService.PathFor(0), "{ this is not json");
            LogAssert.ignoreFailingMessages = true;
            Assert.IsFalse(SaveService.TryLoad(0, out _), "corrupt file");
            LogAssert.ignoreFailingMessages = false;

            File.WriteAllText(SaveService.PathFor(2), $"{{\"version\":{SaveService.CurrentVersion + 1}}}");
            Assert.IsFalse(SaveService.TryLoad(2, out _), "save from a newer game version");
        }

        [Test]
        public void InvalidSlots_AreRejected()
        {
            Assert.IsFalse(SaveService.Save(new SaveData(), -1));
            Assert.IsFalse(SaveService.Save(new SaveData(), SaveService.SlotCount));
            Assert.IsFalse(SaveService.TryLoad(99, out _));
        }
    }

    public class ChronoStationTests
    {
        private TestWorld _world;
        private string _dir;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "eotv_station_test_" + System.Guid.NewGuid().ToString("N"));
            SaveService.DirectoryOverride = _dir;
            EchoOfTheVoid.Core.GameSession.Reset();
            _world = TestWorld.Create(new Vector2(0f, 1.5f));
        }

        [TearDown]
        public void TearDown()
        {
            _world.Dispose();
            SaveService.DirectoryOverride = null;
            if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
        }

        private GameObject CreateStation(Vector2 floorPoint)
        {
            var go = new GameObject("Station");
            go.transform.position = floorPoint;
            var trigger = go.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(3f, 2.5f);
            trigger.offset = new Vector2(0f, 1.25f);
            go.AddComponent<ChronoStation>().Configure("test_station", 25);
            return go;
        }

        [UnityTest]
        public IEnumerator Interact_HealsSavesAndSetsTheRespawnPoint()
        {
            var station = CreateStation(new Vector2(0f, 0f));
            yield return new WaitForSeconds(0.4f); // land inside the trigger

            _world.Stats.TakeDamage(new DamageInfo(60, Vector2.zero, Vector2.zero, RealmType.Prime));
            Assert.AreEqual(40, _world.Stats.CurrentHealth);
            yield return new WaitForSeconds(1.4f); // let the 0.8s i-frames end (frame-rounding can stretch them)

            string usedId = null;
            System.Action<string> onUsed = id => usedId = id;
            ChronoStation.AnyStationUsed += onUsed;
            _world.Input.PressInteract();
            yield return null;
            yield return null;
            ChronoStation.AnyStationUsed -= onUsed;

            Assert.AreEqual("test_station", usedId, "AnyStationUsed must fire so regular enemies can respawn");
            Assert.AreEqual(65, _world.Stats.CurrentHealth, "Station heals 25 HP (spec 3.3)");
            Assert.IsTrue(SaveService.TryLoad(0, out var saved), "Station must write the save file");
            Assert.AreEqual("test_station", saved.checkpointId);
            Assert.AreEqual(65, saved.currentHealth);

            // Dying now must respawn at the station, not at the original spawn point
            _world.Player.transform.position = new Vector2(10f, 3f);
            _world.Stats.TakeDamage(new DamageInfo(999, Vector2.zero, Vector2.zero, RealmType.Prime));
            yield return new WaitForSecondsRealtime(1.6f);

            Assert.That(_world.Player.transform.position.x, Is.EqualTo(0f).Within(0.5f), "Respawn at the station");
            Object.Destroy(station);
        }

        [UnityTest]
        public IEnumerator WithoutInteract_NothingHappens()
        {
            CreateStation(new Vector2(0f, 0f));
            yield return new WaitForSeconds(0.5f);

            Assert.IsFalse(SaveService.Exists(0), "Standing in range must not save on its own");
        }
    }

    public class AnimationDriverTests
    {
        private TestWorld _world;

        [TearDown]
        public void TearDown() => _world.Dispose();

#if UNITY_EDITOR
        private static RuntimeAnimatorController BuildController()
        {
            var controller = new UnityEditor.Animations.AnimatorController();
            controller.AddLayer("Base");
            controller.layers[0].stateMachine.AddState("Idle");
            controller.AddParameter(PlayerAnimationDriver.Speed, AnimatorControllerParameterType.Float);
            controller.AddParameter(PlayerAnimationDriver.IsGrounded, AnimatorControllerParameterType.Bool);
            controller.AddParameter(PlayerAnimationDriver.IsDashing, AnimatorControllerParameterType.Bool);
            controller.AddParameter(PlayerAnimationDriver.TriggerJump, AnimatorControllerParameterType.Trigger);
            controller.AddParameter(PlayerAnimationDriver.TriggerDie, AnimatorControllerParameterType.Trigger);
            // Deliberately missing: VelocityY, Dash, Attack*, Realm... the driver must ignore them
            return controller;
        }

        private void AssignController()
        {
            var animator = _world.Player.GetComponentInChildren<Animator>();
            animator.runtimeAnimatorController = BuildController();
            _world.Player.GetComponent<PlayerAnimationDriver>().RefreshAnimator();
        }

        [UnityTest]
        public IEnumerator DrivesParametersAndTriggers_AndIgnoresMissingOnes()
        {
            _world = TestWorld.Create(new Vector2(0f, 1.5f), withAnimator: true);
            AssignController();
            var animator = _world.Player.GetComponentInChildren<Animator>();

            yield return new WaitForSeconds(0.6f); // land
            Assert.IsTrue(animator.GetBool(PlayerAnimationDriver.IsGrounded), "IsGrounded follows the controller");

            _world.Input.Move = 1f;
            yield return new WaitForSeconds(0.4f);
            Assert.Greater(animator.GetFloat(PlayerAnimationDriver.Speed), 8f, "Speed follows horizontal velocity");
            _world.Input.Move = 0f;

            _world.Input.PressJump();
            yield return null;
            yield return null;
            Assert.IsTrue(animator.GetBool(PlayerAnimationDriver.TriggerJump), "Jump trigger fires on jump");

            _world.Input.PressDash(); // 'Dash' trigger and VelocityY are not in this controller: must not throw
            yield return new WaitForSeconds(0.1f);
            Assert.IsTrue(animator.GetBool(PlayerAnimationDriver.IsDashing), "IsDashing true during the dash");
            yield return new WaitForSeconds(0.5f); // dash i-frames would otherwise block the damage

            _world.Stats.TakeDamage(new DamageInfo(999, Vector2.zero, Vector2.zero, RealmType.Prime));
            Assert.IsTrue(animator.GetBool(PlayerAnimationDriver.TriggerDie), "Die trigger fires on death");
        }
#endif

        [UnityTest]
        public IEnumerator WithoutAnAnimator_NothingBreaks()
        {
            _world = TestWorld.Create(new Vector2(0f, 1.5f), withAnimator: false);
            _world.Input.Move = 1f;
            _world.Input.PressJump();
            yield return new WaitForSeconds(0.5f);

            Assert.IsNotNull(_world.Player, "No exception, player still alive");
            Assert.IsFalse(_world.Stats.IsDead);
        }
    }
}

namespace EchoOfTheVoid.Tests
{
    public class ContractTests
    {
        private TestWorld _world;

        [UnityEngine.TestTools.UnityTearDown]
        public System.Collections.IEnumerator TearDown()
        {
            _world?.Dispose();
            _world = null;
            yield break;
        }

        [UnityEngine.TestTools.UnityTest]
        public System.Collections.IEnumerator SoftRespawn_ReturnsToLastSafeGround_WithoutLosingHealth()
        {
            _world = TestWorld.Create(new UnityEngine.Vector2(0f, 1.5f));
            yield return new UnityEngine.WaitForSeconds(0.6f); // land: this is the safe spot
            UnityEngine.Vector3 safe = _world.Player.transform.position;
            int hp = _world.Stats.CurrentHealth;

            _world.Player.transform.position = new UnityEngine.Vector2(9f, 5f); // "fell into a hazard" mid-air
            Assert.IsTrue(_world.Respawn.SoftRespawn());
            Assert.IsFalse(_world.Respawn.SoftRespawn(), "A second call while respawning is refused");
            yield return new UnityEngine.WaitForSecondsRealtime(0.6f);

            Assert.That(_world.Player.transform.position.x, NUnit.Framework.Is.EqualTo(safe.x).Within(0.6f));
            Assert.AreEqual(hp, _world.Stats.CurrentHealth, "Soft respawn costs no health (spec D9)");
            Assert.IsTrue(_world.Controller.enabled);
        }

        [NUnit.Framework.Test]
        public void BossEvents_DeliverTheirPayloads()
        {
            string engagedId = null, defeatedId = null; int engagedMax = 0, current = 0;
            System.Action<string, string, int> onEngaged = (id, name, max) => { engagedId = id; engagedMax = max; };
            System.Action<string, int, int> onHealth = (id, cur, max) => current = cur;
            System.Action<string> onDefeated = id => defeatedId = id;

            EchoOfTheVoid.Core.BossEvents.Engaged += onEngaged;
            EchoOfTheVoid.Core.BossEvents.HealthChanged += onHealth;
            EchoOfTheVoid.Core.BossEvents.Defeated += onDefeated;
            try
            {
                EchoOfTheVoid.Core.BossEvents.RaiseEngaged("sentinel_01", "Sentinel-01", 600);
                EchoOfTheVoid.Core.BossEvents.RaiseHealthChanged("sentinel_01", 450, 600);
                EchoOfTheVoid.Core.BossEvents.RaiseDefeated("sentinel_01");
            }
            finally
            {
                EchoOfTheVoid.Core.BossEvents.Engaged -= onEngaged;
                EchoOfTheVoid.Core.BossEvents.HealthChanged -= onHealth;
                EchoOfTheVoid.Core.BossEvents.Defeated -= onDefeated;
            }

            Assert.AreEqual("sentinel_01", engagedId);
            Assert.AreEqual(600, engagedMax);
            Assert.AreEqual(450, current);
            Assert.AreEqual("sentinel_01", defeatedId);
        }
    }
}
