using UnityEngine;
using EchoOfTheVoid.Player;

namespace EchoOfTheVoid.Environment
{
    /// <summary>
    /// A purple air current where Kael may flip gravity (Gravity Inversion). Inside it, press the gravity key to walk on
    /// the ceiling; a second later after leaving, gravity returns by itself. Size it with the trigger collider.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class GravitonField : MonoBehaviour
    {
        private void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var controller = other.GetComponentInParent<PlayerController>();
            if (controller != null) controller.EnterGravitonField();
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            var controller = other.GetComponentInParent<PlayerController>();
            if (controller != null) controller.LeaveGravitonField();
        }
    }
}
