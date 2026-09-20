using UnityEngine;
using EchoOfTheVoid.Settings;

namespace EchoOfTheVoid.Core
{
    /// <summary>
    /// Adaptive music (spec 7.1): the Prime and Echo stems play in lock-step and only their volumes change.
    /// A realm switch equal-power crossfades between them in 0.18 s; the fade runs on unscaled time so a hitstop
    /// freeze cannot stall it. Weights satisfy prime^2 + echo^2 = 1 at every moment (no loudness dip).
    /// </summary>
    public class MusicLayerController : MonoBehaviour
    {
        [SerializeField] private AudioClip primeStem;
        [SerializeField] private AudioClip echoStem;
        [SerializeField, Range(0f, 1f)] private float volume = 0.6f;
        [SerializeField] private float crossfadeSeconds = 0.18f;

        private AudioSource _primeSource;
        private AudioSource _echoSource;

        private float _mix;          // 0 = pure Prime, 1 = pure Echo
        private float _mixFrom;
        private float _mixTo;
        private float _fadeElapsed;

        /// <summary>Current weight of each stem (before the master volume), 0..1.</summary>
        public float PrimeWeight => Mathf.Cos(_mix * Mathf.PI * 0.5f);
        public float EchoWeight => Mathf.Sin(_mix * Mathf.PI * 0.5f);
        public bool HasStems => primeStem != null && echoStem != null;

        public void Configure(AudioClip prime, AudioClip echo)
        {
            primeStem = prime;
            echoStem = echo;
        }

        private void OnEnable()
        {
            RealityEventBus.OnRealmSwitched += HandleRealmSwitched;
        }

        private void OnDisable()
        {
            RealityEventBus.OnRealmSwitched -= HandleRealmSwitched;
        }

        private void Start()
        {
            _mix = _mixFrom = _mixTo = (RealityManager.Instance != null && RealityManager.Instance.CurrentRealm == RealmType.Echo) ? 1f : 0f;
            _fadeElapsed = crossfadeSeconds;

            if (!HasStems) return; // art not delivered yet: stay silent

            _primeSource = CreateSource("Stem_Prime", primeStem);
            _echoSource = CreateSource("Stem_Echo", echoStem);

            // Start both on the same audio-clock tick so the stems stay sample-aligned
            double startTime = AudioSettings.dspTime + 0.1;
            _primeSource.PlayScheduled(startTime);
            _echoSource.PlayScheduled(startTime);
            ApplyVolumes();
        }

        private AudioSource CreateSource(string sourceName, AudioClip clip)
        {
            var child = new GameObject(sourceName);
            child.transform.SetParent(transform, false);
            var source = child.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            return source;
        }

        private void HandleRealmSwitched(RealmType realm)
        {
            float target = (realm == RealmType.Echo) ? 1f : 0f;
            if (Mathf.Approximately(target, _mixTo)) return;

            _mixFrom = _mix;
            _mixTo = target;
            _fadeElapsed = 0f;
        }

        private void Update()
        {
            if (_fadeElapsed < crossfadeSeconds)
            {
                _fadeElapsed += Time.unscaledDeltaTime;
                float t = crossfadeSeconds > 0f ? Mathf.Clamp01(_fadeElapsed / crossfadeSeconds) : 1f;
                _mix = Mathf.Lerp(_mixFrom, _mixTo, t);
            }
            ApplyVolumes();
        }

        private void ApplyVolumes()
        {
            if (_primeSource == null) return;
            float level = volume * SettingsService.Current.musicVolume;
            _primeSource.volume = PrimeWeight * level;
            _echoSource.volume = EchoWeight * level;
        }
    }
}
