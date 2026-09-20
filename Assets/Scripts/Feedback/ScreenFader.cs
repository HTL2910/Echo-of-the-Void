using UnityEngine;
using EchoOfTheVoid.Settings;
using UnityEngine.UI;

namespace EchoOfTheVoid.Feedback
{
    /// <summary>Full-screen black overlay for room transitions. Creates its own canvas on first use; runs on unscaled time.</summary>
    public class ScreenFader : MonoBehaviour
    {
        public static ScreenFader Instance { get; private set; }

        private Image _image;
        private float _timer;
        private float _duration;
        private float _peak = 1f;

        public float Alpha => _image != null ? _image.color.a : 0f;

        /// <summary>Ensures a fader exists (idempotent).</summary>
        public static ScreenFader Ensure()
        {
            if (Instance != null) return Instance;

            var go = new GameObject("ScreenFader");
            DontDestroyOnLoad(go);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;

            var imageGo = new GameObject("Black");
            imageGo.transform.SetParent(go.transform, false);
            var image = imageGo.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0f);
            image.raycastTarget = false;
            var rect = image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            var fader = go.AddComponent<ScreenFader>();
            fader._image = image;
            Instance = fader;
            return fader;
        }

        /// <summary>Start opaque and fade to clear over <paramref name="duration"/> seconds (a room-change flash).</summary>
        public static void Flash(float duration)
        {
            var fader = Ensure();
            fader._duration = Mathf.Max(0.01f, duration);
            fader._timer = fader._duration;
            fader._peak = SettingsService.Current.reduceFlashing ? 0.25f : 1f; // photosensitivity option (spec 9.6)
            fader.SetAlpha(fader._peak);
        }

        private void Update()
        {
            if (_timer <= 0f) return;

            _timer -= Time.unscaledDeltaTime;
            SetAlpha(_peak * Mathf.Clamp01(_timer / _duration));
        }

        private void SetAlpha(float alpha)
        {
            if (_image == null) return;
            var c = _image.color;
            c.a = alpha;
            _image.color = c;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
