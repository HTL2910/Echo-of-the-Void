using System.Collections;
using UnityEngine;
using EchoOfTheVoid.Settings;

namespace EchoOfTheVoid.Feedback
{
    public class CameraShakeManager : MonoBehaviour
    {
        public static CameraShakeManager Instance { get; private set; }

        private Transform _cameraTransform;
        private Vector3 _originalLocalPos;
        private Coroutine _shakeRoutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (Camera.main != null)
            {
                _cameraTransform = Camera.main.transform;
            }
        }

        public void ShakeLight()
        {
            // GDD: A = 0.08, F = 25Hz, T = 0.12s
            Shake(0.08f, 25f, 0.12f, 0f);
        }

        public void ShakeMedium()
        {
            // GDD: A = 0.22, F = 35Hz, T = 0.20s
            Shake(0.22f, 35f, 0.20f, 0f);
        }

        public void ShakeHeavy()
        {
            // GDD: A = 0.55, F = 45Hz, T = 0.40s, roll +/- 1.5 deg
            Shake(0.55f, 45f, 0.40f, 1.5f);
        }

        public Vector3 ShakeOffset { get; private set; } = Vector3.zero;
        public float ShakeRoll { get; private set; } = 0f;

        public void Shake(float amplitude, float frequency, float duration, float maxRoll)
        {
            float strength = SettingsService.Current.screenShake; // accessibility slider (spec 9.6)
            if (strength <= 0.001f) return;
            amplitude *= strength;
            maxRoll *= strength;

            if (_shakeRoutine != null)
            {
                StopCoroutine(_shakeRoutine);
            }
            _shakeRoutine = StartCoroutine(ShakeCoroutine(amplitude, frequency, duration, maxRoll));
        }

        private IEnumerator ShakeCoroutine(float amplitude, float frequency, float duration, float maxRoll)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float decay = 1f - (elapsed / duration);
                float currentAmp = amplitude * decay;

                float offsetX = (Mathf.PerlinNoise(Time.time * frequency, 0f) * 2f - 1f) * currentAmp;
                float offsetY = (Mathf.PerlinNoise(0f, Time.time * frequency) * 2f - 1f) * currentAmp;

                ShakeOffset = new Vector3(offsetX, offsetY, 0f);

                if (maxRoll > 0.01f)
                {
                    ShakeRoll = (Mathf.PerlinNoise(Time.time * frequency, Time.time * frequency) * 2f - 1f) * maxRoll * decay;
                }
                else
                {
                    ShakeRoll = 0f;
                }

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            ShakeOffset = Vector3.zero;
            ShakeRoll = 0f;
            _shakeRoutine = null;
        }
    }
}
