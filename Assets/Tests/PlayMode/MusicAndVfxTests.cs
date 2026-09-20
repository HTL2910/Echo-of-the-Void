using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using EchoOfTheVoid.Core;
using EchoOfTheVoid.Enemies;
using EchoOfTheVoid.Feedback;

namespace EchoOfTheVoid.Tests
{
    public class MusicLayerTests
    {
        private GameObject _go;
        private MusicLayerController _music;

        private static AudioClip Synth(string name) => AudioClip.Create(name, 44100, 1, 44100, false);

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("Music");
            _music = _go.AddComponent<MusicLayerController>();
            _music.Configure(Synth("prime"), Synth("echo"));
            RealityEventBus.TriggerRealmSwitch(RealmType.Prime);
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            Object.Destroy(_go);
        }

        [UnityTest]
        public IEnumerator StartsOnThePrimeStem()
        {
            yield return null;
            Assert.That(_music.PrimeWeight, Is.EqualTo(1f).Within(0.001f));
            Assert.That(_music.EchoWeight, Is.EqualTo(0f).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator Crossfade_KeepsConstantPower_AndLandsOnEcho()
        {
            yield return null;
            RealityEventBus.TriggerRealmSwitch(RealmType.Echo);

            float worstPowerError = 0f;
            float midEcho = -1f;
            float elapsed = 0f;
            while (elapsed < 0.4f)
            {
                yield return null;
                elapsed += Time.unscaledDeltaTime;
                float power = _music.PrimeWeight * _music.PrimeWeight + _music.EchoWeight * _music.EchoWeight;
                worstPowerError = Mathf.Max(worstPowerError, Mathf.Abs(power - 1f));
                if (midEcho < 0f && _music.EchoWeight > 0.05f && _music.EchoWeight < 0.95f) midEcho = _music.EchoWeight;
            }

            Assert.Less(worstPowerError, 0.01f, "Equal-power: loudness must not dip during the fade (spec 7.1)");
            Assert.That(_music.EchoWeight, Is.EqualTo(1f).Within(0.001f));
            Assert.That(_music.PrimeWeight, Is.EqualTo(0f).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator Crossfade_ContinuesDuringHitstop()
        {
            yield return null;
            Time.timeScale = 0f; // hitstop freeze
            RealityEventBus.TriggerRealmSwitch(RealmType.Echo);
            yield return new WaitForSecondsRealtime(0.4f);

            Assert.That(_music.EchoWeight, Is.EqualTo(1f).Within(0.001f), "The fade runs on unscaled time");
        }

        [UnityTest]
        public IEnumerator BackToPrime_FadesBack()
        {
            yield return null;
            RealityEventBus.TriggerRealmSwitch(RealmType.Echo);
            yield return new WaitForSecondsRealtime(0.4f);
            RealityEventBus.TriggerRealmSwitch(RealmType.Prime);
            yield return new WaitForSecondsRealtime(0.4f);

            Assert.That(_music.PrimeWeight, Is.EqualTo(1f).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator WithoutStems_StaysSilentAndDoesNotThrow()
        {
            Object.Destroy(_go);
            yield return null;
            _go = new GameObject("MusicNoClips");
            _music = _go.AddComponent<MusicLayerController>();
            yield return null;

            RealityEventBus.TriggerRealmSwitch(RealmType.Echo);
            yield return new WaitForSecondsRealtime(0.3f);

            Assert.IsFalse(_music.HasStems);
        }
    }

    public class VfxTests
    {
        private GameObject _libraryGo;
        private VfxLibrary _library;
        private GameObject _prefab;
        private TestWorld _world;

        [SetUp]
        public void SetUp()
        {
            _prefab = new GameObject("VfxPrefab");
            _prefab.SetActive(false); // acts as a prefab: only the clones are live
            _libraryGo = new GameObject("VfxLibrary");
            _library = _libraryGo.AddComponent<VfxLibrary>();
        }

        [TearDown]
        public void TearDown()
        {
            _world?.Dispose();
            _world = null;
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (go.name.StartsWith("VfxPrefab") || go.name.StartsWith("TestDummy")) Object.Destroy(go);
            }
            Object.Destroy(_libraryGo);
        }

        private static List<GameObject> Clones()
        {
            var list = new List<GameObject>();
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (go.name == "VfxPrefab(Clone)") list.Add(go);
            return list;
        }

        [UnityTest]
        public IEnumerator Play_SpawnsTheConfiguredPrefab_AtThePosition_AndFlips()
        {
            _library.Configure(VfxId.ImpactClean, _prefab);
            Assert.IsTrue(VfxLibrary.Has(VfxId.ImpactClean));
            Assert.IsFalse(VfxLibrary.Has(VfxId.EnemyDeath));

            VfxLibrary.Play(VfxId.ImpactClean, new Vector3(3f, 2f, 0f), flipX: true);
            yield return null;

            var clones = Clones();
            Assert.AreEqual(1, clones.Count);
            Assert.AreEqual(new Vector3(3f, 2f, 0f), clones[0].transform.position);
            Assert.Less(clones[0].transform.localScale.x, 0f, "flipX mirrors the effect");
        }

        [UnityTest]
        public IEnumerator Play_WithNoPrefab_IsASilentNoOp()
        {
            VfxLibrary.Play(VfxId.EnemyDeath, Vector3.zero);
            yield return null;
            Assert.AreEqual(0, Clones().Count);
        }

        [UnityTest]
        public IEnumerator ShiftWave_OnlyOnARealChange_AtKaelsPosition()
        {
            _library.Configure(VfxId.ShiftWave, _prefab);
            _world = TestWorld.Create(new Vector2(4f, 1.5f));
            yield return new WaitForSeconds(0.2f);

            RealityEventBus.TriggerRealmSwitch(RealmType.Prime); // same realm as the start: the start-up broadcast
            yield return null;
            Assert.AreEqual(0, Clones().Count, "No shockwave for a non-change");

            RealityEventBus.TriggerRealmSwitch(RealmType.Echo);
            yield return null;
            var clones = Clones();
            Assert.AreEqual(1, clones.Count);
            Assert.That(clones[0].transform.position.x, Is.EqualTo(4f).Within(0.5f), "Centred on Kael");
        }

        [UnityTest]
        public IEnumerator DashAndLanding_SpawnDust()
        {
            _library.Configure(VfxId.DashDust, _prefab);
            _library.Configure(VfxId.LandDust, _prefab);
            _world = TestWorld.Create(new Vector2(0f, 3f));
            yield return new WaitForSeconds(0.8f);
            Assert.AreEqual(1, Clones().Count, "Landing puff");

            _world.Input.PressDash();
            yield return new WaitForSeconds(0.1f);
            Assert.AreEqual(2, Clones().Count, "Dash puff");
        }

        [UnityTest]
        public IEnumerator HittingAnEnemy_SpawnsAnImpact()
        {
            _library.Configure(VfxId.ImpactClean, _prefab);
            _library.Configure(VfxId.ImpactDeflect, _prefab);
            _world = TestWorld.Create(new Vector2(0f, 1.5f));

            var dummy = new GameObject("TestDummy");
            dummy.layer = LayerMask.NameToLayer("Enemy");
            dummy.transform.position = new Vector2(1.6f, 1.0f);
            dummy.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            dummy.AddComponent<BoxCollider2D>().size = new Vector2(1f, 2f);
            dummy.AddComponent<TrainingDummy>();

            yield return new WaitForSeconds(0.6f);
            _world.Input.PressAttack();
            yield return new WaitForSeconds(0.2f);

            Assert.GreaterOrEqual(Clones().Count, 1, "A hit must spawn an impact effect");
        }
    }
}
