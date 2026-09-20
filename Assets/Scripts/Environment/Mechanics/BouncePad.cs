using UnityEngine;

namespace EchoOfTheVoid.Environment.Mechanics
{
    /// <summary>
    /// K3: Standalone bounce pad (spec §4). Always active in both realms.
    /// Launches any Rigidbody2D upward with configurable force.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class BouncePad : MonoBehaviour
    {
        [SerializeField] private float bounceForce = 20f;
        [SerializeField] private bool allowHorizontalMomentum = true;

        private SpriteRenderer _sprite;

        private void Awake()
        {
            _sprite = GetComponentInChildren<SpriteRenderer>();
            if (_sprite != null)
                _sprite.color = new Color(0.4f, 1f, 0.4f, 1f); // green
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            // Bounce only when contacted from above
            bool hitFromAbove = false;
            foreach (var contact in collision.contacts)
            {
                if (contact.normal.y > 0.7f) { hitFromAbove = true; break; }
            }
            if (!hitFromAbove) return;

            var rb = collision.rigidbody;
            if (rb == null) return;

            float xVel = allowHorizontalMomentum ? rb.linearVelocity.x : 0f;
            rb.linearVelocity = new Vector2(xVel, bounceForce);
        }
    }
}
