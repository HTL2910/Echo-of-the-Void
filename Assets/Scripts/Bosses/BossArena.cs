using UnityEngine;
using EchoOfTheVoid.Core;

namespace EchoOfTheVoid.Bosses
{
    /// <summary>
    /// K4 + integration: boss arena trigger (spec 5.2).
    /// Entering starts the fight and locks the entrance; defeating the boss unlocks it, records the kill in the save
    /// (so it never respawns), and spawns the reward and a Chrono Station. If Kael dies mid-fight the boss resets
    /// and the entrance reopens so the fight can be retried.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class BossArena : MonoBehaviour
    {
        [Header("Linked Boss")]
        [SerializeField] private BossBase boss;

        [Header("Gate Control")]
        [SerializeField] private GameObject entranceGate;

        [Header("Rewards")]
        [SerializeField] private GameObject wallJumpPickupPrefab;   // the ability reward (name kept for old prefabs)
        [SerializeField] private Transform rewardSpawnPoint;
        [SerializeField] private GameObject chronoStationPrefab;
        [SerializeField] private Transform chronoStationSpawnPoint;

        private bool _fightStarted;
        private bool _defeated;

        public bool FightStarted => _fightStarted;
        public bool IsDefeated => _defeated;

        public void Configure(BossBase linkedBoss, GameObject gate, GameObject rewardPrefab, Transform rewardPoint,
            GameObject stationPrefab, Transform stationPoint)
        {
            boss = linkedBoss;
            entranceGate = gate;
            wallJumpPickupPrefab = rewardPrefab;
            rewardSpawnPoint = rewardPoint;
            chronoStationPrefab = stationPrefab;
            chronoStationSpawnPoint = stationPoint;
        }

        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnEnable()
        {
            BossEvents.Defeated += OnBossDefeated;
            BossEvents.Reset += OnBossReset;
        }

        private void OnDisable()
        {
            BossEvents.Defeated -= OnBossDefeated;
            BossEvents.Reset -= OnBossReset;
        }

        private void Start()
        {
            // Already beaten in this save: the boss stays gone, the way stays open, the station is there
            if (boss != null && GameSession.IsBossDefeated(boss.BossId))
            {
                _defeated = true;
                boss.gameObject.SetActive(false);
                LockEntrance(false);
                SpawnStation();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_fightStarted || _defeated || boss == null) return;
            if (!other.CompareTag("Player")) return;

            _fightStarted = true;
            LockEntrance(true);
            boss.Engage();
        }

        private void OnBossDefeated(string id)
        {
            if (boss == null || id != boss.BossId || _defeated) return;

            _defeated = true;
            GameSession.MarkBossDefeated(id);
            LockEntrance(false);

            if (wallJumpPickupPrefab != null && rewardSpawnPoint != null)
                Instantiate(wallJumpPickupPrefab, rewardSpawnPoint.position, Quaternion.identity);
            SpawnStation();
        }

        private void OnBossReset(string id)
        {
            // Kael died mid-fight: reopen the entrance and allow a rematch
            if (boss == null || id != boss.BossId || _defeated) return;

            _fightStarted = false;
            LockEntrance(false);
        }

        private void SpawnStation()
        {
            if (chronoStationPrefab != null && chronoStationSpawnPoint != null)
                Instantiate(chronoStationPrefab, chronoStationSpawnPoint.position, Quaternion.identity);
        }

        private void LockEntrance(bool locked)
        {
            if (entranceGate != null) entranceGate.SetActive(locked);
        }
    }
}
