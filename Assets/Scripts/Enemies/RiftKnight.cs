using System.Collections;
using UnityEngine;
using EchoOfTheVoid.Combat;
using EchoOfTheVoid.Core;

namespace EchoOfTheVoid.Enemies
{
    /// <summary>
    /// Heavy enemy whose shield blocks attacks coming from the direction it faces.
    /// </summary>
    public class RiftKnight : EnemyBase
    {
        public enum KnightState { Idle, Advance, Slash, Stomp }

        private float _facingDirection = 1f;
        private int _connectedHitCount;
        private float _realmSwitchTimer;
        private Transform _playerTransform;
        private KnightState _state = KnightState.Idle;
        private EnemyAnimationDriver _animDriver;
        private bool _isAttacking;
        private float _slashCooldownTimer;
        private float _stompCooldownTimer;

        public float FacingDirection => _facingDirection;
        public KnightState CurrentState => _state;
        private RiftKnightDataSO RiftData => enemyData as RiftKnightDataSO;
        private float RealmSwitchInterval => RiftData != null ? RiftData.RealmSwitchInterval : 4f;
        private int HitsBeforeRealmSwitch => RiftData != null ? RiftData.HitsBeforeRealmSwitch : 3;
        private float SlashRange => RiftData != null ? RiftData.SlashRange : 1.6f;
        private int SlashDamage => RiftData != null ? RiftData.SlashDamage : 25;
        private float AttackCooldown => RiftData != null ? RiftData.AttackCooldown : 1.5f;
        private float StompRange => RiftData != null ? RiftData.StompRange : 4f;
        private int StompDamage => RiftData != null ? RiftData.StompDamage : 35;
        private float ShockwaveHeight => RiftData != null ? RiftData.ShockwaveHeight : 1.2f;
        private float StompCooldown => RiftData != null ? RiftData.StompCooldown : 4f;

        protected override void Awake()
        {
            overrideDataRealm = true;
            base.Awake();
            _animDriver = GetComponent<EnemyAnimationDriver>();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        protected override void Update()
        {
            base.Update();
            if (isDead) return;

            _realmSwitchTimer += Time.deltaTime;
            if (_slashCooldownTimer > 0f) _slashCooldownTimer -= Time.deltaTime;
            if (_stompCooldownTimer > 0f) _stompCooldownTimer -= Time.deltaTime;
            if (_realmSwitchTimer >= RealmSwitchInterval)
                SwitchRealm();

            if (_playerTransform == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) _playerTransform = player.transform;
            }

            if (_playerTransform != null)
            {
                _facingDirection = _playerTransform.position.x < transform.position.x ? -1f : 1f;
                if (spriteRenderer != null) spriteRenderer.flipX = _facingDirection < 0f;

                float distance = Vector2.Distance(transform.position, _playerTransform.position);
                if (!_isAttacking && !isStunned && _slashCooldownTimer <= 0f && distance <= SlashRange)
                    StartCoroutine(SlashRoutine());
                else if (!_isAttacking && !isStunned && _stompCooldownTimer <= 0f && distance <= StompRange)
                    StartCoroutine(StompRoutine());
            }
        }

        private void FixedUpdate()
        {
            if (isDead || isStunned || _isAttacking || _playerTransform == null) return;

            float detectionRadius = enemyData != null ? enemyData.DetectionRadius : 6f;
            float distance = Vector2.Distance(transform.position, _playerTransform.position);
            if (distance <= detectionRadius)
            {
                _state = KnightState.Advance;
                float speed = enemyData != null ? enemyData.MoveSpeed : 3.8f;
                rb.linearVelocity = new Vector2(_facingDirection * speed, rb.linearVelocity.y);
            }
            else
            {
                _state = KnightState.Idle;
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            }
        }

        private IEnumerator SlashRoutine()
        {
            _isAttacking = true;
            _state = KnightState.Slash;
            _slashCooldownTimer = AttackCooldown;
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

            yield return new WaitForSeconds(0.2f);
            if (isDead || isStunned)
            {
                _isAttacking = false;
                yield break;
            }

            if (_animDriver != null) _animDriver.TriggerEnemyAttack();
            Vector2 origin = (Vector2)transform.position + Vector2.right * (_facingDirection * 0.7f);
            foreach (Collider2D hit in Physics2D.OverlapCircleAll(origin, SlashRange))
            {
                if (hit == null || !hit.CompareTag("Player")) continue;
                IDamageable target = hit.GetComponentInParent<IDamageable>();
                if (target == null) continue;

                target.TakeDamage(new DamageInfo(
                    SlashDamage,
                    origin,
                    new Vector2(_facingDirection * 6f, 2f),
                    EntityRealm,
                    attacker: gameObject));
                break;
            }

            yield return new WaitForSeconds(0.2f);
            _isAttacking = false;
            _state = KnightState.Advance;
        }

        private IEnumerator StompRoutine()
        {
            _isAttacking = true;
            _state = KnightState.Stomp;
            _stompCooldownTimer = StompCooldown;
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

            yield return new WaitForSeconds(0.3f);
            if (isDead || isStunned)
            {
                _isAttacking = false;
                yield break;
            }

            if (_animDriver != null) _animDriver.TriggerEnemyAttack();
            Vector2 center = (Vector2)transform.position + Vector2.right * (_facingDirection * StompRange * 0.5f);
            Vector2 size = new Vector2(StompRange, ShockwaveHeight);
            foreach (Collider2D hit in Physics2D.OverlapBoxAll(center, size, 0f))
            {
                if (hit == null || !hit.CompareTag("Player")) continue;
                IDamageable target = hit.GetComponentInParent<IDamageable>();
                if (target == null) continue;

                target.TakeDamage(new DamageInfo(
                    StompDamage,
                    hit.ClosestPoint(transform.position),
                    new Vector2(_facingDirection * 8f, 3f),
                    EntityRealm,
                    attacker: gameObject));
                break;
            }

            yield return new WaitForSeconds(0.25f);
            _isAttacking = false;
            _state = KnightState.Advance;
        }

        public override HitFeedback TakeDamage(DamageInfo info)
        {
            Vector2 source = info.Attacker != null
                ? info.Attacker.transform.position
                : info.HitPoint;
            float directionToAttack = source.x - transform.position.x;

            if (directionToAttack * _facingDirection > 0f)
                return new HitFeedback(isDeflected: true, dealtDamage: 0, isDead: false);

            HitFeedback feedback = base.TakeDamage(info);
            if (feedback.DealtDamage > 0 && !feedback.IsDead)
            {
                _connectedHitCount++;
                if (_connectedHitCount >= HitsBeforeRealmSwitch)
                    SwitchRealm();
            }

            return feedback;
        }

        private void SwitchRealm()
        {
            customRealm = customRealm == RealmType.Prime
                ? RealmType.Echo
                : RealmType.Prime;
            _connectedHitCount = 0;
            _realmSwitchTimer = 0f;
            UpdateVisualAffinity();
        }

        public override void ResetEnemyState()
        {
            customRealm = RealmType.Prime;
            _connectedHitCount = 0;
            _realmSwitchTimer = 0f;
            _playerTransform = null;
            _state = KnightState.Idle;
            _isAttacking = false;
            _slashCooldownTimer = 0f;
            _stompCooldownTimer = 0f;
            base.ResetEnemyState();
        }

    }
}
