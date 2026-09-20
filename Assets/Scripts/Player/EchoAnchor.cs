using System;
using UnityEngine;
using EchoOfTheVoid.Core;
using EchoOfTheVoid.Environment;
using EchoOfTheVoid.UI;

namespace EchoOfTheVoid.Player
{
    /// <summary>
    /// Echo Anchor (spec 4): press once to leave a shadow behind (25 CE, one at a time, lasts 8 s), press again to swap
    /// places with it. The shadow stands on the "Anchor" layer, so pressure plates count it as someone standing there.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class EchoAnchor : MonoBehaviour
    {
        public const int EnergyCost = 25;
        public const float Lifetime = 8f;
        private const float FadeSeconds = 1f;

        private PlayerController _controller;
        private PlayerStats _stats;
        private Rigidbody2D _rb;
        private BoxCollider2D _box;
        private SpriteRenderer _kaelSprite;

        private GameObject _shadow;
        private SpriteRenderer _shadowSprite;
        private float _expiresAt;

        public bool HasAnchor => _shadow != null;
        public Vector2 AnchorPosition => _shadow != null ? (Vector2)_shadow.transform.position : Vector2.zero;
        public float RemainingSeconds => _shadow != null ? Mathf.Max(0f, _expiresAt - Time.time) : 0f;

        public event Action Placed;
        public event Action Swapped;
        public event Action Expired;
        public event Action Blocked;

        private void Awake()
        {
            _controller = GetComponent<PlayerController>();
            _stats = GetComponent<PlayerStats>();
            _rb = GetComponent<Rigidbody2D>();
            _box = GetComponent<BoxCollider2D>();
            _kaelSprite = GetComponentInChildren<SpriteRenderer>();
        }

        private void OnEnable()
        {
            if (_stats != null) _stats.OnPlayerDeath += Clear;
        }

        private void OnDisable()
        {
            if (_stats != null) _stats.OnPlayerDeath -= Clear;
            Clear();
        }

        private void Update()
        {
            if (_shadow == null) return;

            float remaining = _expiresAt - Time.time;
            if (remaining <= 0f)
            {
                Clear();
                Expired?.Invoke();
                return;
            }

            // Blink during the last second so the player knows it is about to go
            if (remaining < FadeSeconds && _shadowSprite != null)
            {
                var c = _shadowSprite.color;
                c.a = 0.25f + 0.35f * Mathf.Abs(Mathf.Sin(Time.time * 14f));
                _shadowSprite.color = c;
            }
        }

        /// <summary>The button press: places the anchor, or swaps with it if one exists.</summary>
        /// <returns>true if something happened.</returns>
        public bool Activate()
        {
            if (!_controller.HasAbility(AbilityFlags.EchoAnchor)) return false;
            return HasAnchor ? Swap() : Place();
        }

        private bool Place()
        {
            if (_stats != null && !_stats.ConsumeEnergy(EnergyCost)) return false;

            _shadow = BuildShadow();
            _expiresAt = Time.time + Lifetime;
            AudioManager.Play(SfxGroup.AnchorPlace);
            Placed?.Invoke();
            return true;
        }

        private bool Swap()
        {
            Vector3 destination = _shadow.transform.position;

            if (!IsFree(destination))
            {
                if (PlayerHUD.Instance != null) PlayerHUD.Instance.ShowAnnouncement("ANCHOR BLOCKED", 0.8f);
                Blocked?.Invoke();
                return false;
            }

            Clear();
            _rb.position = destination;
            transform.position = destination;
            _rb.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();

            AudioManager.Play(SfxGroup.AnchorSwap);
            Swapped?.Invoke();
            return true;
        }

        /// <summary>Would Kael fit at <paramref name="destination"/> without being embedded in something solid?</summary>
        private bool IsFree(Vector3 destination)
        {
            Bounds b = _box.bounds;
            b.center += destination - transform.position;
            b.Expand(-0.12f); // touching is fine

            RealmType realm = RealityManager.Instance != null ? RealityManager.Instance.CurrentRealm : RealmType.Prime;
            if (RealityObstacles.AnySolidOverlap(realm, b)) return false;

            foreach (var hit in Physics2D.OverlapBoxAll(b.center, b.size, 0f))
            {
                if (hit.isTrigger || hit.attachedRigidbody == _rb) continue;
                if (_shadow != null && hit.transform.IsChildOf(_shadow.transform)) continue;
                return false;
            }
            return true;
        }

        private GameObject BuildShadow()
        {
            var root = new GameObject("EchoAnchor_Shadow");
            root.transform.position = transform.position;
            int layer = LayerMask.NameToLayer("Anchor");
            root.layer = layer >= 0 ? layer : 0;

            var body = root.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic; // gives the trigger events a body to report against
            var col = root.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = _box.size;
            col.offset = _box.offset;

            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            visual.layer = root.layer;
            _shadowSprite = visual.AddComponent<SpriteRenderer>();
            if (_kaelSprite != null)
            {
                _shadowSprite.sprite = _kaelSprite.sprite;
                _shadowSprite.flipX = _kaelSprite.flipX;
                _shadowSprite.sortingOrder = _kaelSprite.sortingOrder - 1;
                visual.transform.position = _kaelSprite.transform.position;
                visual.transform.localScale = _kaelSprite.transform.lossyScale;
            }
            bool echo = RealityManager.Instance != null && RealityManager.Instance.CurrentRealm == RealmType.Echo;
            _shadowSprite.color = echo ? new Color(0.85f, 0.4f, 1f, 0.55f) : new Color(0.3f, 0.9f, 1f, 0.55f);
            return root;
        }

        /// <summary>Remove the shadow without any effect (death, respawn, expiry, after a swap).</summary>
        public void Clear()
        {
            if (_shadow != null) Destroy(_shadow);
            _shadow = null;
            _shadowSprite = null;
        }
    }
}
