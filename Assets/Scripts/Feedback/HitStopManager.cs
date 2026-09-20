using System.Collections;
using UnityEngine;

namespace EchoOfTheVoid.Feedback
{
    public class HitStopManager : MonoBehaviour
    {
        public static HitStopManager Instance { get; private set; }

        private Coroutine _hitStopRoutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void TriggerHitStop(float duration, float timeScale = 0f)
        {
            if (duration <= 0f) return;

            if (_hitStopRoutine != null)
            {
                StopCoroutine(_hitStopRoutine);
            }
            _hitStopRoutine = StartCoroutine(HitStopCoroutine(duration, timeScale));
        }

        private IEnumerator HitStopCoroutine(float duration, float targetTimeScale)
        {
            float originalTimeScale = 1f;
            Time.timeScale = targetTimeScale;
            yield return new WaitForSecondsRealtime(duration);
            Time.timeScale = originalTimeScale;
            _hitStopRoutine = null;
        }
    }
}
