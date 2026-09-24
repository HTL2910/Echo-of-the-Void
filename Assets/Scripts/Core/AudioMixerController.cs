using UnityEngine;
using UnityEngine.Audio;
using EchoOfTheVoid.Player;
using EchoOfTheVoid.Save;

namespace EchoOfTheVoid.Core
{
    /// <summary>
    /// Controls audio mixer snapshots and volume levels (spec 6. Âm thanh, spec_audiomixer_lowhp.md).
    /// Routes: Master → Music (LPF), SFX (Player/World), UI, Ambience.
    /// Snapshots: Default_Prime/Echo, LowHealth (800 Hz LPF when HP < 25%), Paused.
    /// </summary>
    public class AudioMixerController : MonoBehaviour
    {
        public static AudioMixerController Instance { get; private set; }

        [Header("Mixer & Snapshots")]
        [SerializeField] private AudioMixer mixer;
        [SerializeField] private AudioMixerSnapshot snapshotPrime;
        [SerializeField] private AudioMixerSnapshot snapshotEcho;
        [SerializeField] private AudioMixerSnapshot snapshotLowHp;
        [SerializeField] private AudioMixerSnapshot snapshotPaused;

        [Header("Exposed Parameters")]
        [SerializeField] private string masterVolumeParam = "MasterVolume";
        [SerializeField] private string musicVolumeParam = "MusicVolume";
        [SerializeField] private string sfxVolumeParam = "SfxVolume";
        [SerializeField] private string uiVolumeParam = "UiVolume";
        [SerializeField] private string musicLpfParam = "MusicLowPassCutoff";

        private bool _isLowHp;
        private bool _isPaused;
        private RealmType _currentRealm = RealmType.Prime;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            RealityEventBus.OnRealmSwitched += HandleRealmSwitched;
            GameFlow.OnGamePaused += HandleGamePaused;
            GameFlow.OnGameResumed += HandleGameResumed;
            GameFlow.OnGameLoaded += OnGameLoaded;
        }

        private void OnDisable()
        {
            RealityEventBus.OnRealmSwitched -= HandleRealmSwitched;
            GameFlow.OnGamePaused -= HandleGamePaused;
            GameFlow.OnGameResumed -= HandleGameResumed;
            GameFlow.OnGameLoaded -= OnGameLoaded;

            if (PlayerStats.Instance != null)
                PlayerStats.Instance.OnHealthChanged -= HandlePlayerHealthChanged;
        }

        private void OnGameLoaded(SaveData data)
        {
            _isLowHp = false;
            _isPaused = false;
            _currentRealm = RealmType.Prime;

            if (PlayerStats.Instance != null)
            {
                PlayerStats.Instance.OnHealthChanged -= HandlePlayerHealthChanged;
                PlayerStats.Instance.OnHealthChanged += HandlePlayerHealthChanged;
            }
        }

        /// <summary>Set mixer volume (linear 0-1 to dB).</summary>
        public void SetVolume(string exposedParam, float linear01)
        {
            if (mixer == null) return;
            float dB = Mathf.Log10(Mathf.Clamp(linear01, 0.0001f, 1f)) * 20f;
            mixer.SetFloat(exposedParam, dB);
        }

        /// <summary>Set mixer parameter (e.g., LPF cutoff in Hz).</summary>
        public void SetParameter(string paramName, float value)
        {
            if (mixer == null) return;
            mixer.SetFloat(paramName, value);
        }

        private void HandlePlayerHealthChanged(int current, int max)
        {
            bool low = max > 0 && ((float)current / max) <= 0.25f;
            if (low != _isLowHp)
            {
                _isLowHp = low;
                ApplySnapshotState(0.4f);
            }
        }

        private void HandleRealmSwitched(RealmType realm)
        {
            _currentRealm = realm;
            if (!_isLowHp && !_isPaused)
                ApplySnapshotState(0.18f);
        }

        private void HandleGamePaused()
        {
            _isPaused = true;
            ApplySnapshotState(0.15f);
        }

        private void HandleGameResumed()
        {
            _isPaused = false;
            ApplySnapshotState(0.15f);
        }

        private void ApplySnapshotState(float transitionTime)
        {
            if (mixer == null) return;

            AudioMixerSnapshot target = null;

            if (_isPaused && snapshotPaused != null)
                target = snapshotPaused;
            else if (_isLowHp && snapshotLowHp != null)
                target = snapshotLowHp;
            else if (_currentRealm == RealmType.Echo && snapshotEcho != null)
                target = snapshotEcho;
            else if (snapshotPrime != null)
                target = snapshotPrime;

            if (target != null)
                target.TransitionTo(transitionTime);
        }
    }
}
