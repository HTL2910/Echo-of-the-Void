using System.Collections;
using UnityEngine;
using EchoOfTheVoid.Core;
using EchoOfTheVoid.Combat;

namespace EchoOfTheVoid.Enemies
{
    /// <summary>
    /// Stationary turret enemy mounted on ceiling/wall (spec §5.1, §3.2).
    /// Fires an energy beam every 2.5s (20 dmg).
    /// In Prime Realm: Beam damages player.
    /// In Echo Realm: Beam transforms into a usable RailCable for grinding.
    /// </summary>
    public class PrismSentry : EnemyBase
    {
        [Header("Beam Configuration")]
        [SerializeField] private Vector2 beamDirection = Vector2.down;
        [SerializeField] private float beamLength = 8.0f;
        [SerializeField] private float fireInterval = 2.5f;
        [SerializeField] private float beamDuration = 1.0f;
        [SerializeField] private int beamDamage = 20;

        [Header("Components")]
        [SerializeField] private RailCable railCable;
        [SerializeField] private LineRenderer beamLineRenderer;

        private float _fireTimer;
        private bool _isFiring;
        private LayerMask _hitMask;

        public bool IsFiring => _isFiring;
        public RailCable Cable => railCable;

        protected override void Awake()
        {
            base.Awake();
            if (rb != null)
            {
                rb.gravityScale = 0f;
                rb.linearVelocity = Vector2.zero;
            }

            if (railCable == null)
            {
                var cableGo = new GameObject("RailCable");
                cableGo.transform.SetParent(transform);
                cableGo.AddComponent<BoxCollider2D>();
                railCable = cableGo.AddComponent<RailCable>();
            }

            _hitMask = LayerMask.GetMask("Player", "Neutral", "PrimeSolid", "EchoSolid");
            if (_hitMask == 0) _hitMask = ~0;
        }

        protected override void Start()
        {
            base.Start();
            customRealm = RealmType.Echo;
            _fireTimer = fireInterval;
            UpdateBeamState();
        }

        protected override void Update()
        {
            base.Update();
            if (isDead)
            {
                DisableBeam();
                return;
            }

            if (isStunned)
            {
                DisableBeam();
                return;
            }

            _fireTimer -= Time.deltaTime;
            if (_fireTimer <= 0f && !_isFiring)
            {
                StartCoroutine(FireBeamRoutine());
            }

            if (_isFiring)
            {
                UpdateBeamBehavior();
            }
        }

        protected override void HandleRealmSwitch(RealmType currentRealm)
        {
            base.HandleRealmSwitch(currentRealm);
            if (_isFiring)
            {
                UpdateBeamState();
            }
        }

        private IEnumerator FireBeamRoutine()
        {
            _isFiring = true;
            UpdateBeamState();

            yield return new WaitForSeconds(beamDuration);

            DisableBeam();
            _fireTimer = fireInterval;
            _isFiring = false;
        }

        private void UpdateBeamBehavior()
        {
            Vector2 start = transform.position;
            Vector2 dir = beamDirection.normalized;
            Vector2 end = start + dir * beamLength;

            RaycastHit2D hit = Physics2D.Raycast(start, dir, beamLength, _hitMask);
            if (hit.collider != null)
            {
                end = hit.point;
            }

            RealmType currentRealm = RealityManager.Instance != null 
                ? RealityManager.Instance.CurrentRealm 
                : RealmType.Prime;

            if (currentRealm == RealmType.Prime)
            {
                // Damage player in Prime
                if (hit.collider != null && hit.collider.CompareTag("Player"))
                {
                    var damageable = hit.collider.GetComponentInParent<IDamageable>();
                    if (damageable != null)
                    {
                        DamageInfo info = new DamageInfo(beamDamage, hit.point, dir * 5f, RealmType.Echo, attacker: gameObject);
                        damageable.TakeDamage(info);
                    }
                }
            }
            else
            {
                // In Echo Realm, cable is active and harmless
                if (railCable != null)
                {
                    railCable.SetupCable(start, end);
                    railCable.SetActive(true);
                }
            }
        }

        private void UpdateBeamState()
        {
            if (!_isFiring)
            {
                DisableBeam();
                return;
            }

            RealmType currentRealm = RealityManager.Instance != null 
                ? RealityManager.Instance.CurrentRealm 
                : RealmType.Prime;

            if (currentRealm == RealmType.Echo)
            {
                if (railCable != null) railCable.SetActive(true);
            }
            else
            {
                if (railCable != null) railCable.SetActive(false);
            }
        }

        private void DisableBeam()
        {
            if (railCable != null) railCable.SetActive(false);
        }
    }
}
