using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using EchoOfTheVoid.Core;
using EchoOfTheVoid.Combat;
using EchoOfTheVoid.Enemies;
using EchoOfTheVoid.Environment;
using EchoOfTheVoid.Save;

namespace EchoOfTheVoid.Tests
{
    public class EnemyAITests
    {
        private string _tempSaveDir;

        [SetUp]
        public void Setup()
        {
            _tempSaveDir = Path.Combine(Path.GetTempPath(), "eotv_ai_test_" + System.Guid.NewGuid().ToString("N"));
            SaveService.DirectoryOverride = _tempSaveDir;
            GameSession.Reset();
        }

        [TearDown]
        public void Teardown()
        {
            SaveService.DirectoryOverride = null;
            if (Directory.Exists(_tempSaveDir))
            {
                Directory.Delete(_tempSaveDir, true);
            }

            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                Object.Destroy(go);
            }
        }

        [UnityTest]
        public IEnumerator EnemyAnimationDriver_WithoutAnimator_DoesNotThrow()
        {
            var go = new GameObject("TestEnemy");
            go.AddComponent<BoxCollider2D>();
            var crawler = go.AddComponent<ChronoCrawler>();
            var driver = go.AddComponent<EnemyAnimationDriver>();

            yield return null;

            Assert.DoesNotThrow(() =>
            {
                driver.SetMovement(5f, true);
                driver.TriggerEnemyAlert();
                driver.TriggerEnemyCharge();
                driver.TriggerEnemyAttack();
                driver.TriggerEnemyHurt();
                driver.TriggerEnemyDie();
            });

            Assert.IsNull(driver.AnimatorComponent);
        }

        [UnityTest]
        public IEnumerator EnemyPoise_ReachingZero_EntersStun_AndTakes50PercentBonusDamage()
        {
            var go = new GameObject("StunTestDummy");
            go.AddComponent<BoxCollider2D>();
            var dummy = go.AddComponent<TrainingDummy>();

            yield return null; // Wait for Awake & Start

            // TrainingDummy starts with MaxPoise = 50, MaxHealth = 50, Realm = Prime
            Assert.AreEqual(50f, dummy.MaxPoise);
            Assert.IsFalse(dummy.IsStunned);

            // Hit 1 with Prime (same realm): deals 20 dmg, 30 poise dmg -> remaining poise = 20
            var hit1 = new DamageInfo(20, go.transform.position, Vector2.zero, RealmType.Prime);
            dummy.TakeDamage(hit1);

            Assert.IsFalse(dummy.IsStunned);
            Assert.AreEqual(20f, dummy.CurrentPoise);

            // Hit 2 with Prime: deals 20 dmg, 30 poise dmg -> poise drops <= 0 -> Stunned!
            var hit2 = new DamageInfo(20, go.transform.position, Vector2.zero, RealmType.Prime);
            dummy.TakeDamage(hit2);

            Assert.IsTrue(dummy.IsStunned, "Enemy should enter Stun when Poise reaches 0");

            // Hit 3 while stunned: base damage 10 -> with +50% bonus -> dealtDamage should be 15
            int hpBefore = dummy.CurrentHealth;
            var hit3 = new DamageInfo(10, go.transform.position, Vector2.zero, RealmType.Prime);
            var feedback = dummy.TakeDamage(hit3);

            Assert.AreEqual(15, feedback.DealtDamage, "Dealt damage during Stun must be increased by +50% (10 * 1.5 = 15)");
            Assert.AreEqual(hpBefore - 15, dummy.CurrentHealth);
        }

        [UnityTest]
        public IEnumerator EnemyRespawner_OnChronoStationUsed_RestoresHealthAndPosition()
        {
            var world = TestWorld.Create(new Vector2(0f, 1f));

            // Setup station
            var stationGo = new GameObject("Station");
            stationGo.transform.position = new Vector3(0f, 1f, 0f);
            var col = stationGo.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(3f, 3f);
            var station = stationGo.AddComponent<ChronoStation>();
            station.Configure("test_station", 25);

            // Setup enemy with respawner
            var enemyGo = new GameObject("RespawnEnemy");
            enemyGo.transform.position = new Vector3(15f, 1f, 0f);
            enemyGo.AddComponent<BoxCollider2D>();
            var dummy = enemyGo.AddComponent<TrainingDummy>();
            var respawner = enemyGo.AddComponent<EnemyRespawner>();

            yield return null;

            Assert.AreEqual(new Vector3(15f, 1f, 0f), respawner.SpawnPosition);

            // Move enemy and damage it to death
            enemyGo.transform.position = new Vector3(25f, 10f, 0f);
            var fatalHit = new DamageInfo(100, enemyGo.transform.position, Vector2.zero, RealmType.Prime);
            dummy.TakeDamage(fatalHit);

            yield return new WaitForSeconds(0.35f);

            // Trigger station via Activate
            station.Activate(world.Controller);

            yield return null;

            Assert.IsTrue(enemyGo.activeSelf, "Enemy GameObject should be re-enabled after station used");
            Assert.AreEqual(15f, enemyGo.transform.position.x, 0.01f, "Enemy should return to spawn position X");
            Assert.AreEqual(1f, enemyGo.transform.position.y, 0.01f, "Enemy should return to spawn position Y");
            Assert.IsFalse(dummy.IsDead, "Enemy should not be dead after respawn");
            Assert.AreEqual(dummy.MaxPoise, dummy.CurrentPoise, "Enemy poise should be restored to max");
            Assert.Greater(dummy.CurrentHealth, 0, "Enemy health should be restored");
        }
    }
}
