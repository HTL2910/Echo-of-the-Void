using System.Collections;
using UnityEngine;
using EchoOfTheVoid.Core;
using EchoOfTheVoid.Combat;

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

        [Header("Advanced Void Skills")]
        [SerializeField] private bool enableSpreadVolley = true;
        [SerializeField] private float spreadAngle = 18f;
        [SerializeField] private bool enableVoidMine = true;
        [SerializeField] private float mineCooldown = 5.0f;
        [SerializeField] private bool enablePhaseBlink = true;
        [SerializeField] private float blinkCooldown = 6.0f;
        [SerializeField] private float blinkDistance = 4.0f;

        private Transform _playerTransform;
        private float _shootTimer;
        private float _mineTimer;
        private float _blinkTimer;
        private int _shotCount;
        private bool _isCharging;
        private bool _isBlinking;
        private EnemyAnimationDriver _animDriver;

        public bool IsCharging => _isCharging;
        public int ShotCount => _shotCount;

        protected override void Awake()
        {
            base.Awake();
            _animDriver = GetComponent<EnemyAnimationDriver>();
            OnReset += HandleWeaverReset;
        }

        protected override void Start()
        {
            base.Start();
            customRealm = RealmType.Echo;
            rb.gravityScale = 0f; // Flying enemy

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) _playerTransform = player.transform;
            _shootTimer = shootInterval * 0.5f;
            _mineTimer = 2.0f;
            _blinkTimer = 1.0f;
        }

        private void HandleWeaverReset()
        {
            StopAllCoroutines();
            _isCharging = false;
            _isBlinking = false;
            _shootTimer = shootInterval * 0.5f;
            _mineTimer = 2.0f;
            _blinkTimer = 1.0f;
            _shotCount = 0;
            rb.linearVelocity = Vector2.zero;
            if (spriteRenderer != null) UpdateVisualAffinity();
        }

        protected override void Update()
        {
            base.Update();
            if (isDead) return;

            if (isStunned)
            {
                if (_isCharging || _isBlinking)
                {
                    StopAllCoroutines();
                    _isCharging = false;
                    _isBlinking = false;
                }
                rb.linearVelocity = Vector2.zero;
                if (_animDriver != null) _animDriver.SetMovement(0f, false);
                return;
            }

            if (_mineTimer > 0f) _mineTimer -= Time.deltaTime;
            if (_blinkTimer > 0f) _blinkTimer -= Time.deltaTime;

            if (_isBlinking) return;

            if (_playerTransform == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) _playerTransform = player.transform;
                return;
            }

            Vector2 toPlayer = _playerTransform.position - transform.position;
            float dist = toPlayer.magnitude;

            // Skill: Phase Blink if player gets within dangerous point-blank range
            if (enablePhaseBlink && _blinkTimer <= 0f && dist < 2.0f)
            {
                StartCoroutine(PhaseBlinkRoutine(-toPlayer.normalized));
                return;
            }

            // Skill: Deploy Void Mine when retreating
            if (enableVoidMine && _mineTimer <= 0f && dist < retreatDistance)
            {
                DeployVoidMine();
            }

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

            if (_animDriver != null)
            {
                _animDriver.SetMovement(rb.linearVelocity.magnitude, false);
            }

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
            if (_animDriver != null) _animDriver.TriggerEnemyCharge();

            // Flash telegraph
            if (spriteRenderer != null) spriteRenderer.color = Color.white;
            yield return new WaitForSeconds(chargeDuration);
            if (spriteRenderer != null) UpdateVisualAffinity();

            if (isDead || isStunned)
            {
                _isCharging = false;
                yield break;
            }

            if (_animDriver != null) _animDriver.TriggerEnemyAttack();

            _shotCount++;

            // Skill 1: Alternating spread shot volley vs single projectile
            if (enableSpreadVolley && (_shotCount % 2 == 0))
            {
                // Tri-spread volley (-spreadAngle, 0, +spreadAngle)
                SpawnProjectile(dir);
                SpawnProjectile(Quaternion.Euler(0, 0, spreadAngle) * dir);
                SpawnProjectile(Quaternion.Euler(0, 0, -spreadAngle) * dir);
            }
            else
            {
                // Single focused projectile
                SpawnProjectile(dir);
            }

            _shootTimer = shootInterval;
            _isCharging = false;
        }

        private void SpawnProjectile(Vector2 dir)
        {
            GameObject projObj = new GameObject("EchoProjectile");
            projObj.transform.position = transform.position;
            projObj.transform.localScale = new Vector3(0.5f, 0.5f, 1f);

            var sr = projObj.AddComponent<SpriteRenderer>();
            sr.sprite = projectileSprite ?? GetOrCreateCircleSprite();
            sr.color = new Color(0.85f, 0.2f, 1.0f, 1f);
            sr.sortingOrder = 5; // Render on top of everything

            var proj = projObj.AddComponent<EchoProjectile>();
            proj.Initialize(dir);
        }

        private void DeployVoidMine()
        {
            _mineTimer = mineCooldown;

            GameObject mineObj = new GameObject("VoidMine");
            mineObj.transform.position = transform.position;
            mineObj.transform.localScale = new Vector3(0.7f, 0.7f, 1f);

            var sr = mineObj.AddComponent<SpriteRenderer>();
            sr.sprite = projectileSprite ?? GetOrCreateCircleSprite();
            sr.color = new Color(0.7f, 0.1f, 0.9f, 0.85f);
            sr.sortingOrder = 5;

            // Mine drifts slightly then remains stationary
            var proj = mineObj.AddComponent<EchoProjectile>();
            proj.Initialize(Vector2.zero);
        }

        // Fallback: generate a white filled circle sprite at runtime so projectiles are always visible
        private static Sprite _cachedCircleSprite;
        private static Sprite GetOrCreateCircleSprite()
        {
            if (_cachedCircleSprite != null) return _cachedCircleSprite;
            const int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            float center = size / 2f;
            float radius = size / 2f - 1f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dist = Mathf.Sqrt((x - center) * (x - center) + (y - center) * (y - center));
                    float alpha = Mathf.Clamp01(1f - (dist - radius + 1.5f));
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            tex.Apply();
            _cachedCircleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            return _cachedCircleSprite;
        }

        private IEnumerator PhaseBlinkRoutine(Vector2 escapeDir)
        {
            _isBlinking = true;
            _blinkTimer = blinkCooldown;
            rb.linearVelocity = Vector2.zero;

            // Fade out
            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(0.6f, 0.2f, 0.9f, 0.25f);
            }
            yield return new WaitForSeconds(0.15f);

            // Teleport
            transform.position += (Vector3)(escapeDir * blinkDistance);

            // Fade in
            yield return new WaitForSeconds(0.1f);
            if (spriteRenderer != null) UpdateVisualAffinity();

            _isBlinking = false;
        }
    }
}
