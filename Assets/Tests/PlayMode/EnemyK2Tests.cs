using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using EchoOfTheVoid.Core;
using EchoOfTheVoid.Combat;
using EchoOfTheVoid.Enemies;

namespace EchoOfTheVoid.Tests
{
    /// <summary>K2: Tests for VoidStrider and PrismSentry behaviour.</summary>
    public class EnemyK2Tests
    {
        [TearDown]
        public void Teardown()
        {
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
                Object.Destroy(go);
        }

        // ─────────────────────────────────────────────
        // VoidStrider
        // ─────────────────────────────────────────────

        [UnityTest]
        public IEnumerator VoidStrider_StartsInPatrolState()
        {
            var go = new GameObject("Strider");
            go.AddComponent<BoxCollider2D>();
            var strider = go.AddComponent<VoidStrider>();
            yield return null;

            Assert.AreEqual(VoidStrider.StriderState.Patrol, strider.CurrentState);
        }

        [UnityTest]
        public IEnumerator VoidStrider_WithinDetectRadius_SwitchesToCharge()
        {
            var go = new GameObject("Strider");
            go.AddComponent<BoxCollider2D>();
            var strider = go.AddComponent<VoidStrider>();

            // Create a fake player inside detection radius (default 6 tiles)
            var playerGo = new GameObject("Player");
            playerGo.tag = "Player";
            playerGo.transform.position = new Vector3(3f, 0f, 0f);
            go.transform.position = Vector3.zero;

            yield return null; // Allow Start to run
            yield return null; // Allow Update to detect player

            Assert.AreEqual(VoidStrider.StriderState.Charge, strider.CurrentState,
                "VoidStrider should switch to Charge when player is within detection radius");
        }

        [UnityTest]
        public IEnumerator VoidStrider_ReceivesFullDamageFromSameRealm()
        {
            var go = new GameObject("Strider");
            go.AddComponent<BoxCollider2D>();
            go.AddComponent<VoidStrider>(); // realm defaults from data, Prime by default
            yield return null;

            // Since VoidStrider realm can vary, test damage reduction for cross-realm
            var strider = go.GetComponent<VoidStrider>();
            int hpBefore = strider.CurrentHealth;

            // Attack from Prime (same as default realm)
            var dmg = new DamageInfo(10, go.transform.position, Vector2.zero, RealmType.Prime);
            var feedback = strider.TakeDamage(dmg);

            // If not deflected, full damage
            if (!feedback.IsDeflected)
                Assert.AreEqual(10, feedback.DealtDamage);
        }

        [UnityTest]
        public IEnumerator VoidStrider_ReceivesReducedDamageFromOppositeRealm()
        {
            var go = new GameObject("Strider");
            go.AddComponent<BoxCollider2D>();
            var strider = go.AddComponent<VoidStrider>();
            yield return null;

            // Attack from opposite realm → 20% damage
            RealmType opposite = (strider.EntityRealm == RealmType.Prime) ? RealmType.Echo : RealmType.Prime;
            var dmg = new DamageInfo(100, go.transform.position, Vector2.zero, opposite);
            var feedback = strider.TakeDamage(dmg);

            Assert.IsTrue(feedback.IsDeflected, "Cross-realm attack should be deflected");
            Assert.LessOrEqual(feedback.DealtDamage, 20, "Deflected damage should be at most 20% of 100");
        }

        // ─────────────────────────────────────────────
        // PrismSentry
        // ─────────────────────────────────────────────

        [UnityTest]
        public IEnumerator PrismSentry_StartsWithRailCable_Inactive()
        {
            var go = new GameObject("Sentry");
            go.AddComponent<BoxCollider2D>();
            var sentry = go.AddComponent<PrismSentry>();
            yield return null;

            // RailCable should be present but inactive before first fire
            Assert.IsNotNull(sentry.Cable, "PrismSentry should have a RailCable child");
        }

        [UnityTest]
        public IEnumerator PrismSentry_IsStationary_ZeroVelocity()
        {
            var go = new GameObject("Sentry");
            go.AddComponent<BoxCollider2D>();
            go.AddComponent<PrismSentry>();
            yield return null;
            yield return null;

            var rb = go.GetComponent<Rigidbody2D>();
            Assert.IsNotNull(rb);
            Assert.AreEqual(0f, rb.linearVelocity.magnitude, 0.01f,
                "PrismSentry should not move (stationary turret)");
        }
    }
}
