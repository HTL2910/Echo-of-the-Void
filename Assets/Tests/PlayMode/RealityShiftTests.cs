using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using EchoOfTheVoid.Core;
using EchoOfTheVoid.Environment;

namespace EchoOfTheVoid.Tests
{
    /// <summary>Reality Shift must never embed Kael in solid geometry (spec 2.2).</summary>
    public class RealityShiftTests
    {
        private TestWorld _world;
        private GameObject _manager;
        private GameObject _platform;
        private int _deniedCount;

        private void OnDenied() => _deniedCount++;

        [SetUp]
        public void SetUp()
        {
            _deniedCount = 0;
            RealityEventBus.OnShiftDenied += OnDenied;

            _manager = new GameObject("RealityManager");
            _manager.AddComponent<RealityManager>();
            _world = TestWorld.Create(new Vector2(0f, 1.5f));
        }

        [TearDown]
        public void TearDown()
        {
            RealityEventBus.OnShiftDenied -= OnDenied;
            _world.Dispose();
            if (_platform != null) Object.Destroy(_platform);
            Object.Destroy(_manager);
        }

        /// <summary>An Echo-only platform (invisible to physics while in Prime) centred at <paramref name="center"/>.</summary>
        private void CreateEchoPlatform(Vector2 center, Vector2 size)
        {
            _platform = new GameObject("EchoPlatform");
            _platform.layer = _world.NeutralLayer;
            _platform.transform.position = center;
            _platform.AddComponent<BoxCollider2D>().size = size;
            _platform.AddComponent<SpriteRenderer>();
            _platform.AddComponent<RealityPlatform>().Configure(RealmType.Echo, Color.white);
        }

        private IEnumerator Land()
        {
            yield return new WaitForSeconds(0.6f);
        }

        [UnityTest]
        public IEnumerator Shift_IsDenied_WhenTheTargetRealmWouldEmbedKael()
        {
            CreateEchoPlatform(new Vector2(0f, 0.8f), new Vector2(4f, 2f)); // wraps Kael's body
            yield return Land();
            Assert.AreEqual(RealmType.Prime, RealityManager.Instance.CurrentRealm);

            _world.Input.PressShift();
            yield return null;
            yield return null;

            Assert.AreEqual(RealmType.Prime, RealityManager.Instance.CurrentRealm, "Shift must be refused");
            Assert.AreEqual(1, _deniedCount, "A denial event must fire so the HUD/audio can react");
        }

        [UnityTest]
        public IEnumerator DeniedShift_CostsNoCooldown_SoItWorksOnceKaelStepsAway()
        {
            CreateEchoPlatform(new Vector2(0f, 0.8f), new Vector2(4f, 2f));
            yield return Land();

            _world.Input.PressShift();
            yield return null;
            Assert.AreEqual(1, _deniedCount);

            _world.Input.Move = 1f;
            yield return new WaitForSeconds(0.6f); // runs clear of the platform (x > 2.5)
            _world.Input.Move = 0f;

            _world.Input.PressShift();
            yield return null;
            yield return null;

            Assert.AreEqual(RealmType.Echo, RealityManager.Instance.CurrentRealm, "Shift works once the way is clear");
        }

        [UnityTest]
        public IEnumerator Shift_IsAllowed_WhenAnEchoPlatformOnlyTouchesKaelsFeet()
        {
            // Top of the platform is exactly Kael's foot level: shifting makes it solid under him, not around him
            CreateEchoPlatform(new Vector2(0f, -0.25f), new Vector2(4f, 0.5f));
            yield return Land();

            _world.Input.PressShift();
            yield return null;
            yield return null;

            Assert.AreEqual(RealmType.Echo, RealityManager.Instance.CurrentRealm);
            Assert.AreEqual(0, _deniedCount);
        }

        [UnityTest]
        public IEnumerator Shift_BackToPrime_IsAlsoGuarded()
        {
            yield return Land();
            _world.Input.PressShift();          // Prime -> Echo, nothing in the way
            yield return new WaitForSeconds(0.4f); // clear the cooldown
            Assert.AreEqual(RealmType.Echo, RealityManager.Instance.CurrentRealm);

            var primeWall = new GameObject("PrimePlatform");
            primeWall.layer = _world.NeutralLayer;
            primeWall.transform.position = _world.Player.transform.position;
            primeWall.AddComponent<BoxCollider2D>().size = new Vector2(3f, 3f);
            primeWall.AddComponent<SpriteRenderer>();
            primeWall.AddComponent<RealityPlatform>().Configure(RealmType.Prime, Color.white);
            _platform = primeWall;
            yield return null;

            _world.Input.PressShift();
            yield return null;
            yield return null;

            Assert.AreEqual(RealmType.Echo, RealityManager.Instance.CurrentRealm, "Must stay in Echo: Prime is solid around Kael");
            Assert.AreEqual(1, _deniedCount);
        }
    }
}
