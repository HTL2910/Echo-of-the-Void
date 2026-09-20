using System.Collections;
using UnityEngine;
using EchoOfTheVoid.Core;

namespace EchoOfTheVoid.Enemies
{
    public class VoidWeaver : EnemyBase
    {
        [Header("Flying & Shooting")]
        [SerializeField] private float preferredDistance = 6.0f;
        [SerializeField] private float retreatDistance = 3.0f;
        [SerializeField] private float shootInterval = 3.0f;
        [SerializeField] private float chargeDuration = 0.8f;
        [SerializeField] private Sprite projectileSprite;

        private Transform _playerTransform;
        private float _shootTimer;
        private bool _isCharging;

        protected override void Start()
        {
            base.Start();
            customRealm = RealmType.Echo;
            rb.gravityScale = 0f; // Flying enemy

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) _playerTransform = player.transform;
            _shootTimer = shootInterval * 0.5f;
        }

        private void Update()
        {
            if (isDead) return;

            if (_playerTransform == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) _playerTransform = player.transform;
                return;
            }

            Vector2 toPlayer = _playerTransform.position - transform.position;
            float dist = toPlayer.magnitude;

            // Maintain distance
            Vector2 targetVel = Vector2.zero;
            float speed = (enemyData != null) ? enemyData.MoveSpeed : 2.5f;

            if (dist < retreatDistance)
            {
                // Retreat away from player
                targetVel = -toPlayer.normalized * speed;
            }
            else if (dist > preferredDistance + 1.5f)
            {
                // Approach player gently
                targetVel = toPlayer.normalized * speed;
            }
            else
            {
                // Hover slight float
                targetVel = new Vector2(0f, Mathf.Sin(Time.time * 2f) * 0.5f);
            }

            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, targetVel, Time.deltaTime * 3f);

            // Shooting logic
            _shootTimer -= Time.deltaTime;
            if (_shootTimer <= 0f && !_isCharging)
            {
                StartCoroutine(ShootRoutine(toPlayer.normalized));
            }
        }

        private IEnumerator ShootRoutine(Vector2 dir)
        {
            _isCharging = true;

            // Flash telegraph
            if (spriteRenderer != null) spriteRenderer.color = Color.white;
            yield return new WaitForSeconds(chargeDuration);
            if (spriteRenderer != null) UpdateVisualAffinity();

            // Spawn projectile
            GameObject projObj = new GameObject("EchoProjectile");
            projObj.transform.position = transform.position;
            projObj.transform.localScale = new Vector3(0.5f, 0.5f, 1f);

            var sr = projObj.AddComponent<SpriteRenderer>();
            sr.sprite = projectileSprite ?? spriteRenderer?.sprite;
            sr.color = new Color(0.85f, 0.2f, 1.0f, 1f);

            var proj = projObj.AddComponent<EchoProjectile>();
            proj.Initialize(dir);

            _shootTimer = shootInterval;
            _isCharging = false;
        }
    }
}
