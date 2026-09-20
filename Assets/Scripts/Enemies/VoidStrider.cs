using UnityEngine;
using EchoOfTheVoid.Core;

namespace EchoOfTheVoid.Enemies
{
    /// <summary>
    /// Fast patrolling hunter enemy (spec §5.1).
    /// Patrols at 4.0 tiles/s, accelerates to 6.5 tiles/s upon detecting player within 6.0 tiles.
    /// </summary>
    public class VoidStrider : EnemyBase
    {
        public enum StriderState { Patrol, Charge }

        [Header("Strider Detection")]
        [SerializeField] private float edgeCheckDistance = 1.2f;
        [SerializeField] private LayerMask groundLayer = ~0;

        private StriderState _state = StriderState.Patrol;
        private float _moveDirection = 1f;
        private Transform _playerTransform;
        private EnemyAnimationDriver _animDriver;

        public StriderState CurrentState => _state;
        public float MoveDirection => _moveDirection;

        protected override void Awake()
        {
            base.Awake();
            _animDriver = GetComponent<EnemyAnimationDriver>();
            OnReset += HandleStriderReset;
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
        }

        private void FindPlayer()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) _playerTransform = player.transform;
        }

        private void HandleStriderReset()
        {
            _state = StriderState.Patrol;
            _moveDirection = 1f;
            if (spriteRenderer != null) spriteRenderer.flipX = false;
        }

        protected override void Update()
        {
            base.Update();
            if (isDead) return;

            if (isStunned)
            {
                if (_animDriver != null) _animDriver.SetMovement(0f, true);
                return;
            }

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
                }
                else if (dist > detectRadius + 2.5f && _state == StriderState.Charge)
                {
                    _state = StriderState.Patrol;
                }
            }

            CheckEdgesAndWalls();

            if (_animDriver != null)
            {
                _animDriver.SetMovement(Mathf.Abs(rb.linearVelocity.x), true);
            }
        }

        private void FixedUpdate()
        {
            if (isDead || isStunned) return;

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
    }
}
