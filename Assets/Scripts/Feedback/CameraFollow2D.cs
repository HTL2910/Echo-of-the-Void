using UnityEngine;

namespace EchoOfTheVoid.Feedback
{
    public class CameraFollow2D : MonoBehaviour
    {
        [Header("Target Tracking")]
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 1.5f, -10f);
        [SerializeField] private float smoothTime = 0.18f;
        [SerializeField] private float lookAheadFactor = 1.5f;

        [Header("Room Bounds Clamping")]
        [SerializeField] private bool useBounds = false;
        [SerializeField] private Vector2 minBounds = new Vector2(-50f, -10f);
        [SerializeField] private Vector2 maxBounds = new Vector2(50f, 20f);

        private Vector3 _currentVelocity;
        private Camera _cam;

        private void Awake()
        {
            _cam = GetComponent<Camera>();
        }

        private void Start()
        {
            if (target == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) target = player.transform;
            }
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) target = player.transform;
                else return;
            }

            // Look-ahead based on facing or movement
            float lookAheadX = 0f;
            var rb = target.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                lookAheadX = Mathf.Clamp(rb.linearVelocity.x, -1f, 1f) * lookAheadFactor;
            }

            Vector3 targetPos = target.position + offset + new Vector3(lookAheadX, 0f, 0f);

            if (useBounds && _cam != null && _cam.orthographic)
            {
                float vertExtent = _cam.orthographicSize;
                float horzExtent = vertExtent * Screen.width / Screen.height;

                targetPos.x = Mathf.Clamp(targetPos.x, minBounds.x + horzExtent, maxBounds.x - horzExtent);
                targetPos.y = Mathf.Clamp(targetPos.y, minBounds.y + vertExtent, maxBounds.y - vertExtent);
            }

            Vector3 nextPos = Vector3.SmoothDamp(transform.position, targetPos, ref _currentVelocity, smoothTime);

            if (CameraShakeManager.Instance != null)
            {
                nextPos += CameraShakeManager.Instance.ShakeOffset;
                float roll = CameraShakeManager.Instance.ShakeRoll;
                transform.localRotation = (Mathf.Abs(roll) > 0.001f) ? Quaternion.Euler(0f, 0f, roll) : Quaternion.identity;
            }

            transform.position = nextPos;
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        public void SetBounds(Vector2 min, Vector2 max)
        {
            useBounds = true;
            minBounds = min;
            maxBounds = max;
        }
    }
}
