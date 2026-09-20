using System.Collections;
using UnityEngine;
using EchoOfTheVoid.Core;
using EchoOfTheVoid.Combat;
using EchoOfTheVoid.Player;

namespace EchoOfTheVoid.Bosses
{
    /// <summary>
    /// K4: Sentinel-01 boss (spec §5.2).
    /// HP 600, Poise 100. Upper body = Echo realm, legs/chains = Prime realm.
    /// FSM: Idle -> SweepKick -> MissileRain -> LaserSweep -> repeat.
    /// Overheat (Poise = 0): stunned 3.5s, +50% dmg. Under 40% HP: warning shrinks to 0.9s.
    /// </summary>
    public class Sentinel01 : BossBase
    {
        private enum SentinelState { Idle, SweepKick, MissileRain, LaserSweep, Overheat }

        [Header("Sentinel-01 References")]
        [SerializeField] private Transform missileSpawnPoint;
        [SerializeField] private GameObject missileWarningPrefab;
        [SerializeField] private Transform laserOrigin;

        [Header("Sentinel-01 Timings")]
        [SerializeField] private float laserDuration = 2.0f;

        private SentinelState _currentState = SentinelState.Idle;
        private Transform _playerTransform;

        protected override void Awake()
        {
            base.Awake();
            bossId = "sentinel_01";
            displayName = "SENTINEL-01";
            maxHp = 600;
            maxPoise = 100f;
            // Upper body (torso) = Echo, legs/chain = Prime
            myRealm = RealmType.Echo;
            currentHp = maxHp;
            currentPoise = maxPoise;
        }

        protected override void Start()
        {
            base.Start();
            FindPlayer();
        }

        private void FindPlayer()
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) _playerTransform = go.transform;
        }

        protected override IEnumerator RunPhaseRoutine(BossPhaseData phase)
        {
            // Sentinel-01 loops through its attack cycle
            // SweepKick
            _currentState = SentinelState.SweepKick;
            yield return DoSweepKick(phase);
            if (isDead) yield break;

            // MissileRain
            _currentState = SentinelState.MissileRain;
            yield return DoMissileRain(phase);
            if (isDead) yield break;

            // LaserSweep
            _currentState = SentinelState.LaserSweep;
            yield return DoLaserSweep(phase);
            if (isDead) yield break;

            // Brief idle between cycles
            _currentState = SentinelState.Idle;
            yield return new WaitForSeconds(1.0f);
        }

        private IEnumerator DoSweepKick(BossPhaseData phase)
        {
            // Spec: 360° sweep, player must jump over it
            float interval = (phase != null) ? phase.SweepKickInterval : 3.5f;
            int dmg = (phase != null) ? phase.SweepKickDamage : 18;

            // Windup
            yield return new WaitForSeconds(0.8f);
            if (isDead || isStunned) yield break;

            // Hitbox sweep — do a circular overlap
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 3.0f, LayerMask.GetMask("Player"));
            foreach (var hit in hits)
            {
                var damageable = hit.GetComponentInParent<IDamageable>();
                if (damageable == null) continue;
                Vector2 knockDir = (hit.transform.position - transform.position).normalized;
                DamageInfo info = new DamageInfo(dmg, hit.ClosestPoint(transform.position), knockDir * 10f, RealmType.Prime);
                damageable.TakeDamage(info);
            }

            yield return new WaitForSeconds(interval - 0.8f);
        }

        private IEnumerator DoMissileRain(BossPhaseData phase)
        {
            if (_playerTransform == null) FindPlayer();

            int missileCount = (phase != null) ? phase.MissileCount : 4;
            // Phase 2 shortens warning (under 40% HP)
            float warningDuration = (phase != null) ? phase.MissileWarningDuration : 1.2f;
            float hpFraction = (float)currentHp / maxHp;
            if (hpFraction < 0.4f) warningDuration = 0.9f;

            int dmg = (phase != null) ? phase.MissileImpactDamage : 22;

            for (int i = 0; i < missileCount; i++)
            {
                if (isDead || isStunned) yield break;

                // Pick a target position (spread around player)
                Vector2 targetPos = _playerTransform != null
                    ? (Vector2)_playerTransform.position + Random.insideUnitCircle * 1.5f
                    : (Vector2)transform.position + Vector2.right * (i * 2f - missileCount);

                // Spawn warning circle (visual only — Anti will skin it)
                if (missileWarningPrefab != null)
                    Object.Instantiate(missileWarningPrefab, targetPos, Quaternion.identity);

                yield return new WaitForSeconds(warningDuration);
                if (isDead) yield break;

                // Missile impact — overlap at target
                Collider2D[] hits = Physics2D.OverlapCircleAll(targetPos, 1.0f, LayerMask.GetMask("Player"));
                foreach (var hit in hits)
                {
                    var damageable = hit.GetComponentInParent<IDamageable>();
                    if (damageable == null) continue;
                    DamageInfo info = new DamageInfo(dmg, targetPos, Vector2.zero, RealmType.Echo);
                    damageable.TakeDamage(info);
                }

                yield return new WaitForSeconds(0.3f);
            }
        }

        private IEnumerator DoLaserSweep(BossPhaseData phase)
        {
            // Sweeps the floor; player must jump + dash over it
            float interval = (phase != null) ? phase.LaserSweepInterval : 5.0f;
            int dmgPerSec = (phase != null) ? phase.LaserDamagePerSecond : 12;

            float elapsed = 0f;
            float sweepAngle = -60f;
            while (elapsed < laserDuration && !isDead && !isStunned)
            {
                // Rotate laser origin
                if (laserOrigin != null)
                    laserOrigin.localRotation = Quaternion.Euler(0f, 0f, sweepAngle + (elapsed / laserDuration) * 120f);

                // Raycast along laser direction
                Vector2 dir = laserOrigin != null
                    ? laserOrigin.right
                    : Vector2.right;
                RaycastHit2D hit = Physics2D.Raycast(
                    laserOrigin != null ? (Vector2)laserOrigin.position : (Vector2)transform.position,
                    dir, 12f, LayerMask.GetMask("Player"));

                if (hit.collider != null)
                {
                    var damageable = hit.collider.GetComponentInParent<IDamageable>();
                    if (damageable != null)
                    {
                        DamageInfo info = new DamageInfo(
                            Mathf.RoundToInt(dmgPerSec * Time.deltaTime),
                            hit.point, dir * 3f, RealmType.Prime);
                        damageable.TakeDamage(info);
                    }
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            yield return new WaitForSeconds(interval - laserDuration);
        }

        protected override IEnumerator OverheatRoutine()
        {
            _currentState = SentinelState.Overheat;
            yield return base.OverheatRoutine();
            _currentState = SentinelState.Idle;
        }
    }
}
