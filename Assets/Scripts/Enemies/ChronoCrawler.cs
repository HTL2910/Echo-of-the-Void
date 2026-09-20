using System.Collections;
using UnityEngine;
using EchoOfTheVoid.Core;

namespace EchoOfTheVoid.Enemies
{
    public class ChronoCrawler : EnemyBase
    {
        private enum CrawlerState { Patrol, Alert, Charge }

        [Header("Patrol & Detection")]
        [SerializeField] private float edgeCheckDistance = 1.0f;
        [SerializeField] private LayerMask groundLayer = ~0;

        private CrawlerState _state = CrawlerState.Patrol;
        private float _moveDirection = 1f;
        private Transform _playerTransform;
        private float _alertTimer;

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

        private void Update()
        {
            if (isDead) return;

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
        }

        private void FixedUpdate()
        {
            if (isDead) return;

            float speed = (_state == CrawlerState.Charge) 
                ? (enemyData != null ? enemyData.AlertSpeed : 5.0f) 
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
            // Check edge and wall
            Vector2 forwardCheckPos = (Vector2)transform.position + new Vector2(_moveDirection * 0.6f, -0.2f);
            RaycastHit2D groundHit = Physics2D.Raycast(forwardCheckPos, Vector2.down, edgeCheckDistance, groundLayer);

            Vector2 wallCheckPos = (Vector2)transform.position + new Vector2(_moveDirection * 0.5f, 0.2f);
            RaycastHit2D wallHit = Physics2D.Raycast(wallCheckPos, new Vector2(_moveDirection, 0f), 0.3f, groundLayer);

            if (groundHit.collider == null || wallHit.collider != null)
            {
                _moveDirection = -_moveDirection;
                if (spriteRenderer != null) spriteRenderer.flipX = (_moveDirection < 0f);
            }

            // Check player proximity
            if (_playerTransform != null)
            {
                float dist = Vector2.Distance(transform.position, _playerTransform.position);
                float detectRadius = (enemyData != null) ? enemyData.DetectionRadius : 5.0f;
                if (dist <= detectRadius)
                {
                    _state = CrawlerState.Alert;
                    _alertTimer = 0.3f; // GDD: pauses 0.3s on alert
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
                if (_playerTransform != null)
                {
                    _moveDirection = (_playerTransform.position.x > transform.position.x) ? 1f : -1f;
                    if (spriteRenderer != null) spriteRenderer.flipX = (_moveDirection < 0f);
                }
            }
        }

        private void UpdateCharge()
        {
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
            }
        }
    }
}
