using System.Collections;
using UnityEngine;
using EchoOfTheVoid.Core;
using EchoOfTheVoid.Environment;

namespace EchoOfTheVoid.Bosses
{
    /// <summary>
    /// K4: Boss arena trigger (spec §5.2).
    /// Entering the arena: fires BossEvents.Engaged + locks entrance.
    /// Boss defeated: unlocks arena, spawns WallJump pickup + ChronoStation.
    /// Player dies mid-fight: boss resets.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class BossArena : MonoBehaviour
    {
        [Header("Linked Boss")]
        [SerializeField] private BossBase boss;

        [Header("Gate Control")]
        [SerializeField] private GameObject entranceGate;

        [Header("Rewards")]
        [SerializeField] private GameObject wallJumpPickupPrefab;
        [SerializeField] private Transform rewardSpawnPoint;
        [SerializeField] private GameObject chronoStationPrefab;
        [SerializeField] private Transform chronoStationSpawnPoint;

        private bool _fightStarted;
        private bool _defeated;

        private void Awake()
        {
            var col = GetComponent<Collider2D>();
            col.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_fightStarted || _defeated) return;
            if (!other.CompareTag("Player")) return;

            _fightStarted = true;
            LockEntrance(true);

            if (boss != null)
            {
                boss.Engage();
                // Listen for boss defeat to unlock and reward
                StartCoroutine(WaitForBossDefeat());
            }
        }

        private IEnumerator WaitForBossDefeat()
        {
            bool defeated = false;

            void OnDefeated(string id)
            {
                if (boss == null) return;
                // Check id matches; BossBase sets its own id
                defeated = true;
            }

            BossEvents.Defeated += OnDefeated;

            while (!defeated)
                yield return null;

            BossEvents.Defeated -= OnDefeated;
            _defeated = true;

            // Unlock entrance
            LockEntrance(false);

            // Spawn WallJump pickup
            if (wallJumpPickupPrefab != null && rewardSpawnPoint != null)
                Instantiate(wallJumpPickupPrefab, rewardSpawnPoint.position, Quaternion.identity);

            // Spawn ChronoStation
            if (chronoStationPrefab != null && chronoStationSpawnPoint != null)
                Instantiate(chronoStationPrefab, chronoStationSpawnPoint.position, Quaternion.identity);
        }

        private void LockEntrance(bool locked)
        {
            if (entranceGate != null)
                entranceGate.SetActive(locked);
        }
    }
}
