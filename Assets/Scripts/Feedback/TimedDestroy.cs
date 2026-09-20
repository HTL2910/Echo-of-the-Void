using UnityEngine;

namespace EchoOfTheVoid.Feedback
{
    /// <summary>Destroys its GameObject after a delay (warning markers, short-lived effects). Runs on scaled time.</summary>
    public class TimedDestroy : MonoBehaviour
    {
        [SerializeField] private float lifetime = 1.5f;

        public void SetLifetime(float seconds) => lifetime = seconds;

        private void Start()
        {
            Destroy(gameObject, lifetime);
        }
    }
}
