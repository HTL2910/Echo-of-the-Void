using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using EchoOfTheVoid.Combat;
using EchoOfTheVoid.Core;
using EchoOfTheVoid.Player;

namespace EchoOfTheVoid.Tests
{
    /// <summary>
    /// Verifies the Kael controller against echo_of_the_void_master_spec.md (section 3.2, 3.3).
    /// Input is injected through <see cref="IPlayerInput"/>, so the tests do not depend on real devices.
    /// </summary>
    public class PlayerMovementTests
    {
        private const float FloorTop = 0f;

        private FakeInput _input;
        private GameObject _floor;
        private GameObject _player;
        private PlayerController _controller;
        private PlayerStats _stats;
        private Rigidbody2D _rb;
        private int _neutralLayer;

        [SetUp]
        public void SetUp()
        {
            _neutralLayer = LayerMask.NameToLayer("Neutral");
            Assert.GreaterOrEqual(_neutralLayer, 0, "Layer 'Neutral' must exist (spec 2.2)");

            _input = new FakeInput();
            _floor = CreateBox("Floor", new Vector2(0f, FloorTop - 0.5f), new Vector2(60f, 1f));
            _player = CreatePlayer(new Vector2(0f, 1.5f));
        }

        [TearDown]
        public void TearDown()
        {
            if (_player != null) Object.DestroyImmediate(_player);
            if (_floor != null) Object.DestroyImmediate(_floor);
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (go.name.StartsWith("TestBox")) Object.DestroyImmediate(go);
            }
            Time.timeScale = 1f;
        }

        private GameObject CreateBox(string name, Vector2 center, Vector2 size)
        {
            var go = new GameObject(name == "Floor" ? name : "TestBox_" + name);
            go.layer = _neutralLayer;
            go.transform.position = center;
            go.AddComponent<BoxCollider2D>().size = size;
            return go;
        }

        private GameObject CreatePlayer(Vector2 position)
        {
            // Build inactive so every Awake sees the full component set (same as loading a scene)
            var go = new GameObject("Player");
            go.SetActive(false);
            go.tag = "Player";
            go.layer = LayerMask.NameToLayer("Player");
            go.transform.position = position;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.freezeRotation = true;
            rb.gravityScale = 0f;

            go.AddComponent<BoxCollider2D>().size = new Vector2(0.875f, 1.625f);

            var visual = new GameObject("Visual");
            visual.transform.SetParent(go.transform, false);
            visual.AddComponent<SpriteRenderer>();

            go.AddComponent<PlayerStats>();
            go.AddComponent<PlayerCombat>();
            go.AddComponent<PlayerRespawn>(); // brings PlayerController via RequireComponent
            var controller = go.GetComponent<PlayerController>();
            controller.SetGroundLayer(1 << _neutralLayer);
            controller.SetInput(_input);

            go.SetActive(true);

            _controller = controller;
            _stats = go.GetComponent<PlayerStats>();
            _rb = rb;
            return go;
        }

        private IEnumerator WaitUntilGrounded(float timeout)
        {
            float t = 0f;
            while (!_controller.IsGrounded && t < timeout)
            {
                t += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }
        }

        private IEnumerator Settle()
        {
            yield return WaitUntilGrounded(2f);
            yield return new WaitForSeconds(0.15f);
        }

        [UnityTest]
        public IEnumerator Kael_LandsOnFloor_AndIsGrounded()
        {
            yield return WaitUntilGrounded(2f);
            Assert.IsTrue(_controller.IsGrounded,
                $"Kael should be grounded after falling onto the floor. pos={_player.transform.position} vel={_rb.linearVelocity} state={_controller.CurrentStateName} bounds={_player.GetComponent<BoxCollider2D>().bounds}");
            Assert.That(_player.transform.position.y, Is.EqualTo(FloorTop + 1.625f * 0.5f).Within(0.1f),
                "Feet should rest on the floor top (hitbox is 1.625 tall)");
        }

        [UnityTest]
        public IEnumerator Kael_Jump_ReachesSpecHeight()
        {
            yield return Settle();
            float startY = _player.transform.position.y;

            _input.PressJump();
            yield return null;

            float maxY = startY;
            for (int i = 0; i < 40; i++) // hold ~0.8s (past the apex)
            {
                yield return new WaitForFixedUpdate();
                maxY = Mathf.Max(maxY, _player.transform.position.y);
            }

            Assert.That(maxY - startY, Is.EqualTo(3.5f).Within(0.3f), "Jump height must be 3.5 tiles (spec 3.2)");
        }

        [UnityTest]
        public IEnumerator Kael_ReleasingJumpEarly_CutsTheJumpShort()
        {
            yield return Settle();
            float startY = _player.transform.position.y;

            _input.PressJump();
            yield return new WaitForSeconds(0.08f);
            _input.ReleaseJump();
            float maxY = startY;
            for (int i = 0; i < 50; i++)
            {
                yield return new WaitForFixedUpdate();
                maxY = Mathf.Max(maxY, _player.transform.position.y);
            }

            Assert.Less(maxY - startY, 3.0f, "Variable jump: early release must lower the apex");
            Assert.Greater(maxY - startY, 0.5f, "...but Kael must still have jumped");
        }

        [UnityTest]
        public IEnumerator Kael_Dash_CoversAboutFourTiles()
        {
            yield return Settle();
            float startX = _player.transform.position.x;

            _input.PressDash();
            yield return new WaitForSeconds(_controller.DashDuration);

            float dx = _player.transform.position.x - startX;
            Assert.That(dx, Is.EqualTo(4f).Within(0.6f), "Phase Dash must cover ~4 tiles in 0.2s (spec D4)");
        }

        [UnityTest]
        public IEnumerator Kael_WalkingOffALedge_Falls()
        {
            // Regression: Idle/Run applied no gravity, so Kael hovered after leaving the ground
            Object.Destroy(_player);
            Object.Destroy(_floor);
            yield return null; // let the destroys happen before rebuilding

            _floor = CreateBox("Floor", new Vector2(-30f, FloorTop - 0.5f), new Vector2(60f, 1f)); // ends at x = 0
            _player = CreatePlayer(new Vector2(-3f, 1f));
            yield return Settle();
            float groundedY = _player.transform.position.y;

            _input.Move = 1f;
            string path = "";
            for (int i = 0; i < 8; i++)
            {
                yield return new WaitForSeconds(0.15f);
                path += $"[{_player.transform.position.x:F1},{_player.transform.position.y:F1},{_controller.CurrentStateName}] ";
            }
            _input.Move = 0f;

            Assert.Less(_player.transform.position.y, groundedY - 1.5f,
                $"Kael must fall after running off the ledge. path={path}");
        }

        [UnityTest]
        public IEnumerator Kael_TouchingWall_IsDetected()
        {
            CreateBox("Wall", new Vector2(1.0f, 5f), new Vector2(1f, 20f)); // left face at x = 0.5
            _player.transform.position = new Vector2(0f, 4f);
            _rb.linearVelocity = Vector2.zero;

            bool sawWall = false;
            for (int i = 0; i < 10 && !sawWall; i++)
            {
                yield return new WaitForFixedUpdate();
                sawWall = _controller.IsTouchingWall;
            }

            Assert.IsTrue(sawWall, "Wall next to Kael in mid-air must be detected");
            Assert.AreEqual(1f, _controller.WallDirection);
        }

        [UnityTest]
        public IEnumerator Kael_Dies_ThenRespawnsAtCheckpointWithHalfHealth()
        {
            yield return Settle();
            Vector3 checkpoint = _player.transform.position;

            _player.transform.position = checkpoint + new Vector3(8f, 0f, 0f);
            _stats.TakeDamage(new DamageInfo(999, Vector2.zero, Vector2.zero, RealmType.Prime));
            Assert.IsTrue(_stats.IsDead);

            yield return new WaitForSecondsRealtime(1.6f);

            Assert.IsFalse(_stats.IsDead, "Kael must be revived");
            Assert.AreEqual(_stats.MaxHealth / 2, _stats.CurrentHealth, "Respawn health is 50% (spec 3.3)");
            Assert.That(_player.transform.position.x, Is.EqualTo(checkpoint.x).Within(0.5f), "Respawn at checkpoint");
            Assert.IsTrue(_controller.enabled, "Controller must be re-enabled after respawn");
        }

        [UnityTest]
        public IEnumerator Kael_WallCoyoteTime_IsActive_WhenLeavingWall()
        {
            CreateBox("Wall", new Vector2(1.0f, 5f), new Vector2(1f, 20f));
            _player.transform.position = new Vector2(0f, 4f);
            _rb.linearVelocity = Vector2.zero;

            for (int i = 0; i < 10; i++) yield return new WaitForFixedUpdate();

            Assert.IsTrue(_controller.IsTouchingWall, "Should touch wall");
            Assert.IsTrue(_controller.CanWallJump, "Can wall jump while touching wall");

            // Move away from wall
            _player.transform.position = new Vector2(-1f, 4f);
            yield return new WaitForFixedUpdate();

            Assert.IsFalse(_controller.IsTouchingWall, "Left the wall");
            Assert.Greater(_controller.WallCoyoteTimer, 0f, "Wall coyote timer active");
            Assert.IsTrue(_controller.CanWallJump, "Can still wall jump during coyote window");
        }

        [UnityTest]
        public IEnumerator Kael_RisingJump_DoesNotStickToWall()
        {
            CreateBox("Wall", new Vector2(1.0f, 5f), new Vector2(1f, 20f));
            _player.transform.position = new Vector2(0f, 1.5f);
            yield return Settle();

            _input.PressJump();
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            Assert.AreEqual("PlayerJumpState", _controller.CurrentStateName, "Must remain in JumpState while rising, not WallSlideState");
            Assert.Greater(_controller.VerticalSpeedUp, 1f, "Must maintain upward momentum while jumping");
        }
    }
}
