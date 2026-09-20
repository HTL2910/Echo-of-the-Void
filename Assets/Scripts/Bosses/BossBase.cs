using System;
using System.Collections;
using UnityEngine;
using EchoOfTheVoid.Core;
using EchoOfTheVoid.Combat;
using EchoOfTheVoid.Player;

// Note: BossBase subscribes to PlayerStats.OnPlayerDeath at runtime via FindObject.

namespace EchoOfTheVoid.Bosses
{
    /// <summary>
    /// K4: Base class for all bosses. Handles HP, Poise/Overheat, realm affinity matrix,
    /// BossEvents integration, and reset on player death.
    /// Concrete bosses override RunPhaseRoutine() to implement their specific attack FSM.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public abstract class BossBase : MonoBehaviour, IDamageable
    {
        [Header("Boss Identity")]
        [SerializeField] protected string bossId = "boss_id";
        [SerializeField] protected string displayName = "BOSS";

        [Header("Stats")]
        [SerializeField] protected int maxHp = 600;
        [SerializeField] protected float maxPoise = 100f;
        [SerializeField] protected float stunDuration = 3.5f;
        [SerializeField] protected float stunDamageMultiplier = 1.5f;

        [Header("Phase Configuration")]
        [SerializeField] protected BossPhaseData[] phases;

        protected int currentHp;
        protected float currentPoise;
        protected bool isDead;
        protected bool isStunned;
        protected int currentPhaseIndex;
        protected Rigidbody2D rb;
        protected SpriteRenderer spriteRenderer;
        protected Collider2D col;
        protected RealmType myRealm = RealmType.Echo;

        public string BossId => bossId;
        public string DisplayName => displayName;
        public RealmType EntityRealm => myRealm;
        public bool IsDead => isDead;
        public bool IsStunned => isStunned;
        public int CurrentHp => currentHp;

        protected virtual void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<Collider2D>();
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            currentHp = maxHp;
            currentPoise = maxPoise;
        }

        private PlayerStats _trackedPlayerStats;

        protected virtual void Start()
        {
            // Find and subscribe to the player's death event for boss reset
            SubscribeToPlayerDeath();
        }

        protected virtual void OnDestroy()
        {
            if (_trackedPlayerStats != null)
                _trackedPlayerStats.OnPlayerDeath -= OnPlayerDied;
        }

        private void SubscribeToPlayerDeath()
        {
            var playerGo = GameObject.FindGameObjectWithTag("Player");
            if (playerGo != null)
            {
                _trackedPlayerStats = playerGo.GetComponent<PlayerStats>();
                if (_trackedPlayerStats != null)
                    _trackedPlayerStats.OnPlayerDeath += OnPlayerDied;
            }
        }

        // Called by BossArena when the fight starts
        public virtual void Engage()
        {
            BossEvents.RaiseEngaged(bossId, displayName, maxHp);
            StartCoroutine(BossFightRoutine());
        }

        private IEnumerator BossFightRoutine()
        {
            currentPhaseIndex = 0;
            while (!isDead)
            {
                // Check for phase advance
                if (phases != null && currentPhaseIndex < phases.Length - 1)
                {
                    var nextPhase = phases[currentPhaseIndex + 1];
                    float hpFraction = (float)currentHp / maxHp;
                    if (hpFraction <= nextPhase.HpThreshold)
                    {
                        currentPhaseIndex++;
                    }
                }

                yield return RunPhaseRoutine(phases != null && phases.Length > 0 ? phases[currentPhaseIndex] : null);
            }
        }

        /// <summary>Concrete bosses implement their per-phase attack logic here.</summary>
        protected abstract IEnumerator RunPhaseRoutine(BossPhaseData phase);

        public virtual HitFeedback TakeDamage(DamageInfo info)
        {
            if (isDead) return new HitFeedback(false, 0, true);

            bool isDeflected = (info.AttackRealm != myRealm);
            int baseDmg = isDeflected
                ? Mathf.Max(1, Mathf.RoundToInt(info.Amount * 0.2f))
                : info.Amount;

            int dealtDmg = isStunned ? Mathf.RoundToInt(baseDmg * stunDamageMultiplier) : baseDmg;
            currentHp = Mathf.Max(0, currentHp - dealtDmg);

            BossEvents.RaiseHealthChanged(bossId, currentHp, maxHp);

            // Poise damage
            if (!isDeflected && !isStunned)
            {
                float poiseDmg = info.IsResonance ? maxPoise * 0.5f : (info.Amount >= 20 ? 30f : 15f);
                currentPoise = Mathf.Max(0f, currentPoise - poiseDmg);
                if (currentPoise <= 0f) StartCoroutine(OverheatRoutine());
            }

            // Knockback (for stationary segments of the boss, no-op for anchored)
            if (rb != null && !isDeflected)
                rb.linearVelocity += info.Knockback * 0.1f;

            StartCoroutine(FlashRoutine(isDeflected));

            if (currentHp <= 0)
            {
                StartCoroutine(DieRoutine());
                return new HitFeedback(isDeflected, dealtDmg, true);
            }

            return new HitFeedback(isDeflected, dealtDmg, false);
        }

        protected virtual IEnumerator OverheatRoutine()
        {
            isStunned = true;
            if (rb != null) rb.linearVelocity = Vector2.zero;
            yield return new WaitForSeconds(stunDuration);
            isStunned = false;
            currentPoise = maxPoise;
        }

        protected virtual IEnumerator DieRoutine()
        {
            if (isDead) yield break;
            isDead = true;
            BossEvents.RaiseDefeated(bossId);
            StopAllCoroutines();

            // Scale-down death anim
            float elapsed = 0f;
            Vector3 initScale = transform.localScale;
            while (elapsed < 0.4f)
            {
                elapsed += Time.deltaTime;
                transform.localScale = Vector3.Lerp(initScale, Vector3.zero, elapsed / 0.4f);
                yield return null;
            }

            OnBossDefeated();
            gameObject.SetActive(false);
        }

        /// <summary>Override to spawn rewards, unlock arena, etc.</summary>
        protected virtual void OnBossDefeated() { }

        protected virtual void OnPlayerDied()
        {
            // Reset boss for re-fight when player dies mid-fight
            if (isDead) return;
            ResetBoss();
        }

        public virtual void ResetBoss()
        {
            StopAllCoroutines();
            isDead = false;
            isStunned = false;
            currentHp = maxHp;
            currentPoise = maxPoise;
            currentPhaseIndex = 0;
            transform.localScale = Vector3.one;
            if (col != null) col.enabled = true;
            if (rb != null) rb.linearVelocity = Vector2.zero;
            BossEvents.RaiseReset(bossId);
        }

        private IEnumerator FlashRoutine(bool isDeflected)
        {
            if (spriteRenderer == null) yield break;
            Color orig = spriteRenderer.color;
            spriteRenderer.color = isDeflected ? new Color(1f, 1f, 0.3f) : Color.white;
            yield return new WaitForSeconds(0.08f);
            if (spriteRenderer != null) spriteRenderer.color = orig;
        }
    }
}
