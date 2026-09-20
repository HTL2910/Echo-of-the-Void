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
        // Recent solid-ground positions. A hazard sends Kael to one from a moment ago, not to the spot right beside it
        // (a player still holding "run" would otherwise die again at once).
        private struct GroundSample { public Vector3 position; public RealmType realm; public float time; }
        private readonly System.Collections.Generic.List<GroundSample> _ground = new System.Collections.Generic.List<GroundSample>();
        private float _nextSampleTime;
        private const float SampleInterval = 0.1f;
        private const float SafeDelay = 0.4f;     // how far back in time the safe spot is
        private const float HistorySeconds = 3f;
        private bool _isRespawning;

        public bool IsRespawning => _isRespawning;

        private void Awake()
        {
            _stats = GetComponent<PlayerStats>();
            _controller = GetComponent<PlayerController>();
            _rb = GetComponent<Rigidbody2D>();
            _renderer = GetComponentInChildren<SpriteRenderer>();
            _checkpointPosition = transform.position;
            _ground.Add(new GroundSample { position = transform.position, realm = RealmType.Prime, time = Time.time });
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

            // Remember where Kael recently stood on solid ground
            if (_controller.IsGrounded && Time.time >= _nextSampleTime)
            {
                _nextSampleTime = Time.time + SampleInterval;
                _ground.Add(new GroundSample
                {
                    position = transform.position,
                    realm = RealityManager.Instance != null ? RealityManager.Instance.CurrentRealm : RealmType.Prime,
                    time = Time.time
                });
                while (_ground.Count > 1 && Time.time - _ground[0].time > HistorySeconds) _ground.RemoveAt(0);
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

        /// <summary>Newest ground sample at least <see cref="SafeDelay"/> old; otherwise the oldest one we have.</summary>
        private void PickSafeGround(out Vector3 position, out RealmType realm)
        {
            position = _checkpointPosition;
            realm = _checkpointRealm;
            if (_ground.Count == 0) return;

            GroundSample chosen = _ground[0];
            for (int i = _ground.Count - 1; i >= 0; i--)
            {
                if (Time.time - _ground[i].time >= SafeDelay) { chosen = _ground[i]; break; }
            }
            position = chosen.position;
            realm = chosen.realm;
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

            Vector3 targetPosition = _checkpointPosition;
            RealmType targetRealm = _checkpointRealm;
            if (!hardDeath) PickSafeGround(out targetPosition, out targetRealm);

            transform.position = targetPosition;
            _rb.simulated = true;
            if (_renderer != null) _renderer.enabled = true;

            // The platform we return to may only be solid in one realm
            if (RealityManager.Instance != null && RealityManager.Instance.CurrentRealm != targetRealm)
            {
                RealityManager.Instance.SwitchRealm(targetRealm);
            }

            _ground.Clear();
            _ground.Add(new GroundSample { position = targetPosition, realm = targetRealm, time = Time.time });
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
