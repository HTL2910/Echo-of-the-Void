using UnityEngine;
using EchoOfTheVoid.Core;
using EchoOfTheVoid.UI;

namespace EchoOfTheVoid.Environment
{
    public class LevelGoalTrigger : MonoBehaviour
    {
        private bool _isTriggered;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_isTriggered) return;
            if (other.CompareTag("Player"))
            {
                _isTriggered = true;

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayResonance();
                }

                if (PlayerHUD.Instance != null)
                {
                    PlayerHUD.Instance.ShowAnnouncement("LEVEL COMPLETE!\nVERTICAL SLICE VERIFIED", 4f);
                }

                Debug.Log("[LevelGoalTrigger] Player reached Level Goal! Vertical Slice complete.");
            }
        }
    }
}
