using UnityEngine;
using EchoOfTheVoid.Player;
using EchoOfTheVoid.Settings;

namespace EchoOfTheVoid.Core
{
    /// <summary>
    /// Heartbeat under 25% health (spec 7.3): loops and speeds up from 60 towards 130 BPM as health drops.
    /// The recorded loop is taken as 60 BPM, so the pitch runs from 1.0 to ~2.17.
    /// </summary>
    public class LowHealthAudio : MonoBehaviour
    {
        public const float Threshold = 0.25f;

        [SerializeField] private AudioClip heartbeatLoop;
        [SerializeField, Range(0f, 1f)] private float volume = 0.6f;

        private AudioSource _source;
        private PlayerStats _stats;

        public bool IsBeating => _source != null && _source.enabled && _source.volume > 0f && _active;
        public float Pitch => _source != null ? _source.pitch : 1f;
        private bool _active;

        public void Configure(AudioClip clip) => heartbeatLoop = clip;

        private void Awake()
        {
            _source = gameObject.AddComponent<AudioSource>();
            _source.loop = true;
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;
        }

        private void Start()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) _stats = player.GetComponent<PlayerStats>();
            if (_stats != null)
            {
                _stats.OnHealthChanged += OnHealthChanged;
                OnHealthChanged(_stats.CurrentHealth, _stats.MaxHealth);
            }
        }

        private void OnDestroy()
        {
            if (_stats != null) _stats.OnHealthChanged -= OnHealthChanged;
        }

        private void OnHealthChanged(int current, int max)
        {
            float ratio = max > 0 ? (float)current / max : 1f;
            bool low = current > 0 && ratio < Threshold;

            if (low)
            {
                float t = 1f - Mathf.Clamp01(ratio / Threshold);         // 0 at 25%, 1 at 0%
                _source.pitch = Mathf.Lerp(1f, 130f / 60f, t);
                _source.volume = volume * SettingsService.Current.sfxVolume;
                if (!_active && heartbeatLoop != null)
                {
                    _source.clip = heartbeatLoop;
                    _source.Play();
                }
            }
            else if (_active)
            {
                _source.Stop();
            }
            _active = low && heartbeatLoop != null;
        }
    }
}
