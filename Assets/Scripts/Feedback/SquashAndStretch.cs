using System.Collections;
using UnityEngine;

namespace EchoOfTheVoid.Feedback
{
    public class SquashAndStretch : MonoBehaviour
    {
        [SerializeField] private Transform targetVisual;

        private Vector3 _originalScale = Vector3.one;
        private Coroutine _squashRoutine;

        private void Awake()
        {
            if (targetVisual == null) targetVisual = transform;
            _originalScale = targetVisual.localScale;
        }

        public void ApplySquash(float scaleX, float scaleY, float duration)
        {
            if (targetVisual == null) return;

            if (_squashRoutine != null)
            {
                StopCoroutine(_squashRoutine);
            }
            _squashRoutine = StartCoroutine(SquashCoroutine(scaleX, scaleY, duration));
        }

        public void OnJump()
        {
            // GDD: ScaleX = 0.75, ScaleY = 1.25, T = 0.15s
            ApplySquash(0.75f, 1.25f, 0.15f);
        }

        public void OnLand()
        {
            // GDD: ScaleX = 1.30, ScaleY = 0.70, T = 0.12s
            ApplySquash(1.30f, 0.70f, 0.12f);
        }

        public void OnDash()
        {
            // GDD: ScaleX = 1.40, ScaleY = 0.70, T = 0.10s
            ApplySquash(1.40f, 0.70f, 0.10f);
        }

        private IEnumerator SquashCoroutine(float targetX, float targetY, float duration)
        {
            Vector3 startScale = new Vector3(_originalScale.x * targetX, _originalScale.y * targetY, _originalScale.z);
            targetVisual.localScale = startScale;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // Ease out quad
                float ease = 1f - (1f - t) * (1f - t);
                targetVisual.localScale = Vector3.Lerp(startScale, _originalScale, ease);
                yield return null;
            }

            targetVisual.localScale = _originalScale;
            _squashRoutine = null;
        }
    }
}
