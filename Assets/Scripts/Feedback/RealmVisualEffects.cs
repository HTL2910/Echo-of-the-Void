using UnityEngine;
using UnityEngine.UI;
using EchoOfTheVoid.Core;
using EchoOfTheVoid.Settings;

namespace EchoOfTheVoid.Feedback
{
    /// <summary>Handles per-realm visual feedback: tint overlays, vignette, particle color adjustments.</summary>
    public class RealmVisualEffects : MonoBehaviour
    {
        public static RealmVisualEffects Instance { get; private set; }

        [SerializeField] private float transitionDuration = 0.18f;

        // Overlay panel for tint
        private Image _tintOverlay;
        private CanvasGroup _tintCanvasGroup;
        private float _tintTimer;

        // Vignette image
        private Image _vignetteOverlay;
        private CanvasGroup _vignetteCanvasGroup;

        // Colors per realm (spec 2.4: Prime cool, Echo warm)
        private readonly Color _primeTint = new(0.2f, 0.4f, 0.8f, 0.15f);  // Cool blue
        private readonly Color _echoTint = new(0.9f, 0.5f, 0.3f, 0.12f);   // Warm orange

        // Vignette colors
        private readonly Color _primeVignette = new(0.1f, 0.3f, 0.7f, 0.4f);
        private readonly Color _echoVignette = new(0.8f, 0.4f, 0.2f, 0.4f);

        private RealmType _currentVisualRealm;
        private Color _targetTintColor;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            CreateTintOverlay();
            CreateVignetteOverlay();

            _currentVisualRealm = RealmType.Prime;
            _targetTintColor = _primeTint;
        }

        private void Start()
        {
            RealityEventBus.OnRealmSwitch += HandleRealmSwitch;
        }

        private void OnDestroy()
        {
            RealityEventBus.OnRealmSwitch -= HandleRealmSwitch;
            if (Instance == this) Instance = null;
        }

        private void CreateTintOverlay()
        {
            var canvasGo = new GameObject("RealmTintOverlay");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;  // Below pause/HUD but above gameplay

            var imageGo = new GameObject("Tint");
            imageGo.transform.SetParent(canvasGo.transform, false);

            _tintOverlay = imageGo.AddComponent<Image>();
            _tintOverlay.color = _primeTint;
            _tintOverlay.raycastTarget = false;

            var rect = _tintOverlay.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            _tintCanvasGroup = canvasGo.AddComponent<CanvasGroup>();
            _tintCanvasGroup.blocksRaycasts = false;
        }

        private void CreateVignetteOverlay()
        {
            var canvasGo = new GameObject("RealmVignetteOverlay");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 99;  // Just below tint

            var imageGo = new GameObject("Vignette");
            imageGo.transform.SetParent(canvasGo.transform, false);

            _vignetteOverlay = imageGo.AddComponent<Image>();
            _vignetteOverlay.color = _primeVignette;
            _vignetteOverlay.raycastTarget = false;

            // Use a simple radial gradient for vignette (white center, fades to color at edges)
            // For now, use a semi-transparent color that will be visible at screen edges
            var rect = _vignetteOverlay.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            _vignetteCanvasGroup = canvasGo.AddComponent<CanvasGroup>();
            _vignetteCanvasGroup.blocksRaycasts = false;
        }

        private void Update()
        {
            if (_tintTimer > 0f)
            {
                _tintTimer -= Time.deltaTime;
                float t = 1f - Mathf.Clamp01(_tintTimer / transitionDuration);

                // Smoothly interpolate tint color over transition duration
                var currentTint = _tintOverlay.color;
                currentTint = Color.Lerp(currentTint, _targetTintColor, t);
                _tintOverlay.color = currentTint;
            }
        }

        private void HandleRealmSwitch(RealmType newRealm)
        {
            if (_currentVisualRealm == newRealm) return;

            _currentVisualRealm = newRealm;
            _tintTimer = transitionDuration;

            // Set target tint and vignette colors
            _targetTintColor = newRealm == RealmType.Prime ? _primeTint : _echoTint;
            var targetVignette = newRealm == RealmType.Prime ? _primeVignette : _echoVignette;

            // Animate vignette color
            StartCoroutine(AnimateVignetteColor(targetVignette));
        }

        private System.Collections.IEnumerator AnimateVignetteColor(Color targetColor)
        {
            float elapsed = 0f;
            Color startColor = _vignetteOverlay.color;

            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / transitionDuration;
                _vignetteOverlay.color = Color.Lerp(startColor, targetColor, t);
                yield return null;
            }

            _vignetteOverlay.color = targetColor;
        }

        /// <summary>Returns the current realm tint color for external systems (particles, etc).</summary>
        public Color GetRealmColor(RealmType realm)
        {
            return realm == RealmType.Prime ? _primeTint : _echoTint;
        }
    }
}
