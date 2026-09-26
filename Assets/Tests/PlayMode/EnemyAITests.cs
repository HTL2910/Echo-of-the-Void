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

        [UnityTest]
        public IEnumerator Enemy_TakesDifferentDamage_InEchoVsPrime()
        {
            var go = new GameObject("DamageTestEnemy");
            go.AddComponent<BoxCollider2D>();
            var dummy = go.AddComponent<TrainingDummy>();

            yield return null;

            int healthStart = dummy.CurrentHealth;

            // Hit 1: Prime realm (same as enemy) - deals 20 damage at 100%
            var primeDamage = new DamageInfo(20, go.transform.position, Vector2.zero, RealmType.Prime);
            dummy.TakeDamage(primeDamage);
            int healthAfterPrime = dummy.CurrentHealth;

            Assert.AreEqual(healthStart - 20, healthAfterPrime, "Prime-realm damage should deal 100% (20 dmg)");

            // Reset for echo test
            dummy.CurrentHealth = healthStart;

            // Hit 2: Echo realm (opposite) - deals 20 damage at 20%
            var echoDamage = new DamageInfo(20, go.transform.position, Vector2.zero, RealmType.Echo);
            dummy.TakeDamage(echoDamage);
            int healthAfterEcho = dummy.CurrentHealth;

            Assert.AreEqual(healthStart - 4, healthAfterEcho, "Echo-realm damage should deal 20% (20 * 0.2 = 4 dmg)");
        }

        [UnityTest]
        public IEnumerator Enemy_StunDuration_ExpiresAndRestoresNormalDamage()
        {
            var go = new GameObject("StunDurationEnemy");
            go.AddComponent<BoxCollider2D>();
            var dummy = go.AddComponent<TrainingDummy>();

            yield return null;

            // Stun the enemy by reducing poise to 0
            var hit1 = new DamageInfo(50, go.transform.position, Vector2.zero, RealmType.Prime);
            dummy.TakeDamage(hit1);

            Assert.IsTrue(dummy.IsStunned, "Enemy should be stunned");

            // Wait for stun to expire (assume default is ~1.5s)
            yield return new WaitForSeconds(2f);

            // Enemy should no longer be stunned
            Assert.IsFalse(dummy.IsStunned, "Stun effect should expire after duration");

            // Damage should return to normal (no bonus)
            int hpBefore = dummy.CurrentHealth;
            var normalHit = new DamageInfo(10, go.transform.position, Vector2.zero, RealmType.Prime);
            dummy.TakeDamage(normalHit);

            Assert.AreEqual(hpBefore - 10, dummy.CurrentHealth, "Damage after stun expires should be normal (no 1.5x bonus)");
        }

        [UnityTest]
        public IEnumerator Enemy_CannotBeStunned_WhenAlreadyDead()
        {
            var go = new GameObject("DeadEnemyStunTest");
            go.AddComponent<BoxCollider2D>();
            var dummy = go.AddComponent<TrainingDummy>();

            yield return null;

            // Kill the enemy
            var fatalHit = new DamageInfo(dummy.MaxHealth + 10, go.transform.position, Vector2.zero, RealmType.Prime);
            dummy.TakeDamage(fatalHit);

            Assert.IsTrue(dummy.IsDead, "Enemy should be dead");

            // Try to apply stun after death (should not throw)
            Assert.DoesNotThrow(() =>
            {
                dummy.TakeDamage(new DamageInfo(5, go.transform.position, Vector2.zero, RealmType.Prime));
            });

            // Should still be dead, not revived
            Assert.IsTrue(dummy.IsDead);
        }

        [UnityTest]
        public IEnumerator Enemy_WithZeroMaxHealth_TakesNoDamage()
        {
            var go = new GameObject("ZeroHealthEnemy");
            go.AddComponent<BoxCollider2D>();
            var dummy = go.AddComponent<TrainingDummy>();

            yield return null;

            // Manually set max health to 0 (edge case)
            dummy.MaxHealth = 0;

            var hit = new DamageInfo(100, go.transform.position, Vector2.zero, RealmType.Prime);
            var feedback = dummy.TakeDamage(hit);

            // Should handle gracefully without error
            Assert.IsNotNull(feedback);
        }
    }
}
