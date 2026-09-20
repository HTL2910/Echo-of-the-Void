using System.Collections.Generic;
using UnityEngine;

namespace EchoOfTheVoid.Core
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("SFX Clips")]
        [SerializeField] private AudioClip[] slashClips;
        [SerializeField] private AudioClip hitClip;
        [SerializeField] private AudioClip dashClip;
        [SerializeField] private AudioClip shiftClip;
        [SerializeField] private AudioClip jumpClip;
        [SerializeField] private AudioClip resonanceClip;
        [SerializeField] private AudioClip shiftDeniedClip;

        [Header("Audio Pool Settings")]
        [SerializeField] private int poolSize = 8;

        private List<AudioSource> _sourcesPool;
        private int _poolIndex = 0;

        private void OnEnable() => RealityEventBus.OnShiftDenied += PlayShiftDenied;
        private void OnDisable() => RealityEventBus.OnShiftDenied -= PlayShiftDenied;

        public void PlayShiftDenied()
        {
            PlaySound(shiftDeniedClip, 0.7f, 0.02f);
        }

        public void ConfigureShiftDenied(AudioClip clip)
        {
            shiftDeniedClip = clip;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializePool();
        }

        private void InitializePool()
        {
            _sourcesPool = new List<AudioSource>(poolSize);
            for (int i = 0; i < poolSize; i++)
            {
                GameObject child = new GameObject($"SFX_Channel_{i}");
                child.transform.SetParent(transform);
                var src = child.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 0f; // 2D Sound
                _sourcesPool.Add(src);
            }
        }

        private AudioSource GetAvailableSource()
        {
            if (_sourcesPool == null || _sourcesPool.Count == 0) return null;

            AudioSource src = _sourcesPool[_poolIndex];
            _poolIndex = (_poolIndex + 1) % _sourcesPool.Count;
            return src;
        }

        public void PlaySound(AudioClip clip, float volume = 1f, float pitchRandomness = 0.05f)
        {
            if (clip == null) return;
            AudioSource src = GetAvailableSource();
            if (src == null) return;

            src.pitch = 1f + Random.Range(-pitchRandomness, pitchRandomness);
            src.PlayOneShot(clip, volume);
        }

        public void PlaySlash()
        {
            if (slashClips != null && slashClips.Length > 0)
            {
                AudioClip clip = slashClips[Random.Range(0, slashClips.Length)];
                PlaySound(clip, 0.7f, 0.1f);
            }
        }

        public void PlayHit()
        {
            PlaySound(hitClip, 0.9f, 0.08f);
        }

        public void PlayDash()
        {
            PlaySound(dashClip, 0.8f, 0.05f);
        }

        public void PlayRealityShift()
        {
            PlaySound(shiftClip, 0.85f, 0.02f);
        }

        public void PlayJump()
        {
            PlaySound(jumpClip, 0.6f, 0.05f);
        }

        public void PlayResonance()
        {
            PlaySound(resonanceClip, 1f, 0.02f);
        }

        // Setup helper for SceneGenerator
        public void ConfigureClips(AudioClip[] slashes, AudioClip hit, AudioClip dash, AudioClip shift, AudioClip jump, AudioClip resonance)
        {
            slashClips = slashes;
            hitClip = hit;
            dashClip = dash;
            shiftClip = shift;
            jumpClip = jump;
            resonanceClip = resonance;
        }
    }
}
