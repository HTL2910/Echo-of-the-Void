using UnityEngine;
using EchoOfTheVoid.Core;
using EchoOfTheVoid.UI;

namespace EchoOfTheVoid.Environment
{
    /// <summary>Place at the exit rift of each level. On player contact → shows announcement then advances to next level.</summary>
    public class LevelGoalTrigger : MonoBehaviour
    {
        [Tooltip("Delay in seconds between the 'Level Complete' announcement and loading the next scene.")]
        [SerializeField] private float nextLevelDelay = 3f;

        private bool _isTriggered;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_isTriggered) return;
            if (!other.CompareTag("Player")) return;

            _isTriggered = true;

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayResonance();

            var mgr = LevelManager.Instance;
            var def = mgr != null ? mgr.CurrentLevel : null;
            int next = def != null ? def.LevelIndex + 1 : 0;

            string line2 = next > LevelProgression.TotalLevels
                ? "JOURNEY COMPLETE"
                : (def != null ? $"ZONE {def.ZoneIndex}  ›  LEVEL {def.LevelIndex} / 20" : "LEVEL COMPLETE");

            if (PlayerHUD.Instance != null)
                PlayerHUD.Instance.ShowAnnouncement($"LEVEL COMPLETE!\n{line2}", nextLevelDelay);

            Debug.Log($"[LevelGoalTrigger] Level complete. Advancing after {nextLevelDelay}s.");
            Invoke(nameof(Advance), nextLevelDelay);
        }

        private void Advance() => LevelManager.GoNext();
    }
}

