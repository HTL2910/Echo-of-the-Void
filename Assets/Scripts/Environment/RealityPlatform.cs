using UnityEngine;
using EchoOfTheVoid.Core;

namespace EchoOfTheVoid.Environment
{
    [RequireComponent(typeof(Collider2D))]
    public class RealityPlatform : MonoBehaviour, IRealityObstacle
    {
        [Header("Realm Configuration")]
        [SerializeField] private RealmType solidInRealm;
        [SerializeField] private Color activeColor = Color.white;
        [SerializeField] private float inactiveAlpha = 0.30f;

        private Collider2D _collider;
        private SpriteRenderer _renderer;

        public RealmType SolidInRealm => solidInRealm;

        /// <summary>World-space box of this platform, valid even while its collider is switched off.</summary>
        public Bounds WorldBounds
        {
            get
            {
                if (_collider == null) _collider = GetComponent<Collider2D>();
                if (_collider.enabled) return _collider.bounds;

                if (_collider is BoxCollider2D box)
                {
                    Vector3 center = transform.TransformPoint(box.offset);
                    Vector3 size = Vector3.Scale(box.size, transform.lossyScale);
                    return new Bounds(center, new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), 1f));
                }
                return (_renderer != null) ? _renderer.bounds : _collider.bounds;
            }
        }

        public bool Overlaps(Bounds worldBounds) => WorldBounds.Intersects(worldBounds);

        private void OnEnable() => RealityObstacles.Register(this);
        private void OnDisable() => RealityObstacles.Unregister(this);

        public void Configure(RealmType realm, Color baseColor)
        {
            solidInRealm = realm;
            activeColor = baseColor;
            if (_renderer != null)
            {
                _renderer.color = baseColor;
            }
        }

        private void Awake()
        {
            _collider = GetComponent<Collider2D>();
            _renderer = GetComponent<SpriteRenderer>();
            RealityEventBus.OnRealmSwitched += HandleRealmSwitch;
        }

        private void Start()
        {
            // Set initial state matching RealityManager
            if (RealityManager.Instance != null)
            {
                HandleRealmSwitch(RealityManager.Instance.CurrentRealm);
            }
            else
            {
                HandleRealmSwitch(RealmType.Prime);
            }
        }

        private void OnDestroy()
        {
            RealityEventBus.OnRealmSwitched -= HandleRealmSwitch;
        }

        private void HandleRealmSwitch(RealmType currentRealm)
        {
            bool isSolid = (currentRealm == solidInRealm);
            if (_collider != null)
            {
                _collider.enabled = isSolid;
            }

            if (_renderer != null)
            {
                Color c = activeColor;
                c.a = isSolid ? 1.0f : inactiveAlpha;
                _renderer.color = c;
            }
        }
    }
}
