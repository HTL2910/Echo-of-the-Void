using System.Collections;
using UnityEngine;
using EchoOfTheVoid.Core;
using EchoOfTheVoid.Combat;

namespace EchoOfTheVoid.Enemies
{
    /// <summary>
    /// Fast patrolling hunter enemy (spec §5.1).
    /// Patrols at 4.0 tiles/s, accelerates to 6.5 tiles/s upon detecting player within 6.0 tiles.
    /// Equipped with Apex Pounce, Melee Claw Slash, and Phase Evade skills.
    /// </summary>
    public class VoidStrider : EnemyBase
    {
        public enum StriderState { Patrol, Charge, Pounce, ClawSlash }

        [Header("Strider Detection")]
        [SerializeField] private float edgeCheckDistance = 1.2f;
        [SerializeField] private LayerMask groundLayer = ~0;

        [Header("Hunter Skills")]
        [SerializeField] private bool enablePounceSkill = true;
        [SerializeField] private float pounceCooldown = 3.5f;
        [SerializeField] private float pounceMinDistance = 2.5f;
        [SerializeField] private float pounceMaxDistance = 5.0f;
        [SerializeField] private float pounceSpeed = 8.5f;
        [SerializeField] private float pounceJumpForce = 5.0f;

        [SerializeField] private bool enableClawSlash = true;
        [SerializeField] private float slashCooldown = 2.0f;
        [SerializeField] private float slashRange = 1.6f;
        [SerializeField] private int slashDamage = 18;

        [SerializeField] private bool enablePhaseEvade = true;
        [SerializeField] private float evadeCooldown = 4.0f;

        private StriderState _state = StriderState.Patrol;
        private float _moveDirection = 1f;
        private Transform _playerTransform;
        private EnemyAnimationDriver _animDriver;

        private float _pounceTimer;
        private float _slashTimer;
        private float _evadeTimer;
        private bool _isSkillActive;

        public StriderState CurrentState => _state;
        public float MoveDirection => _moveDirection;

        protected override void Awake()
        {
            base.Awake();
            _animDriver = GetComponent<EnemyAnimationDriver>();
            OnReset += HandleStriderReset;
            OnDamaged += HandleStriderDamaged;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            OnDamaged -= HandleStriderDamaged;
        }

        protected override void Start()
        {
            base.Start();
            if (groundLayer.value == 0 || groundLayer.value == ~0)
            {
                int neutralLayer = LayerMask.NameToLayer("Neutral");
                int primeLayer = LayerMask.NameToLayer("PrimeSolid");
                int mask = 0;
                if (neutralLayer != -1) mask |= (1 << neutralLayer);
                if (primeLayer != -1) mask |= (1 << primeLayer);
                if (mask == 0) mask = 1;
                groundLayer = mask;
            }

            FindPlayer();
            _pounceTimer = 1.0f;
            _slashTimer = 1.0f;
        }

        private void FindPlayer()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) _playerTransform = player.transform;
        }

        private void HandleStriderReset()
        {
            StopAllCoroutines();
            _isSkillActive = false;
            _state = StriderState.Patrol;
            _moveDirection = 1f;
            _pounceTimer = 1.0f;
            _slashTimer = 1.0f;
            _evadeTimer = 1.0f;
            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = false;
                UpdateVisualAffinity();
            }
        }

        private void HandleStriderDamaged(bool isDeflected, int damage)
        {
            if (isDead || isStunned || _isSkillActive || !enablePhaseEvade) return;

            // When hit from same realm, chance to phase evade backwards
            if (!isDeflected && _evadeTimer <= 0f)
            {
                StartCoroutine(PhaseEvadeRoutine());
            }
        }

        protected override void Update()
        {
            base.Update();
            if (isDead) return;

            if (isStunned)
            {
                if (_isSkillActive)
                {
                    StopAllCoroutines();
                    _isSkillActive = false;
                }
                if (_animDriver != null) _animDriver.SetMovement(0f, true);
                return;
            }

            if (_pounceTimer > 0f) _pounceTimer -= Time.deltaTime;
            if (_slashTimer > 0f) _slashTimer -= Time.deltaTime;
            if (_evadeTimer > 0f) _evadeTimer -= Time.deltaTime;

            if (_isSkillActive) return;

            if (_playerTransform == null) FindPlayer();

            float detectRadius = (enemyData != null) ? enemyData.DetectionRadius : 6.0f;
            if (_playerTransform != null)
            {
                float dist = Vector2.Distance(transform.position, _playerTransform.position);
                if (dist <= detectRadius)
                {
                    if (_state != StriderState.Charge)
                    {
                        _state = StriderState.Charge;
                        if (_animDriver != null) _animDriver.TriggerEnemyCharge();
                    }
                    _moveDirection = (_playerTransform.position.x > transform.position.x) ? 1f : -1f;
                    if (spriteRenderer != null) spriteRenderer.flipX = (_moveDirection < 0f);

                    // Skill: Melee Claw Slash when in close range
                    if (enableClawSlash && _slashTimer <= 0f && dist <= slashRange)
                    {
                        StartCoroutine(ClawSlashRoutine());
                        return;
                    }

                    // Skill: Apex Pounce when in jump range
                    if (enablePounceSkill && _pounceTimer <= 0f && dist >= pounceMinDistance && dist <= pounceMaxDistance)
                    {
                        StartCoroutine(PounceRoutine());
                        return;
                    }
                }
                else if (dist > detectRadius + 2.5f && _state == StriderState.Charge)
                {
                    _state = StriderState.Patrol;
                }
            }

            CheckEdgesAndWalls();

            if (_animDriver != null && !_isSkillActive)
            {
                _animDriver.SetMovement(Mathf.Abs(rb.linearVelocity.x), true);
            }
        }

        private void FixedUpdate()
        {
            if (isDead || isStunned || _isSkillActive) return;

            float speed = (_state == StriderState.Charge)
                ? (enemyData != null ? enemyData.AlertSpeed : 6.5f)
                : (enemyData != null ? enemyData.MoveSpeed : 4.0f);

            rb.linearVelocity = new Vector2(_moveDirection * speed, rb.linearVelocity.y);
        }

        private void CheckEdgesAndWalls()
        {
            Vector2 forwardCheckPos = (Vector2)transform.position + new Vector2(_moveDirection * 0.7f, -0.2f);
            RaycastHit2D groundHit = Physics2D.Raycast(forwardCheckPos, Vector2.down, edgeCheckDistance, groundLayer);

            Vector2 wallCheckPos = (Vector2)transform.position + new Vector2(_moveDirection * 0.6f, 0.2f);
            RaycastHit2D wallHit = Physics2D.Raycast(wallCheckPos, new Vector2(_moveDirection, 0f), 0.3f, groundLayer);

            if (groundHit.collider == null || wallHit.collider != null)
            {
                _moveDirection = -_moveDirection;
                if (spriteRenderer != null) spriteRenderer.flipX = (_moveDirection < 0f);
            }
        }

        private IEnumerator PounceRoutine()
        {
            _isSkillActive = true;
            _state = StriderState.Pounce;
            _pounceTimer = pounceCooldown;

            // Telegraph crouch
            rb.linearVelocity = Vector2.zero;
            if (spriteRenderer != null) spriteRenderer.color = new Color(0.9f, 0.4f, 1f);
            yield return new WaitForSeconds(0.2f);

            if (isDead || isStunned)
            {
                _isSkillActive = false;
                yield break;
            }

            if (_animDriver != null) _animDriver.TriggerEnemyAttack();

            // Pounce leap
            rb.linearVelocity = new Vector2(_moveDirection * pounceSpeed, pounceJumpForce);
            yield return new WaitForSeconds(0.4f);

            // Wait for landing
            float timer = 1.0f;
            while (timer > 0f)
            {
                if (isDead || isStunned)
                {
                    _isSkillActive = false;
                    yield break;
                }
                RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 0.4f, groundLayer);
                if (hit.collider != null && rb.linearVelocity.y <= 0.1f) break;
                timer -= Time.deltaTime;
                yield return null;
            }

            if (spriteRenderer != null) UpdateVisualAffinity();
            yield return new WaitForSeconds(0.15f);

            _isSkillActive = false;
            _state = StriderState.Charge;
        }

        private IEnumerator ClawSlashRoutine()
        {
            _isSkillActive = true;
            _state = StriderState.ClawSlash;
            _slashTimer = slashCooldown;

            rb.linearVelocity = Vector2.zero;
            if (spriteRenderer != null) spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.15f);

            if (isDead || isStunned)
            {
                _isSkillActive = false;
                yield break;
            }

            if (_animDriver != null) _animDriver.TriggerEnemyAttack();

            // Forward thrust slash
            rb.linearVelocity = new Vector2(_moveDirection * 4f, rb.linearVelocity.y);

            // Hit detection in front
            Vector2 slashOrigin = (Vector2)transform.position + new Vector2(_moveDirection * 0.7f, 0f);
            Collider2D[] hits = Physics2D.OverlapCircleAll(slashOrigin, 1.0f);
            foreach (var hit in hits)
            {
                if (hit != null && hit.CompareTag("Player"))
                {
                    var damageable = hit.GetComponentInParent<IDamageable>();
                    if (damageable != null)
                    {
                        Vector2 knockback = new Vector2(_moveDirection * 6f, 2.5f);
                        DamageInfo info = new DamageInfo(slashDamage, slashOrigin, knockback, EntityRealm, attacker: gameObject);
                        damageable.TakeDamage(info);
                    }
                }
            }

            yield return new WaitForSeconds(0.25f);
            if (spriteRenderer != null) UpdateVisualAffinity();

            _isSkillActive = false;
            _state = StriderState.Charge;
        }

        private IEnumerator PhaseEvadeRoutine()
        {
            _isSkillActive = true;
            _evadeTimer = evadeCooldown;

            // Flash transparent
            if (spriteRenderer != null) spriteRenderer.color = new Color(1f, 1f, 1f, 0.35f);

            // Step back
            rb.linearVelocity = new Vector2(-_moveDirection * 5.0f, 1.5f);
            yield return new WaitForSeconds(0.25f);

            rb.linearVelocity = Vector2.zero;
            if (spriteRenderer != null) UpdateVisualAffinity();

            _isSkillActive = false;
            _state = StriderState.Charge;
        }
    }
}
