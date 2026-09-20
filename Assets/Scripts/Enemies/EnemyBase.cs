using System;
using System.Collections;
using UnityEngine;
using EchoOfTheVoid.Core;
using EchoOfTheVoid.Combat;
using EchoOfTheVoid.Feedback;

namespace EchoOfTheVoid.Enemies
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public abstract class EnemyBase : MonoBehaviour, IDamageable
    {
        [Header("Configuration")]
        [SerializeField] protected EnemyDataSO enemyData;
        [SerializeField] protected RealmType customRealm = RealmType.Prime;
        [SerializeField] protected bool overrideDataRealm = false;

        protected int currentHealth;
        protected float currentPoise;
        protected bool isDead;
        protected Rigidbody2D rb;
        protected Collider2D col;
        protected SpriteRenderer spriteRenderer;

        public RealmType EntityRealm => overrideDataRealm ? customRealm : (enemyData != null ? enemyData.Realm : customRealm);
        public int CurrentHealth => currentHealth;
        public bool IsDead => isDead;

        public event Action<int, int> OnHealthChanged;
        public event Action<bool, int> OnDamaged; // isDeflected, damage
        public event Action OnDeath;

        protected virtual void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<Collider2D>();
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            int maxHp = (enemyData != null) ? enemyData.MaxHealth : 50;
            currentHealth = maxHp;
            currentPoise = (enemyData != null) ? enemyData.PoiseMax : 50f;
        }

        protected virtual void Start()
        {
            UpdateVisualAffinity();
            RealityEventBus.OnRealmSwitched += HandleRealmSwitch;
        }

        protected virtual void OnDestroy()
        {
            RealityEventBus.OnRealmSwitched -= HandleRealmSwitch;
        }

        protected virtual void HandleRealmSwitch(RealmType currentRealm)
        {
            UpdateVisualAffinity();
        }

        protected virtual void UpdateVisualAffinity()
        {
            if (spriteRenderer == null) return;

            // Visual feedback: outline or tint based on EntityRealm
            Color baseColor = (EntityRealm == RealmType.Prime)
                ? new Color(0.2f, 0.9f, 1.0f, 1f)   // Prime Cyan
                : new Color(0.85f, 0.25f, 1.0f, 1f); // Echo Purple

            spriteRenderer.color = baseColor;
        }

        public virtual HitFeedback TakeDamage(DamageInfo info)
        {
            if (isDead)
            {
                return new HitFeedback(isDeflected: false, dealtDamage: 0, isDead: true);
            }

            // GDD Affinity Matrix:
            // Same Realm: 100% Damage
            // Opposite Realm: 20% Damage (Void Armor / Deflected)
            bool isDeflected = (info.AttackRealm != EntityRealm);
            int dealtDamage = isDeflected 
                ? Mathf.Max(1, Mathf.RoundToInt(info.Amount * 0.20f)) 
                : info.Amount;

            currentHealth -= dealtDamage;
            OnDamaged?.Invoke(isDeflected, dealtDamage);
            OnHealthChanged?.Invoke(currentHealth, (enemyData != null ? enemyData.MaxHealth : 50));

            // Knockback
            if (rb != null && !isDeflected)
            {
                rb.linearVelocity = info.Knockback;
            }

            // Flash effect
            StartCoroutine(FlashWhiteRoutine(isDeflected));

            if (currentHealth <= 0)
            {
                Die();
                return new HitFeedback(isDeflected, dealtDamage, isDead: true);
            }

            return new HitFeedback(isDeflected, dealtDamage, isDead: false);
        }

        protected virtual void Die()
        {
            if (isDead) return;
            isDead = true;
            OnDeath?.Invoke();
            VfxLibrary.Play(VfxId.EnemyDeath, transform.position);

            if (col != null) col.enabled = false;
            if (rb != null) rb.linearVelocity = Vector2.zero;

            // Simple shrink and destroy
            StartCoroutine(DeathRoutine());
        }

        private IEnumerator DeathRoutine()
        {
            float elapsed = 0f;
            Vector3 initScale = transform.localScale;
            while (elapsed < 0.25f)
            {
                elapsed += Time.deltaTime;
                transform.localScale = Vector3.Lerp(initScale, Vector3.zero, elapsed / 0.25f);
                yield return null;
            }
            Destroy(gameObject);
        }

        private IEnumerator FlashWhiteRoutine(bool isDeflected)
        {
            if (spriteRenderer == null) yield break;

            Color orig = spriteRenderer.color;
            spriteRenderer.color = isDeflected ? new Color(1f, 1f, 0.3f, 1f) : Color.white; // Yellow if deflected, White if clean
            yield return new WaitForSeconds(0.08f);
            if (spriteRenderer != null) spriteRenderer.color = orig;
        }

        protected virtual void OnCollisionEnter2D(Collision2D collision)
        {
            if (isDead) return;

            // Contact damage to player
            var playerDamageable = collision.gameObject.GetComponentInParent<IDamageable>();
            if (playerDamageable != null && collision.gameObject.CompareTag("Player"))
            {
                int contactDmg = (enemyData != null) ? enemyData.ContactDamage : 10;
                Vector2 dir = (collision.transform.position - transform.position).normalized;
                DamageInfo dmg = new DamageInfo(contactDmg, collision.contacts[0].point, dir * 8f, EntityRealm, isResonance: false, attacker: gameObject);
                playerDamageable.TakeDamage(dmg);
            }
        }
    }
}
