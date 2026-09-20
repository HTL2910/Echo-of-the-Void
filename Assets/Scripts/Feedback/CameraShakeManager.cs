using System.Collections;
using UnityEngine;

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

        public void Shake(float amplitude, float frequency, float duration, float maxRoll)
        {
            if (_cameraTransform == null)
            {
                if (Camera.main != null) _cameraTransform = Camera.main.transform;
                else return;
            }

            if (_shakeRoutine != null)
            {
                StopCoroutine(_shakeRoutine);
            }
            _shakeRoutine = StartCoroutine(ShakeCoroutine(amplitude, frequency, duration, maxRoll));
        }

        private IEnumerator ShakeCoroutine(float amplitude, float frequency, float duration, float maxRoll)
        {
            float elapsed = 0f;
            Vector3 basePos = _cameraTransform.localPosition;
            Quaternion baseRot = _cameraTransform.localRotation;

            while (elapsed < duration)
            {
                float decay = 1f - (elapsed / duration);
                float currentAmp = amplitude * decay;

                float offsetX = (Mathf.PerlinNoise(Time.time * frequency, 0f) * 2f - 1f) * currentAmp;
                float offsetY = (Mathf.PerlinNoise(0f, Time.time * frequency) * 2f - 1f) * currentAmp;

                _cameraTransform.localPosition = new Vector3(basePos.x + offsetX, basePos.y + offsetY, basePos.z);

                if (maxRoll > 0.01f)
                {
                    float roll = (Mathf.PerlinNoise(Time.time * frequency, Time.time * frequency) * 2f - 1f) * maxRoll * decay;
                    _cameraTransform.localRotation = Quaternion.Euler(0f, 0f, roll);
                }

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            _cameraTransform.localPosition = basePos;
            _cameraTransform.localRotation = baseRot;
            _shakeRoutine = null;
        }
    }
}
