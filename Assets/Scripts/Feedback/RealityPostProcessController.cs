using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using EchoOfTheVoid.Core;
using EchoOfTheVoid.Player;

namespace EchoOfTheVoid.Feedback
{
    /// <summary>
    /// Manages real-time visual post-processing and 2D lighting transitions
    /// between Prime and Echo realms (GDD Spec §8.3 & §9.1), plus low-HP heartbeat warning.
    /// </summary>
    public class RealityPostProcessController : MonoBehaviour
    {
        public static RealityPostProcessController Instance { get; private set; }

        [Header("Target References")]
        [SerializeField] private Volume volume;
        [SerializeField] private Light2D globalLight2D;
        [SerializeField] private Light2D playerAuraLight2D;

        [Header("Transition Settings")]
        [SerializeField] private float transitionDuration = 0.22f;

        [Header("Prime Realm Visuals")]
        [SerializeField] private Color primeGlobalLightColor = new Color(0.24f, 0.30f, 0.44f, 1f);
        [SerializeField] private float primeGlobalLightIntensity = 0.55f;
        [SerializeField] private Color primeAuraColor = new Color(0.15f, 0.95f, 0.85f, 1f); // Emerald / Cyan
        [SerializeField] private Color primeColorFilter = new Color(0.92f, 1.0f, 0.97f, 1f);
        [SerializeField] private float primeBloomIntensity = 1.8f;
        [SerializeField] private float primeContrast = 22f;
        [SerializeField] private float primeSaturation = 28f;

        [Header("Echo Realm Visuals")]
        [SerializeField] private Color echoGlobalLightColor = new Color(0.28f, 0.16f, 0.42f, 1f);
        [SerializeField] private float echoGlobalLightIntensity = 0.50f;
        [SerializeField] private Color echoAuraColor = new Color(0.85f, 0.35f, 1.0f, 1f); // Amethyst Violet
        [SerializeField] private Color echoColorFilter = new Color(0.96f, 0.88f, 1.0f, 1f);
        [SerializeField] private float echoBloomIntensity = 2.4f;
        [SerializeField] private float echoContrast = 26f;
        [SerializeField] private float echoSaturation = 34f;

        [Header("Lens Distortion / Shockwave")]
        [SerializeField] private float baselineChromaticAberration = 0.14f;
        [SerializeField] private float shiftChromaticAberration = 0.45f;

        [Header("Low HP Danger (< 30%)")]
        [SerializeField] private Color lowHpVignetteColor = new Color(0.85f, 0.08f, 0.15f, 1f);
        [SerializeField] private float lowHpThreshold = 0.30f;
        [SerializeField] private float heartbeatSpeed = 5.5f;

        private Bloom _bloom;
        private ColorAdjustments _colorAdjustments;
        private Vignette _vignette;
        private ChromaticAberration _chromaticAberration;

        private Coroutine _transitionCoroutine;
        private Coroutine _aberrationPulseCoroutine;
        private bool _isLowHp;
        private Color _baseVignetteColor = new Color(0.02f, 0.04f, 0.08f, 1f);
        private float _baseVignetteIntensity = 0.32f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            ResolveComponents();
        }

        private void Start()
        {
            RealmType currentRealm = RealityManager.Instance != null 
                ? RealityManager.Instance.CurrentRealm 
                : RealmType.Prime;

            ApplyInstantRealmState(currentRealm);

            if (PlayerStats.Instance != null)
            {
                PlayerStats.Instance.OnHealthChanged += HandleHealthChanged;
                CheckHealth(PlayerStats.Instance.CurrentHealth, PlayerStats.Instance.MaxHealth);
            }
        }

        private void OnEnable()
        {
            RealityEventBus.OnRealmSwitched += HandleRealmSwitched;
        }

        private void OnDisable()
        {
            RealityEventBus.OnRealmSwitched -= HandleRealmSwitched;
            if (PlayerStats.Instance != null)
            {
                PlayerStats.Instance.OnHealthChanged -= HandleHealthChanged;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (_isLowHp && _vignette != null)
            {
                // Pulsing red heart-rhythm when below 30% health
                float sine = Mathf.Abs(Mathf.Sin(Time.time * heartbeatSpeed));
                _vignette.color.value = Color.Lerp(_baseVignetteColor, lowHpVignetteColor, sine * 0.85f);
                _vignette.intensity.value = Mathf.Lerp(_baseVignetteIntensity, 0.52f, sine);
            }
        }

        public void Configure(Volume targetVolume, Light2D globalLight, Light2D playerAura)
        {
            volume = targetVolume;
            globalLight2D = globalLight;
            playerAuraLight2D = playerAura;
            ResolveComponents();
        }

        public void SetPlayerAuraLight(Light2D playerAura)
        {
            playerAuraLight2D = playerAura;
            RealmType currentRealm = RealityManager.Instance != null 
                ? RealityManager.Instance.CurrentRealm 
                : RealmType.Prime;
            if (playerAuraLight2D != null)
            {
                playerAuraLight2D.color = currentRealm == RealmType.Prime ? primeAuraColor : echoAuraColor;
            }
        }

        private void ResolveComponents()
        {
            if (volume == null)
            {
                volume = GetComponent<Volume>();
                if (volume == null)
                {
                    volume = FindFirstObjectByType<Volume>();
                }
            }

            if (volume != null && volume.profile != null)
            {
                volume.profile.TryGet(out _bloom);
                volume.profile.TryGet(out _colorAdjustments);
                volume.profile.TryGet(out _vignette);
                volume.profile.TryGet(out _chromaticAberration);

                if (_vignette != null)
                {
                    _baseVignetteColor = _vignette.color.value;
                    _baseVignetteIntensity = _vignette.intensity.value;
                }
            }

            if (globalLight2D == null)
            {
                var lights = FindObjectsByType<Light2D>(FindObjectsSortMode.None);
                foreach (var l in lights)
                {
                    if (l.lightType == Light2D.LightType.Global)
                    {
                        globalLight2D = l;
                        break;
                    }
                }
            }
        }

        private void HandleRealmSwitched(RealmType newRealm)
        {
            if (_transitionCoroutine != null)
            {
                StopCoroutine(_transitionCoroutine);
            }
            _transitionCoroutine = StartCoroutine(TransitionRoutine(newRealm));

            if (_aberrationPulseCoroutine != null)
            {
                StopCoroutine(_aberrationPulseCoroutine);
            }
            _aberrationPulseCoroutine = StartCoroutine(AberrationPulseRoutine());
        }

        private IEnumerator TransitionRoutine(RealmType targetRealm)
        {
            bool isPrime = targetRealm == RealmType.Prime;

            Color targetGlobalColor = isPrime ? primeGlobalLightColor : echoGlobalLightColor;
            float targetGlobalIntensity = isPrime ? primeGlobalLightIntensity : echoGlobalLightIntensity;
            Color targetAuraColor = isPrime ? primeAuraColor : echoAuraColor;

            Color targetFilter = isPrime ? primeColorFilter : echoColorFilter;
            float targetBloom = isPrime ? primeBloomIntensity : echoBloomIntensity;
            float targetContrast = isPrime ? primeContrast : echoContrast;
            float targetSat = isPrime ? primeSaturation : echoSaturation;

            Color startGlobalColor = globalLight2D != null ? globalLight2D.color : targetGlobalColor;
            float startGlobalIntensity = globalLight2D != null ? globalLight2D.intensity : targetGlobalIntensity;
            Color startAuraColor = playerAuraLight2D != null ? playerAuraLight2D.color : targetAuraColor;

            Color startFilter = _colorAdjustments != null ? _colorAdjustments.colorFilter.value : targetFilter;
            float startBloom = _bloom != null ? _bloom.intensity.value : targetBloom;
            float startContrast = _colorAdjustments != null ? _colorAdjustments.contrast.value : targetContrast;
            float startSat = _colorAdjustments != null ? _colorAdjustments.saturation.value : targetSat;

            float elapsed = 0f;
            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / transitionDuration);
                float smooth = Mathf.SmoothStep(0f, 1f, t);

                if (globalLight2D != null)
                {
                    globalLight2D.color = Color.Lerp(startGlobalColor, targetGlobalColor, smooth);
                    globalLight2D.intensity = Mathf.Lerp(startGlobalIntensity, targetGlobalIntensity, smooth);
                }

                if (playerAuraLight2D != null)
                {
                    playerAuraLight2D.color = Color.Lerp(startAuraColor, targetAuraColor, smooth);
                }

                if (_bloom != null)
                {
                    _bloom.intensity.value = Mathf.Lerp(startBloom, targetBloom, smooth);
                }

                if (_colorAdjustments != null)
                {
                    _colorAdjustments.colorFilter.value = Color.Lerp(startFilter, targetFilter, smooth);
                    _colorAdjustments.contrast.value = Mathf.Lerp(startContrast, targetContrast, smooth);
                    _colorAdjustments.saturation.value = Mathf.Lerp(startSat, targetSat, smooth);
                }

                yield return null;
            }

            ApplyInstantRealmState(targetRealm);
            _transitionCoroutine = null;
        }

        private IEnumerator AberrationPulseRoutine()
        {
            if (_chromaticAberration == null) yield break;

            _chromaticAberration.intensity.value = shiftChromaticAberration;
            float duration = 0.25f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                _chromaticAberration.intensity.value = Mathf.Lerp(shiftChromaticAberration, baselineChromaticAberration, t * t);
                yield return null;
            }

            _chromaticAberration.intensity.value = baselineChromaticAberration;
            _aberrationPulseCoroutine = null;
        }

        private void ApplyInstantRealmState(RealmType realm)
        {
            bool isPrime = realm == RealmType.Prime;

            if (globalLight2D != null)
            {
                globalLight2D.color = isPrime ? primeGlobalLightColor : echoGlobalLightColor;
                globalLight2D.intensity = isPrime ? primeGlobalLightIntensity : echoGlobalLightIntensity;
            }

            if (playerAuraLight2D != null)
            {
                playerAuraLight2D.color = isPrime ? primeAuraColor : echoAuraColor;
            }

            if (_bloom != null)
            {
                _bloom.intensity.value = isPrime ? primeBloomIntensity : echoBloomIntensity;
            }

            if (_colorAdjustments != null)
            {
                _colorAdjustments.colorFilter.value = isPrime ? primeColorFilter : echoColorFilter;
                _colorAdjustments.contrast.value = isPrime ? primeContrast : echoContrast;
                _colorAdjustments.saturation.value = isPrime ? primeSaturation : echoSaturation;
            }

            if (_chromaticAberration != null)
            {
                _chromaticAberration.intensity.value = baselineChromaticAberration;
            }
        }

        private void HandleHealthChanged(int current, int max)
        {
            CheckHealth(current, max);
        }

        private void CheckHealth(int current, int max)
        {
            if (max <= 0) return;
            float ratio = (float)current / max;
            _isLowHp = ratio <= lowHpThreshold;

            if (!_isLowHp && _vignette != null)
            {
                _vignette.color.value = _baseVignetteColor;
                _vignette.intensity.value = _baseVignetteIntensity;
            }
        }
    }
}
