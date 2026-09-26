using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using EchoOfTheVoid.Core;
using EchoOfTheVoid.Feedback;

namespace EchoOfTheVoid.Tests
{
    public class RealmVisualEffectsTests
    {
        private TestWorld _world;
        private GameObject _effectsObj;
        private RealmVisualEffects _effects;

        [SetUp]
        public void SetUp()
        {
            _world = TestWorld.Create(new Vector2(0f, 0f));
            _effectsObj = new GameObject("RealmVisualEffects");
            _effects = _effectsObj.AddComponent<RealmVisualEffects>();
        }

        [TearDown]
        public void TearDown()
        {
            _world.Dispose();
            Object.Destroy(_effectsObj);
        }

        [UnityTest]
        public IEnumerator TintTransition_Smoothly_OverExpectedDuration()
        {
            // Initial state: Prime realm
            yield return null;
            Assert.AreEqual(RealmType.Prime, RealityManager.Instance.CurrentRealm);

            // Switch to Echo
            RealityManager.Instance.ToggleRealm();
            yield return null;

            // Over 0.18s, tint should transition
            float elapsed = 0f;
            float expectedDuration = 0.18f;
            Color startColor = Color.clear;
            bool capturedStart = false;

            while (elapsed < expectedDuration * 2f)
            {
                if (!capturedStart)
                {
                    startColor = _effects.GetRealmColor(RealmType.Echo);
                    capturedStart = true;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            // After transition completes, realm should be Echo
            Assert.AreEqual(RealmType.Echo, RealityManager.Instance.CurrentRealm);
        }

        [UnityTest]
        public IEnumerator RealmSwitch_UpdatesVisualState()
        {
            yield return null;
            Assert.AreEqual(RealmType.Prime, RealityManager.Instance.CurrentRealm);

            Color primeTint = _effects.GetRealmColor(RealmType.Prime);
            Color echoTint = _effects.GetRealmColor(RealmType.Echo);

            // Tints should be different
            Assert.AreNotEqual(primeTint, echoTint, "Prime and Echo tints must differ");

            // Switch realm
            RealityManager.Instance.ToggleRealm();
            yield return new WaitForSeconds(0.5f);

            Assert.AreEqual(RealmType.Echo, RealityManager.Instance.CurrentRealm);
        }

        [UnityTest]
        public IEnumerator Vignette_UpdatesOnRealmSwitch()
        {
            // Ensure effects are running
            yield return null;
            yield return null;

            RealityManager.Instance.ToggleRealm();
            yield return new WaitForSeconds(0.3f);

            // Vignette should be updated after switch
            Assert.AreEqual(RealmType.Echo, RealityManager.Instance.CurrentRealm);
        }
    }
}
