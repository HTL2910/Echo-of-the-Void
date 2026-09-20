using System.Collections.Generic;
using UnityEngine;
using EchoOfTheVoid.Player;

namespace EchoOfTheVoid.Tests
{
    /// <summary>Scriptable input: edge flags fire for exactly one frame, Move is held.</summary>
    public class FakeInput : IPlayerInput
    {
        public float Move;
        private bool _jump, _jumpReleased, _dash, _interact, _shift, _attack, _anchor, _gravity;

        public void PressJump() => _jump = true;
        public void ReleaseJump() => _jumpReleased = true;
        public void PressDash() => _dash = true;
        public void PressInteract() => _interact = true;
        public void PressShift() => _shift = true;
        public void PressAttack() => _attack = true;
        public void PressAnchor() => _anchor = true;
        public void PressGravity() => _gravity = true;

        public PlayerInputFrame Poll()
        {
            var frame = new PlayerInputFrame
            {
                Move = Move,
                JumpPressed = _jump,
                JumpReleased = _jumpReleased,
                DashPressed = _dash,
                InteractPressed = _interact,
                ShiftPressed = _shift,
                AttackPressed = _attack,
                AnchorPressed = _anchor,
                GravityPressed = _gravity
            };
            _jump = _jumpReleased = _dash = _interact = _shift = _attack = _anchor = _gravity = false;
            return frame;
        }
    }

    /// <summary>A flat floor (top at y = 0) plus a fully wired Kael driven by <see cref="FakeInput"/>.</summary>
    public class TestWorld
    {
        public readonly FakeInput Input = new FakeInput();
        public GameObject Floor;
        public GameObject Player;
        public PlayerController Controller;
        public PlayerStats Stats;
        public PlayerRespawn Respawn;
        public AbilitySet Abilities;
        public EchoAnchor Anchor;
        public Rigidbody2D Rb;
        private readonly List<GameObject> _boxes = new List<GameObject>();
        public readonly int NeutralLayer = LayerMask.NameToLayer("Neutral");

        public static TestWorld Create(Vector2 playerPosition, bool withAnimator = false)
        {
            var world = new TestWorld();
            world.Floor = world.CreateBox("Floor", new Vector2(0f, -0.5f), new Vector2(60f, 1f));
            world.Player = world.CreatePlayer(playerPosition, withAnimator);
            return world;
        }

        public GameObject CreateBox(string name, Vector2 center, Vector2 size)
        {
            var go = new GameObject(name);
            go.layer = NeutralLayer;
            go.transform.position = center;
            go.AddComponent<BoxCollider2D>().size = size;
            _boxes.Add(go);
            return go;
        }

        private GameObject CreatePlayer(Vector2 position, bool withAnimator)
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
            if (withAnimator) visual.AddComponent<Animator>();

            go.AddComponent<PlayerStats>();
            go.AddComponent<PlayerCombat>();
            go.AddComponent<PlayerRespawn>(); // brings PlayerController via RequireComponent
            go.AddComponent<PlayerAnimationDriver>();
            go.AddComponent<AbilitySet>();
            go.AddComponent<EchoAnchor>();

            Controller = go.GetComponent<PlayerController>();
            Controller.SetGroundLayer(1 << NeutralLayer);
            Controller.SetInput(Input);
            Stats = go.GetComponent<PlayerStats>();
            Respawn = go.GetComponent<PlayerRespawn>();
            Abilities = go.GetComponent<AbilitySet>();
            Anchor = go.GetComponent<EchoAnchor>();
            Rb = rb;

            go.SetActive(true);
            return go;
        }

        public void Dispose()
        {
            if (Player != null) Object.Destroy(Player);
            if (Floor != null) Object.Destroy(Floor);
            foreach (var box in _boxes) if (box != null) Object.Destroy(box);
            _boxes.Clear();
            Time.timeScale = 1f;
        }
    }
}
