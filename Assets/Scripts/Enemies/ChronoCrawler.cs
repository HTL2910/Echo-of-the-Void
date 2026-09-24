using System.Collections;
using UnityEngine;
using EchoOfTheVoid.Core;
using EchoOfTheVoid.Combat;

namespace EchoOfTheVoid.Enemies
{
    public class ChronoCrawler : EnemyBase
    {
        public enum CrawlerState { Patrol, Alert, Charge, Leap, Tremor }

        [Header("Patrol & Detection")]
        [SerializeField] private float edgeCheckDistance = 1.0f;
        [SerializeField] private LayerMask groundLayer = ~0;

        [Header("Advanced Chrono Skills")]
        [SerializeField] private bool enableLeapSkill = true;
        [SerializeField] private float leapCooldown = 4.0f;
        [SerializeField] private float leapMinDistance = 2.5f;
        [SerializeField] private float leapMaxDistance = 5.5f;
        [SerializeField] private float leapHorizontalSpeed = 6.5f;
        [SerializeField] private float leapVerticalSpeed = 5.5f;

        [SerializeField] private bool enableTremorSkill = true;
        [SerializeField] private float tremorCooldown = 3.5f;
        [SerializeField] private float tremorRadius = 2.0f;
        [SerializeField] private int tremorDamage = 15;

        private CrawlerState _state = CrawlerState.Patrol;
        private float _moveDirection = 1f;
        private Transform _playerTransform;
        private float _alertTimer;
        private EnemyAnimationDriver _animDriver;

        private float _leapTimer;
        private float _tremorTimer;
        private bool _isSkillActive;

        public CrawlerState CurrentState => _state;
        public float MoveDirection => _moveDirection;

        protected override void Awake()
        {
            base.Awake();
            _animDriver = GetComponent<EnemyAnimationDriver>();
            OnReset += HandleCrawlerReset;
        }

        protected override void Start()
        {
            base.Start();
            customRealm = RealmType.Prime;

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

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) _playerTransform = player.transform;
        }

        private void HandleCrawlerReset()
        {
            StopAllCoroutines();
            _isSkillActive = false;
            _state = CrawlerState.Patrol;
            _moveDirection = 1f;
            _leapTimer = 1.0f;
            _tremorTimer = 1.5f;
            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = false;
                UpdateVisualAffinity();
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

            if (_leapTimer > 0f) _leapTimer -= Time.deltaTime;
            if (_tremorTimer > 0f) _tremorTimer -= Time.deltaTime;

            if (_isSkillActive) return;

            switch (_state)
            {
                case CrawlerState.Patrol:
                    UpdatePatrol();
                    break;
                case CrawlerState.Alert:
                    UpdateAlert();
                    break;
                case CrawlerState.Charge:
                    UpdateCharge();
                    break;
            }

            if (_animDriver != null && !_isSkillActive)
            {
                float currentSpeed = Mathf.Abs(rb.linearVelocity.x);
                _animDriver.SetMovement(currentSpeed, true);
            }
        }

        private void FixedUpdate()
        {
            if (isDead || isStunned || _isSkillActive) return;

            // Enrage speed boost below 50% HP
            float enrageMultiplier = (currentHealth <= ((enemyData != null) ? enemyData.MaxHealth : 50) * 0.5f) ? 1.25f : 1.0f;

            float speed = (_state == CrawlerState.Charge) 
                ? (enemyData != null ? enemyData.AlertSpeed : 5.0f) * enrageMultiplier
                : (enemyData != null ? enemyData.MoveSpeed : 3.2f);

            if (_state != CrawlerState.Alert)
            {
                rb.linearVelocity = new Vector2(_moveDirection * speed, rb.linearVelocity.y);
            }
            else
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            }
        }

        private void UpdatePatrol()
        {
            CheckEdgesAndWalls();

            // Check player proximity
            if (_playerTransform != null)
            {
                float dist = Vector2.Distance(transform.position, _playerTransform.position);
                float detectRadius = (enemyData != null) ? enemyData.DetectionRadius : 5.0f;
                if (dist <= detectRadius)
                {
                    _state = CrawlerState.Alert;
                    _alertTimer = 0.3f; // GDD: pauses 0.3s on alert
                    if (_animDriver != null) _animDriver.TriggerEnemyAlert();
                    if (spriteRenderer != null) spriteRenderer.color = Color.red;
                }
            }
        }

        private void UpdateAlert()
        {
            _alertTimer -= Time.deltaTime;
            if (_alertTimer <= 0f)
            {
                _state = CrawlerState.Charge;
                if (_animDriver != null) _animDriver.TriggerEnemyCharge();
                if (_playerTransform != null)
                {
                    _moveDirection = (_playerTransform.position.x > transform.position.x) ? 1f : -1f;
                    if (spriteRenderer != null) spriteRenderer.flipX = (_moveDirection < 0f);
                }
            }
        }

        private void UpdateCharge()
        {
            CheckEdgesAndWalls();

            if (_playerTransform == null)
            {
                _state = CrawlerState.Patrol;
                UpdateVisualAffinity();
                return;
            }

            float dist = Vector2.Distance(transform.position, _playerTransform.position);
            if (dist > 8f) // Lost player
            {
                _state = CrawlerState.Patrol;
                UpdateVisualAffinity();
                return;
            }

            // Skill 1: Ground Tremor when player is too close
            if (enableTremorSkill && _tremorTimer <= 0f && dist <= 1.8f)
            {
                StartCoroutine(TremorRoutine());
                return;
            }

            // Skill 2: Temporal Leap Pounce when player is at jump range
            if (enableLeapSkill && _leapTimer <= 0f && dist >= leapMinDistance && dist <= leapMaxDistance)
            {
                StartCoroutine(LeapRoutine());
                return;
            }
        }

        private void CheckEdgesAndWalls()
        {
            Vector2 forwardCheckPos = (Vector2)transform.position + new Vector2(_moveDirection * 0.6f, -0.2f);
            RaycastHit2D groundHit = Physics2D.Raycast(forwardCheckPos, Vector2.down, edgeCheckDistance, groundLayer);
            Vector2 wallCheckPos = (Vector2)transform.position + new Vector2(_moveDirection * 0.5f, 0.2f);
            RaycastHit2D wallHit = Physics2D.Raycast(wallCheckPos, new Vector2(_moveDirection, 0f), 0.3f, groundLayer);

            if (groundHit.collider == null || wallHit.collider != null)
            {
                _moveDirection = -_moveDirection;
                if (spriteRenderer != null) spriteRenderer.flipX = (_moveDirection < 0f);
            }
        }

        private IEnumerator LeapRoutine()
        {
            _isSkillActive = true;
            _state = CrawlerState.Leap;
            _leapTimer = leapCooldown;

            // Telegraph windup
            rb.linearVelocity = Vector2.zero;
            if (spriteRenderer != null) spriteRenderer.color = new Color(1f, 0.8f, 0.2f);
            if (_animDriver != null) _animDriver.TriggerEnemyAttack();
            yield return new WaitForSeconds(0.25f);

            if (isDead || isStunned)
            {
                _isSkillActive = false;
                yield break;
            }

            // Face player
            if (_playerTransform != null)
            {
                _moveDirection = (_playerTransform.position.x > transform.position.x) ? 1f : -1f;
                if (spriteRenderer != null) spriteRenderer.flipX = (_moveDirection < 0f);
            }

            // Launch leap
            rb.linearVelocity = new Vector2(_moveDirection * leapHorizontalSpeed, leapVerticalSpeed);
            if (_animDriver != null) _animDriver.SetMovement(leapHorizontalSpeed, false);

            yield return new WaitForSeconds(0.35f);

            // Wait until grounded or time elapsed
            float airborneTimer = 1.0f;
            while (airborneTimer > 0f)
            {
                if (isDead || isStunned)
                {
                    _isSkillActive = false;
                    yield break;
                }

                RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 0.4f, groundLayer);
                if (hit.collider != null && rb.linearVelocity.y <= 0.1f)
                {
                    break;
                }
                airborneTimer -= Time.deltaTime;
                yield return null;
            }

            // Land recovery
            rb.linearVelocity = Vector2.zero;
            if (spriteRenderer != null) UpdateVisualAffinity();
            yield return new WaitForSeconds(0.15f);

            _isSkillActive = false;
            _state = CrawlerState.Charge;
        }

        private IEnumerator TremorRoutine()
        {
            _isSkillActive = true;
            _state = CrawlerState.Tremor;
            _tremorTimer = tremorCooldown;

            rb.linearVelocity = Vector2.zero;
            if (_animDriver != null) _animDriver.TriggerEnemyAttack();

            // Telegraph pulse
            if (spriteRenderer != null) spriteRenderer.color = new Color(1f, 0.3f, 0.3f);
            yield return new WaitForSeconds(0.3f);

            if (isDead || isStunned)
            {
                _isSkillActive = false;
                yield break;
            }

            // Stomp AOE damage
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, tremorRadius);
            foreach (var hit in hits)
            {
                if (hit != null && hit.CompareTag("Player"))
                {
                    var damageable = hit.GetComponentInParent<IDamageable>();
                    if (damageable != null)
                    {
                        Vector2 knockbackDir = (hit.transform.position - transform.position).normalized + Vector3.up * 0.5f;
                        DamageInfo info = new DamageInfo(tremorDamage, transform.position, knockbackDir * 5f, EntityRealm, attacker: gameObject);
                        damageable.TakeDamage(info);
                    }
                }
            }

            if (spriteRenderer != null) UpdateVisualAffinity();
            yield return new WaitForSeconds(0.2f);

            _isSkillActive = false;
            _state = CrawlerState.Charge;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, tremorRadius);
        }
    }
}
