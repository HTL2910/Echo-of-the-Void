using UnityEngine;

namespace EchoOfTheVoid.Feedback
{
    /// <summary>
    /// Follows the player with look-ahead, stays inside the current room's bounds and layers camera shake on top.
    /// The smoothed "base" position is kept separately from the shaken position, so shake never feeds back into
    /// the follow (it would otherwise be damped away and drift).
    /// </summary>
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
        private Vector3 _basePosition;
        private bool _hasBase;
        private Camera _cam;

        public bool UsesBounds => useBounds;
        public Vector2 MinBounds => minBounds;
        public Vector2 MaxBounds => maxBounds;

        private void Awake()
        {
            _cam = GetComponent<Camera>();
        }

        private void Start()
        {
            FindTargetIfMissing();
        }

        private bool FindTargetIfMissing()
        {
            if (target != null) return true;
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return false;
            target = player.transform;
            return true;
        }

        private void LateUpdate()
        {
            if (!FindTargetIfMissing()) return;

            if (!_hasBase)
            {
                _basePosition = transform.position;
                _hasBase = true;
            }

            _basePosition = Vector3.SmoothDamp(_basePosition, ComputeTargetPosition(), ref _currentVelocity, smoothTime);

            Vector3 finalPosition = _basePosition;
            if (CameraShakeManager.Instance != null)
            {
                finalPosition += CameraShakeManager.Instance.ShakeOffset;
                float roll = CameraShakeManager.Instance.ShakeRoll;
                transform.localRotation = (Mathf.Abs(roll) > 0.001f) ? Quaternion.Euler(0f, 0f, roll) : Quaternion.identity;
            }
            transform.position = finalPosition;
        }

        private Vector3 ComputeTargetPosition()
        {
            // Look-ahead based on movement
            float lookAheadX = 0f;
            var rb = target.GetComponent<Rigidbody2D>();
            if (rb != null) lookAheadX = Mathf.Clamp(rb.linearVelocity.x, -1f, 1f) * lookAheadFactor;

            Vector3 targetPos = target.position + offset + new Vector3(lookAheadX, 0f, 0f);

            if (useBounds && _cam != null && _cam.orthographic)
            {
                float vertExtent = _cam.orthographicSize;
                float horzExtent = vertExtent * _cam.aspect;
                targetPos.x = ClampAxis(targetPos.x, minBounds.x, maxBounds.x, horzExtent);
                targetPos.y = ClampAxis(targetPos.y, minBounds.y, maxBounds.y, vertExtent);
            }
            return targetPos;
        }

        /// <summary>Keep the view inside [min, max]; a room smaller than the view is simply centred.</summary>
        private static float ClampAxis(float value, float min, float max, float extent)
        {
            if (max - min <= extent * 2f) return (min + max) * 0.5f;
            return Mathf.Clamp(value, min + extent, max - extent);
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

        /// <summary>Jump straight to where the camera should be (room change, respawn, load) without smoothing.</summary>
        public void SnapToTarget()
        {
            if (!FindTargetIfMissing()) return;
            _basePosition = ComputeTargetPosition();
            _hasBase = true;
            _currentVelocity = Vector3.zero;
            transform.position = _basePosition;
        }
    }
}
