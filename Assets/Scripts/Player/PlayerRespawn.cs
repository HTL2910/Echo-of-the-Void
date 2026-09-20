using System.Collections;
using UnityEngine;
using EchoOfTheVoid.Core;
using EchoOfTheVoid.UI;

namespace EchoOfTheVoid.Player
{
    [RequireComponent(typeof(PlayerStats), typeof(PlayerController), typeof(Rigidbody2D))]
    public class PlayerRespawn : MonoBehaviour
    {
        [Header("Respawn (Spec 3.3)")]
        [SerializeField] private float respawnDelay = 1.0f;
        [SerializeField, Range(0.1f, 1f)] private float respawnHealthRatio = 0.5f;
        [SerializeField] private float fallLimitY = -15f;

        private PlayerStats _stats;
        private PlayerController _controller;
        private Rigidbody2D _rb;
        private SpriteRenderer _renderer;

        private Vector3 _checkpointPosition;
        private RealmType _checkpointRealm = RealmType.Prime;
        private bool _isRespawning;

        public bool IsRespawning => _isRespawning;

        private void Awake()
        {
            _stats = GetComponent<PlayerStats>();
            _controller = GetComponent<PlayerController>();
            _rb = GetComponent<Rigidbody2D>();
            _renderer = GetComponentInChildren<SpriteRenderer>();
            _checkpointPosition = transform.position;
        }

        private void OnEnable()
        {
            _stats.OnPlayerDeath += HandleDeath;
        }

        private void OnDisable()
        {
            _stats.OnPlayerDeath -= HandleDeath;
        }

        private void Update()
        {
            // Fell out of the world: soft respawn, HP unchanged (Spec D9)
            if (!_isRespawning && transform.position.y < fallLimitY)
            {
                StartCoroutine(RespawnRoutine(hardDeath: false));
            }
        }

        public void SetCheckpoint(Vector3 position, RealmType realm)
        {
            _checkpointPosition = position;
            _checkpointRealm = realm;
        }

        private void HandleDeath()
        {
            if (_isRespawning) return;

            if (PlayerHUD.Instance != null)
            {
                PlayerHUD.Instance.ShowAnnouncement("TIME COLLAPSED - RESPAWNING...", respawnDelay);
            }

            StartCoroutine(RespawnRoutine(hardDeath: true));
        }

        private IEnumerator RespawnRoutine(bool hardDeath)
        {
            _isRespawning = true;
            _controller.enabled = false;
            _rb.linearVelocity = Vector2.zero;
            _rb.simulated = false;
            if (_renderer != null) _renderer.enabled = false;

            yield return new WaitForSecondsRealtime(hardDeath ? respawnDelay : 0.3f);

            transform.position = _checkpointPosition;
            _rb.simulated = true;
            if (_renderer != null) _renderer.enabled = true;

            // Checkpoint platforms may only be solid in one realm
            if (RealityManager.Instance != null && RealityManager.Instance.CurrentRealm != _checkpointRealm)
            {
                RealityManager.Instance.SwitchRealm(_checkpointRealm);
            }

            _controller.ResetForRespawn();
            _controller.enabled = true;

            if (hardDeath)
            {
                _stats.Revive(Mathf.RoundToInt(_stats.MaxHealth * respawnHealthRatio));
            }

            _isRespawning = false;
        }
    }
}
