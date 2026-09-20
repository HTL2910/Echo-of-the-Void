using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using EchoOfTheVoid.Core;
using EchoOfTheVoid.Environment;
using EchoOfTheVoid.Feedback;
using EchoOfTheVoid.Save;

namespace EchoOfTheVoid.Tests
{
    public class RoomTests
    {
        private TestWorld _world;
        private GameObject _cameraGo, _managerGo, _roomA, _roomB, _shakeGo;
        private Camera _camera;
        private CameraFollow2D _follow;

        [SetUp]
        public void SetUp()
        {
            GameSession.Reset();
            _world = TestWorld.Create(new Vector2(-3f, 1.5f));

            _cameraGo = new GameObject("TestCamera");
            _camera = _cameraGo.AddComponent<Camera>();
            _camera.orthographic = true;
            _camera.orthographicSize = 4f;
            _camera.aspect = 16f / 9f;
            _cameraGo.transform.position = new Vector3(0f, 2f, -10f);
            _follow = _cameraGo.AddComponent<CameraFollow2D>();
            _follow.SetTarget(_world.Player.transform);

            _managerGo = new GameObject("RoomManager");
            _managerGo.AddComponent<RoomManager>();

            _roomA = MakeRoom("A", new Vector2(-15f, 2f), new Vector2(30f, 12f)); // x -30..0
            _roomB = MakeRoom("B", new Vector2(15f, 2f), new Vector2(30f, 12f));  // x 0..30
        }

        [TearDown]
        public void TearDown()
        {
            _world.Dispose();
            foreach (var go in new[] { _cameraGo, _managerGo, _roomA, _roomB, _shakeGo })
                if (go != null) Object.Destroy(go);
            if (ScreenFader.Instance != null) Object.Destroy(ScreenFader.Instance.gameObject);
            GameSession.Reset();
        }

        private static GameObject MakeRoom(string id, Vector2 center, Vector2 size)
        {
            var go = new GameObject("Room_" + id);
            go.transform.position = center;
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = size;
            go.AddComponent<RoomBounds>().Configure(id);
            return go;
        }

        [UnityTest]
        public IEnumerator EnteringANewRoom_ConfinesTheCamera_FlashesAndBrieflyLocksInput()
        {
            string from = null, to = null;
            bool lockedInEvent = false;
            float alphaInEvent = -1f;
            int enteredCount = 0;
            System.Action<string, string> onEntered = (a, b) =>
            {
                enteredCount++;
                from = a; to = b;
                lockedInEvent = _world.Controller.IsInputLocked;
                alphaInEvent = ScreenFader.Instance != null ? ScreenFader.Instance.Alpha : -1f;
            };
            RoomManager.RoomEntered += onEntered;
            try
            {
                yield return new WaitForSeconds(0.6f); // Kael lands in room A
                Assert.AreEqual("A", RoomManager.Instance.CurrentRoom.RoomId);
                Assert.IsFalse(_world.Controller.IsInputLocked, "The first room does not freeze input");
                Assert.IsTrue(ScreenFader.Instance == null || ScreenFader.Instance.Alpha < 0.01f, "...or flash");
                Assert.That(_follow.MaxBounds.x, Is.EqualTo(0f).Within(0.01f), "Camera confined to room A");
                CollectionAssert.Contains(GameSession.Current.visitedRooms, "A");
                enteredCount = 0; // only the border crossing is counted below

                _world.Input.Move = 1f;
                yield return new WaitForSeconds(0.6f); // runs across the border at x = 0
                _world.Input.Move = 0f;

                Assert.AreEqual(1, enteredCount, "Exactly one transition");
                Assert.AreEqual("A", from);
                Assert.AreEqual("B", to);
                Assert.IsTrue(lockedInEvent, "Input is frozen for a moment at the border");
                Assert.Greater(alphaInEvent, 0.5f, "The screen flashes");
                Assert.That(_follow.MinBounds.x, Is.EqualTo(0f).Within(0.01f), "Camera now confined to room B");
                Assert.That(_follow.MaxBounds.x, Is.EqualTo(30f).Within(0.01f));
                CollectionAssert.AreEquivalent(new[] { "A", "B" }, GameSession.Current.visitedRooms);

                yield return new WaitForSeconds(0.4f);
                Assert.IsFalse(_world.Controller.IsInputLocked, "The lock is brief");
                Assert.That(ScreenFader.Instance.Alpha, Is.EqualTo(0f).Within(0.01f), "The flash fades out");
            }
            finally
            {
                RoomManager.RoomEntered -= onEntered;
            }
        }

        [UnityTest]
        public IEnumerator FrozenInput_IsReallyIgnored()
        {
            yield return new WaitForSeconds(0.6f);
            float x0 = _world.Player.transform.position.x;

            _world.Controller.LockInput(0.3f);
            _world.Input.Move = 1f;
            yield return new WaitForSeconds(0.2f);
            Assert.That(_world.Player.transform.position.x, Is.EqualTo(x0).Within(0.05f), "No movement while locked");

            yield return new WaitForSeconds(0.4f);
            Assert.Greater(_world.Player.transform.position.x, x0 + 1f, "Movement resumes after the lock");
            _world.Input.Move = 0f;
        }

        [UnityTest]
        public IEnumerator ARoomSmallerThanTheView_IsCentred_NotJittered()
        {
            _follow.SetBounds(new Vector2(-2f, 0f), new Vector2(2f, 3f)); // 4 wide, the view is ~14 wide
            _follow.SnapToTarget();
            yield return new WaitForSeconds(0.3f);

            Assert.That(_cameraGo.transform.position.x, Is.EqualTo(0f).Within(0.01f), "Centred horizontally");
            Assert.That(_cameraGo.transform.position.y, Is.EqualTo(1.5f).Within(0.01f), "Centred vertically");
        }

        [UnityTest]
        public IEnumerator CameraShake_DoesNotMakeTheCameraDrift()
        {
            _shakeGo = new GameObject("Shake");
            var shake = _shakeGo.AddComponent<CameraShakeManager>();
            yield return new WaitForSeconds(0.6f);
            _follow.SnapToTarget();
            yield return null;
            Vector3 before = _cameraGo.transform.position;

            shake.ShakeHeavy();
            yield return new WaitForSeconds(0.8f); // shake (0.4 s) is over
            Vector3 after = _cameraGo.transform.position;

            Assert.That(Vector3.Distance(before, after), Is.LessThan(0.05f), "Back where it was: shake must not accumulate");
        }
    }
}
