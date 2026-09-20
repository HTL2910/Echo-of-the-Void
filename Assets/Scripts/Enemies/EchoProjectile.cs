using UnityEngine;
using EchoOfTheVoid.Core;
using EchoOfTheVoid.Combat;

namespace EchoOfTheVoid.Enemies
{
    [RequireComponent(typeof(CircleCollider2D))]
    public class EchoProjectile : MonoBehaviour
    {
        [SerializeField] private float speed = 12f;
        [SerializeField] private int damage = 18;
        [SerializeField] private float lifetime = 5f;

        private Vector2 _direction;
        private CircleCollider2D _collider;
        private SpriteRenderer _renderer;

        public void Initialize(Vector2 direction)
        {
            _direction = direction.normalized;
            _collider = GetComponent<CircleCollider2D>();
            _collider.isTrigger = true;
            _renderer = GetComponentInChildren<SpriteRenderer>();

            Destroy(gameObject, lifetime);
        }

        private void Update()
        {
            transform.Translate(_direction * speed * Time.deltaTime, Space.World);

            // GDD Reality Rule:
            // If Player is in Prime, projectile is ghost (transparent/cannot hit)
            // If Player is in Echo, projectile is solid physical purple orb!
            RealmType playerRealm = (RealityManager.Instance != null) 
                ? RealityManager.Instance.CurrentRealm 
                : RealmType.Prime;

            if (_renderer != null)
            {
                Color c = new Color(0.85f, 0.2f, 1.0f, (playerRealm == RealmType.Echo) ? 1.0f : 0.35f);
                _renderer.color = c;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Only hits player if in Echo realm!
            RealmType currentRealm = (RealityManager.Instance != null) ? RealityManager.Instance.CurrentRealm : RealmType.Prime;
            if (currentRealm != RealmType.Echo)
            {
                // Passes through without damage
                return;
            }

            if (other.CompareTag("Player"))
            {
                var damageable = other.GetComponentInParent<IDamageable>();
                if (damageable != null)
                {
                    DamageInfo info = new DamageInfo(damage, transform.position, _direction * 6f, RealmType.Echo, isResonance: false, attacker: gameObject);
                    damageable.TakeDamage(info);
                }
                Destroy(gameObject);
            }
            else if (!other.isTrigger && !other.GetComponentInParent<EnemyBase>())
            {
                // Collides with solid geometry in Echo
                Destroy(gameObject);
            }
        }
    }
}
