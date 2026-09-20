using UnityEngine;
using EchoOfTheVoid.Core;
using EchoOfTheVoid.Player;
using EchoOfTheVoid.Combat;

namespace EchoOfTheVoid.Environment.Mechanics
{
    /// <summary>
    /// K3: Metal spikes hazard (spec §4).
    /// Prime Realm: touching causes SoftRespawn (no HP loss) — 1-hit environmental kill.
    /// Echo Realm: transforms into a bounce pad that launches Kael upward.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Spikes : MonoBehaviour
    {
        [Header("Bounce Settings (Echo Realm)")]
        [SerializeField] private float bounceForce = 18f;

        [Header("Prime Spike Settings")]
        [SerializeField] private bool useSoftRespawn = true;

        private SpriteRenderer _sprite;
        private RealmType _currentRealm = RealmType.Prime;

        // Color coding
        private static readonly Color PrimeColor = new Color(1f, 0.3f, 0.2f, 1f);   // danger red
        private static readonly Color EchoColor  = new Color(0.4f, 1f, 0.6f, 1f);   // safe green

        private void Awake()
        {
            _sprite = GetComponentInChildren<SpriteRenderer>();
        }

        private void Start()
        {
            // Start in whatever realm the game is in (it may not be Prime, e.g. after Continue)
            if (RealityManager.Instance != null) _currentRealm = RealityManager.Instance.CurrentRealm;
            UpdateVisual();
            RealityEventBus.OnRealmSwitched += OnRealmSwitch;
        }

        private void OnDestroy()
        {
            RealityEventBus.OnRealmSwitched -= OnRealmSwitch;
        }

        private void OnRealmSwitch(RealmType realm)
        {
            _currentRealm = realm;
            UpdateVisual();
        }

        private void UpdateVisual()
        {
            if (_sprite == null) return;
            _sprite.color = (_currentRealm == RealmType.Prime) ? PrimeColor : EchoColor;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_currentRealm == RealmType.Prime)
            {
                HandlePrimeContact(other);
            }
            else
            {
                HandleEchoContact(other);
            }
        }

        // Kael standing in the spikes when the world flips to Prime must be caught too, not only on entry
        private void OnTriggerStay2D(Collider2D other)
        {
            if (_currentRealm == RealmType.Prime) HandlePrimeContact(other);
        }

        private void HandlePrimeContact(Collider2D other)
        {
            var respawn = other.GetComponentInParent<PlayerRespawn>();
            if (respawn == null) return;

            if (useSoftRespawn)
                respawn.SoftRespawn();
        }

        private void HandleEchoContact(Collider2D other)
        {
            var rb = other.attachedRigidbody;
            if (rb == null) return;

            // Launch player upward (bounce pad behaviour)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, bounceForce);
        }
    }
}
