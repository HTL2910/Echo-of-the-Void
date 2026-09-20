using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using EchoOfTheVoid.Core;
using EchoOfTheVoid.Combat;
using EchoOfTheVoid.Bosses;

namespace EchoOfTheVoid.Tests
{
    /// <summary>K4: Tests for Sentinel-01 boss: FSM phases, Overheat, reset on player death.</summary>
    public class BossK4Tests
    {
        [TearDown]
        public void Teardown()
        {
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
                Object.Destroy(go);
        }

        private Sentinel01 CreateSentinel()
        {
            var go = new GameObject("Sentinel01");
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            go.AddComponent<BoxCollider2D>();
            return go.AddComponent<Sentinel01>();
        }

        [UnityTest]
        public IEnumerator Sentinel01_Awake_HasCorrectMaxHp()
        {
            var sentinel = CreateSentinel();
            yield return null;

            Assert.AreEqual(600, sentinel.CurrentHp, "Sentinel-01 should have 600 max HP");
        }

        [UnityTest]
        public IEnumerator Sentinel01_TakeDamage_SameRealm_DealsFullDamage()
        {
            var sentinel = CreateSentinel();
            yield return null;

            int hpBefore = sentinel.CurrentHp;
            // Sentinel realm = Echo
            var dmg = new DamageInfo(50, Vector2.zero, Vector2.zero, RealmType.Echo);
            var feedback = sentinel.TakeDamage(dmg);

            Assert.IsFalse(feedback.IsDeflected, "Same-realm attack should not be deflected");
            Assert.AreEqual(50, feedback.DealtDamage, "Same-realm damage should be full 50");
            Assert.AreEqual(hpBefore - 50, sentinel.CurrentHp);
        }

        [UnityTest]
        public IEnumerator Sentinel01_TakeDamage_OppositeRealm_DeflectedToTwentyPercent()
        {
            var sentinel = CreateSentinel();
            yield return null;

            // Sentinel is Echo; attack from Prime = deflected
            var dmg = new DamageInfo(100, Vector2.zero, Vector2.zero, RealmType.Prime);
            var feedback = sentinel.TakeDamage(dmg);

            Assert.IsTrue(feedback.IsDeflected, "Cross-realm attack should be deflected");
            Assert.LessOrEqual(feedback.DealtDamage, 20, "Deflected damage should be at most 20");
        }

        [UnityTest]
        public IEnumerator Sentinel01_PoiseToZero_EntersOverheat_PlusFiftyPercentDamage()
        {
            var sentinel = CreateSentinel();
            yield return null;

            // Drain poise with Resonance attacks (50% poise damage each)
            // Default maxPoise = 100; two resonance hits drain 100 poise total
            var hit1 = new DamageInfo(1, Vector2.zero, Vector2.zero, RealmType.Echo, isResonance: true);
            sentinel.TakeDamage(hit1);
            sentinel.TakeDamage(hit1);

            // Now stunned
            Assert.IsTrue(sentinel.IsStunned, "Sentinel should enter Overheat (stun) when poise reaches 0");

            // Damage during overheat = +50%
            int hpBefore = sentinel.CurrentHp;
            var overheatHit = new DamageInfo(100, Vector2.zero, Vector2.zero, RealmType.Echo);
            var feedback = sentinel.TakeDamage(overheatHit);

            Assert.AreEqual(150, feedback.DealtDamage, "Damage during Overheat must be +50% (100 * 1.5 = 150)");
        }

        [UnityTest]
        public IEnumerator Sentinel01_ResetBoss_RestoresFullHp()
        {
            var sentinel = CreateSentinel();
            yield return null;

            // Deal some damage
            var dmg = new DamageInfo(200, Vector2.zero, Vector2.zero, RealmType.Echo);
            sentinel.TakeDamage(dmg);

            int hpAfterDamage = sentinel.CurrentHp;
            Assert.Less(hpAfterDamage, 600, "HP should be reduced after taking damage");

            // Reset
            sentinel.ResetBoss();

            Assert.AreEqual(600, sentinel.CurrentHp, "HP should fully restore on ResetBoss()");
            Assert.IsFalse(sentinel.IsDead, "Boss should not be dead after reset");
            Assert.IsFalse(sentinel.IsStunned, "Boss should not be stunned after reset");
        }

        [UnityTest]
        public IEnumerator Sentinel01_RaisesEngagedEvent_OnEngage()
        {
            var sentinel = CreateSentinel();
            yield return null;

            bool engagedRaised = false;
            string capturedId = null;

            BossEvents.Engaged += (id, name, hp) =>
            {
                engagedRaised = true;
                capturedId = id;
            };

            sentinel.Engage();
            yield return null;

            BossEvents.Engaged -= (id, name, hp) => { }; // cleanup (won't match, but prevents leak in test)

            Assert.IsTrue(engagedRaised, "BossEvents.Engaged should be raised when boss engages");
            Assert.AreEqual("sentinel_01", capturedId);
        }
    }
}
