using UnityEngine;
using EchoOfTheVoid.Core;

namespace EchoOfTheVoid.Enemies
{
    /// <summary>
    /// Represents an energy cable rail formed by Prism Sentry in Echo Realm.
    /// Exposes endpoints and direction so Claude can wire Player Rail Grind state.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class RailCable : MonoBehaviour
    {
        [SerializeField] private Vector2 startPoint;
        [SerializeField] private Vector2 endPoint;
        [SerializeField] private bool isActive = false;

        private Collider2D _col;

        public Vector2 StartPoint => startPoint;
        public Vector2 EndPoint => endPoint;
        public Vector2 Direction => (endPoint - startPoint).normalized;
        public float Length => Vector2.Distance(startPoint, endPoint);
        public bool IsActive => isActive;

        private void Awake()
        {
            _col = GetComponent<Collider2D>();
            if (_col != null) _col.isTrigger = true;
        }

        public void SetupCable(Vector2 start, Vector2 end)
        {
            startPoint = start;
            endPoint = end;
            UpdateCollider();
        }

        public void SetActive(bool active)
        {
            isActive = active;
            if (_col != null) _col.enabled = active;
        }

        private void UpdateCollider()
        {
            if (_col is BoxCollider2D box)
            {
                Vector2 mid = (startPoint + endPoint) * 0.5f;
                transform.position = mid;
                float dist = Vector2.Distance(startPoint, endPoint);
                box.size = new Vector2(dist, 0.4f);

                float angle = Mathf.Atan2(Direction.y, Direction.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }
        }
    }
}
