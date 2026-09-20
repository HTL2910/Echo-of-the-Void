using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using EchoOfTheVoid.Combat;
using EchoOfTheVoid.Core;
using EchoOfTheVoid.Environment;
using EchoOfTheVoid.Save;

namespace EchoOfTheVoid.Tests
{
    /// <summary>New Game / Continue flow and persistent world state (spec 3.5b).</summary>
    public class SessionTests
    {
        private string _dir;
        private TestWorld _world;
        private GameObject _manager;
        private GameObject _extra;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "eotv_session_test_" + System.Guid.NewGuid().ToString("N"));
            SaveService.DirectoryOverride = _dir;
            GameSession.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            _world?.Dispose();
            _world = null;
            if (_manager != null) Object.Destroy(_manager);
            if (_extra != null) Object.Destroy(_extra);
            GameSession.Reset();
            SaveService.DirectoryOverride = null;
            if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
        }

        [Test]
        public void Continue_WithoutASave_Fails_AndNewGameIsBlank()
        {
            Assert.IsFalse(GameSession.TryContinue(0));
            Assert.IsNull(GameSession.PendingLoad);

            GameSession.MarkCollected("old");
            GameSession.StartNewGame(1);
            Assert.AreEqual(1, GameSession.ActiveSlot);
            Assert.IsFalse(GameSession.IsCollected("old"), "A new game starts with a clean slate");
            Assert.IsNull(GameSession.PendingLoad);
        }

        [UnityTest]
        public IEnumerator Continue_PutsKaelAtTheSavedCheckpoint_WithHealthAbilitiesAndRealm()
        {
            var saved = new SaveData
            {
                sceneName = "Prototype_Level1",
                checkpointId = "station_x",
                checkpointX = 5f, checkpointY = 3f, checkpointRealm = (int)RealmType.Echo,
                maxHealth = 120, currentHealth = 40,
                abilityFlags = (int)(AbilityFlags.RealityShift | AbilityFlags.PhaseDash | AbilityFlags.WallJump)
            };
            Assert.IsTrue(SaveService.Save(saved, 2));
            Assert.IsTrue(GameSession.TryContinue(2));
            Assert.AreEqual(2, GameSession.ActiveSlot);

            _manager = new GameObject("Manager");
            _manager.AddComponent<RealityManager>();
            _world = TestWorld.Create(new Vector2(-20f, 1.5f)); // the scene's default spawn, far from the save
            _manager.AddComponent<SaveBootstrap>();
            yield return new WaitForSeconds(0.2f);

            Assert.That(_world.Player.transform.position.x, Is.EqualTo(5f).Within(0.3f), "Kael is placed at the saved checkpoint");
            Assert.AreEqual(RealmType.Echo, RealityManager.Instance.CurrentRealm, "Realm restored");
            Assert.AreEqual(40, _world.Stats.CurrentHealth);
            Assert.AreEqual(120, _world.Stats.MaxHealth);
            Assert.IsTrue(_world.Abilities.Has(AbilityFlags.WallJump), "Abilities restored");
            Assert.IsNull(GameSession.PendingLoad, "The pending load is consumed once applied");

            // Dying returns to the *saved* checkpoint, not the scene's spawn
            _world.Player.transform.position = new Vector2(12f, 4f);
            _world.Stats.TakeDamage(new DamageInfo(999, Vector2.zero, Vector2.zero, RealmType.Echo));
            yield return new WaitForSecondsRealtime(1.6f);
            Assert.That(_world.Player.transform.position.x, Is.EqualTo(5f).Within(0.5f));
        }

        [UnityTest]
        public IEnumerator Station_KeepsCollectedItemsAndDefeatedBosses_AcrossSaves()
        {
            _world = TestWorld.Create(new Vector2(0f, 1.5f));
            GameSession.StartNewGame(0);
            GameSession.MarkCollected("heart_01");
            GameSession.MarkBossDefeated("sentinel_01");
            yield return new WaitForSeconds(0.5f);

            var stationGo = new GameObject("Station");
            _extra = stationGo;
            var trigger = stationGo.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            stationGo.AddComponent<ChronoStation>().Configure("st", 25);
            stationGo.GetComponent<ChronoStation>().Activate(_world.Controller);

            Assert.IsTrue(SaveService.TryLoad(0, out var loaded));
            CollectionAssert.Contains(loaded.collectedIds, "heart_01");
            CollectionAssert.Contains(loaded.bossDefeated, "sentinel_01");
            Assert.AreEqual("st", loaded.checkpointId);
        }

        [UnityTest]
        public IEnumerator Pickup_StaysGone_OnceCollected_InThisSave()
        {
            _world = TestWorld.Create(new Vector2(0f, 1.5f));

            GameObject MakePickup()
            {
                var go = new GameObject("Pickup");
                go.transform.position = new Vector2(0f, 0.8f);
                var circle = go.AddComponent<CircleCollider2D>();
                circle.isTrigger = true;
                circle.radius = 0.7f;
                go.AddComponent<PersistentId>().SetId("pickup_walljump");
                go.AddComponent<AbilityPickup>().Configure(AbilityFlags.WallJump, "PISTON BOOTS");
                return go;
            }

            var first = MakePickup();
            yield return new WaitForSeconds(0.6f);
            Assert.IsTrue(first == null, "Collected");
            Assert.IsTrue(GameSession.IsCollected("pickup_walljump"), "The collection is remembered");

            // Reloading the level (same save) must not respawn it
            _extra = MakePickup();
            yield return null;
            yield return null;
            Assert.IsTrue(_extra == null, "A collected pickup is removed when the level loads again");
        }
    }
}
