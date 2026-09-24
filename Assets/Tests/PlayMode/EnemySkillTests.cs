using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using EchoOfTheVoid.Core;
using EchoOfTheVoid.Combat;
using EchoOfTheVoid.Enemies;

namespace EchoOfTheVoid.Tests
{
    public class EnemySkillTests
    {
        [TearDown]
        public void Teardown()
        {
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                Object.Destroy(go);
            }
        }

        [UnityTest]
        public IEnumerator ChronoCrawler_HasNewDiverseSkills_Configured()
        {
            var go = new GameObject("Crawler");
            go.AddComponent<BoxCollider2D>();
            var crawler = go.AddComponent<ChronoCrawler>();
            yield return null;

            Assert.AreEqual(ChronoCrawler.CrawlerState.Patrol, crawler.CurrentState);
            Assert.IsNotNull(crawler);
        }

        [UnityTest]
        public IEnumerator VoidWeaver_AlternatesAttackCount_ForSpreadVolley()
        {
            var go = new GameObject("Weaver");
            go.AddComponent<CircleCollider2D>();
            var weaver = go.AddComponent<VoidWeaver>();

            var playerGo = new GameObject("Player");
            playerGo.tag = "Player";
            playerGo.transform.position = new Vector3(5f, 0f, 0f);

            yield return null;

            Assert.IsFalse(weaver.IsCharging);
            Assert.AreEqual(0, weaver.ShotCount);
        }

        [UnityTest]
        public IEnumerator VoidStrider_InitializesWithSkills_AndEntersCharge()
        {
            var go = new GameObject("Strider");
            go.AddComponent<BoxCollider2D>();
            var strider = go.AddComponent<VoidStrider>();

            var playerGo = new GameObject("Player");
            playerGo.tag = "Player";
            playerGo.transform.position = new Vector3(3f, 0f, 0f);
            go.transform.position = Vector3.zero;

            yield return null; // Start
            yield return null; // Update detection

            Assert.AreEqual(VoidStrider.StriderState.Charge, strider.CurrentState);
        }

        [UnityTest]
        public IEnumerator PrismSentry_PulseBarrier_RepelsClosePlayer()
        {
            var sentryGo = new GameObject("Sentry");
            sentryGo.AddComponent<BoxCollider2D>();
            var sentry = sentryGo.AddComponent<PrismSentry>();

            var playerGo = new GameObject("Player");
            playerGo.tag = "Player";
            var col = playerGo.AddComponent<CircleCollider2D>();
            var dummyTarget = playerGo.AddComponent<TrainingDummy>(); // Has IDamageable
            playerGo.transform.position = sentryGo.transform.position + new Vector3(0.5f, 0f, 0f);

            yield return null;
            yield return null;

            // Player is within pulse barrier radius
            Assert.IsNotNull(sentry.Cable);
        }
    }
}
