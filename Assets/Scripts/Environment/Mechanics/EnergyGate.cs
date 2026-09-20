using System;
using UnityEngine;
using EchoOfTheVoid.Core;
using EchoOfTheVoid.Combat;
using EchoOfTheVoid.Player;

namespace EchoOfTheVoid.Environment.Mechanics
{
    /// <summary>
    /// K3: Energy Gate (spec §4).
    /// Normal movement: blocked + contact damage.
    /// Phase Dash (i-frames): player passes through freely.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class EnergyGate : MonoBehaviour
    {
        [SerializeField] private int contactDamage = 10;
        [SerializeField] private RealmType gateRealm = RealmType.Prime;

        private Collider2D _col;
        private SpriteRenderer _sprite;
        private RealmType _currentRealm;

        private static readonly Color ActiveColor   = new Color(1f, 0.8f, 0.1f, 0.9f); // electric yellow
        private static readonly Color InactiveColor = new Color(0.5f, 0.5f, 0.5f, 0.4f);

        private void Awake()
        {
            _col = GetComponent<Collider2D>();
            _sprite = GetComponentInChildren<SpriteRenderer>();
            _col.isTrigger = true;
        }

        private void Start()
        {
            _currentRealm = RealityManager.Instance != null 
                ? RealityManager.Instance.CurrentRealm 
                : RealmType.Prime;
            UpdateState();
            RealityEventBus.OnRealmSwitched += OnRealmSwitch;
        }

        private void OnDestroy()
        {
            RealityEventBus.OnRealmSwitched -= OnRealmSwitch;
        }

        private void OnRealmSwitch(RealmType realm)
        {
            _currentRealm = realm;
            UpdateState();
        }

        private void UpdateState()
        {
            // Gate blocks when current realm matches gateRealm
            bool active = (_currentRealm == gateRealm);
            if (_sprite != null)
                _sprite.color = active ? ActiveColor : InactiveColor;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_currentRealm != gateRealm) return; // gate inactive in this realm

            var controller = other.GetComponentInParent<PlayerController>();
            if (controller == null) return;

            // i-frame (Phase Dash) check: if player has invincibility frames, pass through
            var stats = controller.GetComponent<PlayerStats>();
            if (stats != null && stats.IsInvulnerable) return;

            // Deal contact damage and push back
            var damageable = controller.GetComponent<IDamageable>();
            if (damageable != null)
            {
                Vector2 dir = (controller.transform.position - transform.position).normalized;
                DamageInfo info = new DamageInfo(contactDamage, other.ClosestPoint(transform.position), dir * 6f, gateRealm);
                damageable.TakeDamage(info);
            }
        }
    }
}
