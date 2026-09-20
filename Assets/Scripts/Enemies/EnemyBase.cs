using System;
using System.Collections;
using UnityEngine;
using EchoOfTheVoid.Core;
using EchoOfTheVoid.Combat;
using EchoOfTheVoid.Feedback;

namespace EchoOfTheVoid.Enemies
{
    [RequireComponent(typeof(Rigidbody2D))]
    public abstract class EnemyBase : MonoBehaviour, IDamageable
    {
        [Header("Configuration")]
        [SerializeField] protected EnemyDataSO enemyData;
        [SerializeField] protected RealmType customRealm = RealmType.Prime;
        [SerializeField] protected bool overrideDataRealm = false;

        [Header("Poise & Stun Settings")]
        [SerializeField] protected float stunDuration = 3.0f;
        [SerializeField] protected float poiseRecoverDelay = 3.5f;
        [SerializeField] protected float poiseRecoverRate = 20.0f;

        protected int currentHealth;
        protected float currentPoise;
        protected bool isDead;
        protected bool isStunned;
        protected float stunTimer;
        protected float lastDamageTime;
        protected Vector3 originalLocalScale;

        protected Rigidbody2D rb;
        protected Collider2D col;
        protected SpriteRenderer spriteRenderer;

        public RealmType EntityRealm => overrideDataRealm ? customRealm : (enemyData != null ? enemyData.Realm : customRealm);
        public int CurrentHealth => currentHealth;
        public float CurrentPoise => currentPoise;
        public float MaxPoise => (enemyData != null) ? enemyData.PoiseMax : 50f;
        public bool IsDead => isDead;
        public bool IsStunned => isStunned;

        public event Action<int, int> OnHealthChanged;
        public event Action<bool, int> OnDamaged; // isDeflected, damage
        public event Action<bool> OnStunStateChanged;
        public event Action OnDeath;
        public event Action OnReset;

        protected virtual void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<Collider2D>();
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            originalLocalScale = transform.localScale;

            int maxHp = (enemyData != null) ? enemyData.MaxHealth : 50;
            currentHealth = maxHp;
            currentPoise = MaxPoise;
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

        protected virtual void Update()
        {
            if (isDead) return;

            UpdatePoiseAndStun();
        }

        protected virtual void UpdatePoiseAndStun()
        {
            if (isStunned)
            {
                stunTimer -= Time.deltaTime;
                if (stunTimer <= 0f)
                {
                    ExitStun();
                }
            }
            else
            {
                // Regenerate poise if not hit recently
                if (Time.time - lastDamageTime >= poiseRecoverDelay && currentPoise < MaxPoise)
                {
                    currentPoise = Mathf.Min(MaxPoise, currentPoise + poiseRecoverRate * Time.deltaTime);
                }
            }
        }

        protected virtual void EnterStun(float duration)
        {
            isStunned = true;
            stunTimer = duration;
            if (rb != null)
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            }
            OnStunStateChanged?.Invoke(true);
        }

        protected virtual void ExitStun()
        {
            isStunned = false;
            currentPoise = MaxPoise;
            OnStunStateChanged?.Invoke(false);
            UpdateVisualAffinity();
        }

        protected virtual void HandleRealmSwitch(RealmType currentRealm)
        {
            UpdateVisualAffinity();
        }

        protected virtual void UpdateVisualAffinity()
        {
            if (spriteRenderer == null) return;

            if (isStunned)
            {
                spriteRenderer.color = new Color(0.7f, 0.7f, 0.7f, 0.9f);
                return;
            }

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

            lastDamageTime = Time.time;

            // GDD Affinity Matrix:
            // Same Realm: 100% Damage
            // Opposite Realm: 20% Damage (Void Armor / Deflected)
            bool isDeflected = (info.AttackRealm != EntityRealm);
            int baseDamage = isDeflected 
                ? Mathf.Max(1, Mathf.RoundToInt(info.Amount * 0.20f)) 
                : info.Amount;

            // Spec §5.2: Poise 0 -> Overheat/Stun: +50% bonus damage taken
            int dealtDamage = isStunned ? Mathf.RoundToInt(baseDamage * 1.5f) : baseDamage;

            currentHealth -= dealtDamage;
            OnDamaged?.Invoke(isDeflected, dealtDamage);
            OnHealthChanged?.Invoke(currentHealth, (enemyData != null ? enemyData.MaxHealth : 50));

            // Poise reduction
            if (!isDeflected && !isStunned)
            {
                float poiseDmg = info.IsResonance ? 50f : (info.Amount >= 20 ? 30f : 15f);
                currentPoise = Mathf.Max(0f, currentPoise - poiseDmg);

                if (currentPoise <= 0f)
                {
                    EnterStun(stunDuration);
                }
            }

            // Knockback (only when not deflected)
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

            StartCoroutine(DeathRoutine());
        }

        protected virtual IEnumerator DeathRoutine()
        {
            float elapsed = 0f;
            Vector3 initScale = transform.localScale;
            while (elapsed < 0.25f)
            {
                elapsed += Time.deltaTime;
                transform.localScale = Vector3.Lerp(initScale, Vector3.zero, elapsed / 0.25f);
                yield return null;
            }

            // If an EnemyRespawner is present, let it handle disabling instead of destroying
            var respawner = GetComponent<EnemyRespawner>();
            if (respawner != null)
            {
                gameObject.SetActive(false);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public virtual void ResetEnemyState()
        {
            StopAllCoroutines();
            isDead = false;
            isStunned = false;
            stunTimer = 0f;

            int maxHp = (enemyData != null) ? enemyData.MaxHealth : 50;
            currentHealth = maxHp;
            currentPoise = MaxPoise;

            transform.localScale = originalLocalScale;

            if (col != null) col.enabled = true;
            if (rb != null) rb.linearVelocity = Vector2.zero;

            UpdateVisualAffinity();
            OnHealthChanged?.Invoke(currentHealth, maxHp);
            OnStunStateChanged?.Invoke(false);
            OnReset?.Invoke();
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
            if (isDead || isStunned) return;

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
