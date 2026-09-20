using UnityEngine;
using EchoOfTheVoid.Core;

namespace EchoOfTheVoid.Environment
{
    [RequireComponent(typeof(Collider2D))]
    public class RealityPlatform : MonoBehaviour
    {
        [Header("Realm Configuration")]
        [SerializeField] private RealmType solidInRealm;
        [SerializeField] private Color activeColor = Color.white;
        [SerializeField] private float inactiveAlpha = 0.30f;

        private Collider2D _collider;
        private SpriteRenderer _renderer;

        public RealmType SolidInRealm => solidInRealm;

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
