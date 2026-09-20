using UnityEngine;
using EchoOfTheVoid.Core;
using EchoOfTheVoid.Player;
using EchoOfTheVoid.UI;

namespace EchoOfTheVoid.Environment
{
    /// <summary>Touch to acquire an ability (boss reward / key item). Removes itself if the player already owns it.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class AbilityPickup : MonoBehaviour
    {
        [SerializeField] private AbilityFlags ability = AbilityFlags.WallJump;
        [SerializeField] private string displayName = "PISTON BOOTS";

        public AbilityFlags Ability => ability;

        public void Configure(AbilityFlags flag, string name)
        {
            ability = flag;
            displayName = name;
        }

        private void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var abilities = other.GetComponentInParent<AbilitySet>();
            if (abilities == null) return;

            if (abilities.Unlock(ability) && PlayerHUD.Instance != null)
            {
                PlayerHUD.Instance.ShowAnnouncement($"{displayName} ACQUIRED\n{ability}", 3f);
            }
            Destroy(gameObject);
        }
    }
}
