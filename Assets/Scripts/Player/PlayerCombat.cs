using System;
using System.Collections;
using UnityEngine;
using EchoOfTheVoid.Core;
using EchoOfTheVoid.Combat;
using EchoOfTheVoid.Feedback;

namespace EchoOfTheVoid.Player
{
    public class PlayerCombat : MonoBehaviour
    {
        [Header("Combat Ranges & Damage (GDD)")]
        [SerializeField] private float combo1Range = 1.8f;
        [SerializeField] private int combo1Damage = 15;
        [SerializeField] private float combo2Range = 2.0f;
        [SerializeField] private int combo2Damage = 18;
        [SerializeField] private float combo3Range = 2.6f;
        [SerializeField] private int combo3Damage = 28;
        [SerializeField] private int airSlashDamage = 20;
        [SerializeField] private int resonanceDamage = 65;
        [SerializeField] private int resonanceCost = 50;

        [Header("Layer Setup")]
        [SerializeField] private LayerMask enemyLayer;

        [Header("Visuals")]
        [SerializeField] private Sprite slashSprite;

        private PlayerController _controller;
        private PlayerStats _stats;

        private int _comboStep = 0;
        private float _lastAttackTime = -10f;
        private const float COMBO_RESET_TIME = 0.55f;
        private bool _isAttacking;

        public bool IsAttacking => _isAttacking;
        public int ComboStep => _comboStep;

        public event Action<int> OnAttackPerformed;
        public event Action<bool, int> OnEnemyHit; // isDeflected, damage

        private void Awake()
        {
            _controller = GetComponent<PlayerController>();
            _stats = GetComponent<PlayerStats>();

            // Layer mask: default to Enemy layer (Layer 10)
            if (enemyLayer.value == 0 || enemyLayer.value == ~0)
            {
                int enemyLayerIndex = LayerMask.NameToLayer("Enemy");
                enemyLayer = (enemyLayerIndex != -1) ? (1 << enemyLayerIndex) : (1 << 10);
            }

            EnsureSlashSprite();
        }

        private void EnsureSlashSprite()
        {
            if (slashSprite == null)
            {
                slashSprite = Resources.Load<Sprite>("SlashArc");
            }
#if UNITY_EDITOR
            if (slashSprite == null)
            {
                slashSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/SlashArc.png");
            }
#endif
            if (slashSprite == null)
            {
                var sr = GetComponentInChildren<SpriteRenderer>();
                if (sr != null) slashSprite = sr.sprite;
            }
        }

        private void Update()
        {
            if (Time.time - _lastAttackTime > COMBO_RESET_TIME && _comboStep > 0 && !_isAttacking)
            {
                _comboStep = 0;
            }
        }

        public bool TryNormalAttack()
        {
            if (_isAttacking) return false;

            if (AudioManager.Instance != null) AudioManager.Instance.PlaySlash();

            if (_controller != null && !_controller.IsGrounded)
            {
                StartCoroutine(ExecuteAirSlash());
            }
            else
            {
                _comboStep++;
                if (_comboStep > 3) _comboStep = 1;
                StartCoroutine(ExecuteComboAttack(_comboStep));
            }

            return true;
        }

        public bool TryResonanceStrike()
        {
            if (_isAttacking) return false;
            if (_controller != null && !_controller.HasAbility(AbilityFlags.ResonanceStrike)) return false;
            if (_stats != null && !_stats.ConsumeEnergy(resonanceCost))
            {
                return false;
            }

            if (AudioManager.Instance != null) AudioManager.Instance.PlayResonance();

            StartCoroutine(ExecuteResonanceStrike());
            return true;
        }

        private IEnumerator ExecuteComboAttack(int step)
        {
            _isAttacking = true;
            _lastAttackTime = Time.time;
            OnAttackPerformed?.Invoke(step);

            float range = (step == 1) ? combo1Range : (step == 2) ? combo2Range : combo3Range;
            int damage = (step == 1) ? combo1Damage : (step == 2) ? combo2Damage : combo3Damage;
            float knockbackMag = (step == 1) ? 2.5f : (step == 2) ? 4.5f : 8f;

            float dir = (_controller != null) ? _controller.FacingDirection : 1f;
            Vector2 attackCenter = (Vector2)transform.position + new Vector2(dir * (range * 0.5f), 0f);
            Vector2 attackSize = new Vector2(range, 1.6f);

            // Spawn visible slash visual
            SpawnSlashVisual(attackCenter, attackSize, step == 3, dir);

            PerformDamageCheck(attackCenter, attackSize, damage, new Vector2(dir * knockbackMag, 1.5f), isResonance: false, isCombo3: (step == 3));

            float duration = (step == 3) ? 0.25f : 0.18f;
            yield return new WaitForSeconds(duration);
            _isAttacking = false;
        }

        private IEnumerator ExecuteAirSlash()
        {
            _isAttacking = true;
            _lastAttackTime = Time.time;
            OnAttackPerformed?.Invoke(4);

            if (_controller != null)
            {
                _controller.FreezeVerticalVelocityFor(0.1f);
            }

            float dir = (_controller != null) ? _controller.FacingDirection : 1f;
            Vector2 attackCenter = (Vector2)transform.position + new Vector2(dir * 1.0f, 0f);
            Vector2 attackSize = new Vector2(2.2f, 2.2f);

            SpawnSlashVisual(attackCenter, attackSize, false, dir);
            PerformDamageCheck(attackCenter, attackSize, airSlashDamage, new Vector2(dir * 4f, 2f), isResonance: false, isCombo3: false);

            yield return new WaitForSeconds(0.20f);
            _isAttacking = false;
        }

        private IEnumerator ExecuteResonanceStrike()
        {
            _isAttacking = true;
            _lastAttackTime = Time.time;
            OnAttackPerformed?.Invoke(5);

            float dir = (_controller != null) ? _controller.FacingDirection : 1f;
            Vector2 attackCenter = (Vector2)transform.position + new Vector2(dir * 4.0f, 0f);
            Vector2 attackSize = new Vector2(8.0f, 2.5f);

            SpawnResonanceWaveVisual(attackCenter, attackSize, dir);

            if (CameraShakeManager.Instance != null) CameraShakeManager.Instance.ShakeHeavy();
            if (HitStopManager.Instance != null) HitStopManager.Instance.TriggerHitStop(0.14f, 0f);

            PerformDamageCheck(attackCenter, attackSize, resonanceDamage, new Vector2(dir * 10f, 3f), isResonance: true, isCombo3: true);

            yield return new WaitForSeconds(0.35f);
            _isAttacking = false;
        }

        private void PerformDamageCheck(Vector2 center, Vector2 size, int damage, Vector2 knockback, bool isResonance, bool isCombo3)
        {
            Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, 0f, enemyLayer);
            RealmType currentRealm = (RealityManager.Instance != null) 
                ? RealityManager.Instance.CurrentRealm 
                : RealmType.Prime;

            foreach (var hit in hits)
            {
                if (hit.gameObject == gameObject) continue;

                var damageable = hit.GetComponentInParent<IDamageable>();
                if (damageable != null)
                {
                    DamageInfo info = new DamageInfo(damage, hit.transform.position, knockback, currentRealm, isResonance, gameObject);
                    HitFeedback feedback = damageable.TakeDamage(info);

                    if (AudioManager.Instance != null) AudioManager.Instance.PlayHit();
                    OnEnemyHit?.Invoke(feedback.IsDeflected, feedback.DealtDamage);

                    // Restore +10 CE on hit
                    if (_stats != null) _stats.RestoreEnergy(10f);

                    // Reset Dash cooldown on hitting enemy per GDD spec!
                    if (_controller != null) _controller.ResetDashCooldown();

                    // Hitstop & Shake based on GDD
                    if (isCombo3)
                    {
                        if (HitStopManager.Instance != null) HitStopManager.Instance.TriggerHitStop(0.09f, 0f);
                        if (CameraShakeManager.Instance != null) CameraShakeManager.Instance.ShakeMedium();
                    }
                    else if (!isResonance)
                    {
                        if (HitStopManager.Instance != null) HitStopManager.Instance.TriggerHitStop(0.05f, 0f);
                        if (CameraShakeManager.Instance != null) CameraShakeManager.Instance.ShakeLight();
                    }
                }
            }
        }

        private void SpawnSlashVisual(Vector2 pos, Vector2 size, bool isFinisher, float dir)
        {
            EnsureSlashSprite();

            GameObject slashObj = new GameObject("Slash_Visual");
            slashObj.transform.position = pos;
            slashObj.transform.localScale = new Vector3(Mathf.Abs(size.x) * dir, size.y, 1f);

            var sr = slashObj.AddComponent<SpriteRenderer>();
            sr.sprite = slashSprite;
            sr.sortingOrder = 10; // Ensure visible in front of all entities

            Color baseColor = (RealityManager.Instance != null && RealityManager.Instance.CurrentRealm == RealmType.Echo)
                ? new Color(0.85f, 0.2f, 1.0f, 0.9f) // Purple for Echo
                : new Color(0.0f, 0.9f, 1.0f, 0.9f); // Cyan for Prime

            sr.color = baseColor;
            StartCoroutine(FadeAndDestroy(slashObj, sr, 0.14f));
        }

        private void SpawnResonanceWaveVisual(Vector2 pos, Vector2 size, float dir)
        {
            EnsureSlashSprite();

            GameObject waveObj = new GameObject("Resonance_Wave");
            waveObj.transform.position = pos;
            waveObj.transform.localScale = new Vector3(Mathf.Abs(size.x) * dir, size.y, 1f);

            var sr = waveObj.AddComponent<SpriteRenderer>();
            sr.sprite = slashSprite;
            sr.sortingOrder = 10;
            sr.color = new Color(1.0f, 0.85f, 0.2f, 0.95f); // Golden-Resonance
            StartCoroutine(FadeAndDestroy(waveObj, sr, 0.25f));
        }

        private IEnumerator FadeAndDestroy(GameObject obj, SpriteRenderer sr, float lifetime)
        {
            float elapsed = 0f;
            Color init = sr.color;
            while (elapsed < lifetime)
            {
                elapsed += Time.deltaTime;
                Color c = init;
                c.a = Mathf.Lerp(init.a, 0f, elapsed / lifetime);
                if (sr != null) sr.color = c;
                yield return null;
            }
            if (obj != null) Destroy(obj);
        }

        public void SetEnemyLayer(LayerMask mask)
        {
            enemyLayer = mask;
        }

        public void SetSlashSprite(Sprite sprite)
        {
            slashSprite = sprite;
        }
    }
}
