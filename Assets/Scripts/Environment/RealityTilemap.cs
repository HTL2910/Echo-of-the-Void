using UnityEngine;
using UnityEngine.Tilemaps;
using EchoOfTheVoid.Core;

namespace EchoOfTheVoid.Environment
{
    /// <summary>
    /// Makes a whole Tilemap solid in one realm only: colliders switch on/off with the realm, the tiles fade when
    /// inactive, and the Reality Shift guard sees its tiles. Neutral geometry needs no such component.
    /// </summary>
    [RequireComponent(typeof(Tilemap))]
    public class RealityTilemap : MonoBehaviour, IRealityObstacle
    {
        [SerializeField] private RealmType solidInRealm = RealmType.Prime;
        [SerializeField] private Color activeColor = Color.white;
        [SerializeField] private float inactiveAlpha = 0.30f;

        private Tilemap _tilemap;
        private Collider2D[] _colliders;

        public RealmType SolidInRealm => solidInRealm;

        public void Configure(RealmType realm, Color color)
        {
            solidInRealm = realm;
            activeColor = color;
            if (_tilemap != null) Apply(CurrentRealm); // already running: refresh straight away
        }

        private static RealmType CurrentRealm =>
            RealityManager.Instance != null ? RealityManager.Instance.CurrentRealm : RealmType.Prime;

        private void Awake()
        {
            _tilemap = GetComponent<Tilemap>();
            _colliders = GetComponents<Collider2D>(); // TilemapCollider2D and/or CompositeCollider2D
        }

        private void OnEnable()
        {
            RealityObstacles.Register(this);
            RealityEventBus.OnRealmSwitched += Apply;
        }

        private void Start()
        {
            Apply(CurrentRealm); // after Configure(): the serialized realm is final by now
        }

        private void OnDisable()
        {
            RealityObstacles.Unregister(this);
            RealityEventBus.OnRealmSwitched -= Apply;
        }

        private void Apply(RealmType currentRealm)
        {
            bool solid = currentRealm == solidInRealm;
            foreach (var c in _colliders) c.enabled = solid;

            Color color = activeColor;
            color.a = solid ? activeColor.a : inactiveAlpha;
            _tilemap.color = color;
        }

        public bool Overlaps(Bounds worldBounds)
        {
            var min = _tilemap.WorldToCell(worldBounds.min);
            var max = _tilemap.WorldToCell(worldBounds.max);

            for (int x = min.x; x <= max.x; x++)
            {
                for (int y = min.y; y <= max.y; y++)
                {
                    if (_tilemap.HasTile(new Vector3Int(x, y, 0))) return true;
                }
            }
            return false;
        }
    }
}
