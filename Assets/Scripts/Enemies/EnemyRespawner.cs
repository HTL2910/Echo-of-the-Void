using UnityEngine;
using EchoOfTheVoid.Environment;

namespace EchoOfTheVoid.Enemies
{
    /// <summary>
    /// Handles respawning regular enemies when Kael uses a ChronoStation or resets.
    /// Bosses are not respawned by ChronoStation (spec §5.2).
    /// </summary>
    public class EnemyRespawner : MonoBehaviour
    {
        [SerializeField] private bool isBoss = false;

        private EnemyBase _enemy;
        private Vector3 _spawnPosition;
        private Quaternion _spawnRotation;

        public bool IsBoss
        {
            get => isBoss;
            set => isBoss = value;
        }

        public Vector3 SpawnPosition => _spawnPosition;

        private void Awake()
        {
            _enemy = GetComponent<EnemyBase>();
            _spawnPosition = transform.position;
            _spawnRotation = transform.rotation;
        }

        private void OnEnable()
        {
            ChronoStation.AnyStationUsed += HandleStationUsed;
        }

        private void OnDisable()
        {
            ChronoStation.AnyStationUsed -= HandleStationUsed;
        }

        private void HandleStationUsed(string stationId)
        {
            if (isBoss) return;
            Respawn();
        }

        public void Respawn()
        {
            gameObject.SetActive(true);
            transform.position = _spawnPosition;
            transform.rotation = _spawnRotation;

            if (_enemy != null)
            {
                _enemy.ResetEnemyState();
            }
        }
    }
}
