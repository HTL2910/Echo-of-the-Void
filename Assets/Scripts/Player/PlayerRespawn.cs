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
        private Vector3 _safePosition;   // last spot Kael stood on solid ground (soft respawn target)
        private RealmType _safeRealm = RealmType.Prime;
        private bool _isRespawning;

        public bool IsRespawning => _isRespawning;

        private void Awake()
        {
            _stats = GetComponent<PlayerStats>();
            _controller = GetComponent<PlayerController>();
            _rb = GetComponent<Rigidbody2D>();
            _renderer = GetComponentInChildren<SpriteRenderer>();
            _checkpointPosition = transform.position;
            _safePosition = transform.position;
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
            if (_isRespawning) return;

            // Remember the last solid ground so hazards can put Kael back next to where he was
            if (_controller.IsGrounded)
            {
                _safePosition = transform.position;
                _safeRealm = RealityManager.Instance != null ? RealityManager.Instance.CurrentRealm : RealmType.Prime;
            }

            // Fell out of the world: soft respawn, HP unchanged (Spec D9)
            if (transform.position.y < fallLimitY) SoftRespawn();
        }

        /// <summary>
        /// Hazard respawn (spec D9): back to the last safe ground within ~0.3 s, HP unchanged.
        /// Used by spikes, pits, acid... Returns false if a respawn is already running.
        /// </summary>
        public bool SoftRespawn()
        {
            if (_isRespawning) return false;
            StartCoroutine(RespawnRoutine(hardDeath: false));
            return true;
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

            Vector3 targetPosition = hardDeath ? _checkpointPosition : _safePosition;
            RealmType targetRealm = hardDeath ? _checkpointRealm : _safeRealm;

            transform.position = targetPosition;
            _rb.simulated = true;
            if (_renderer != null) _renderer.enabled = true;

            // The platform we return to may only be solid in one realm
            if (RealityManager.Instance != null && RealityManager.Instance.CurrentRealm != targetRealm)
            {
                RealityManager.Instance.SwitchRealm(targetRealm);
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
