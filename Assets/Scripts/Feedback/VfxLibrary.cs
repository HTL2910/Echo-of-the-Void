using UnityEngine;
using EchoOfTheVoid.Core;

namespace EchoOfTheVoid.Feedback
{
    /// <summary>The effects the artists deliver as prefabs (Assets/Prefabs/VFX). Do not renumber: prefab slots are indexed by value.</summary>
    public enum VfxId
    {
        SlashArcPrime = 0,
        SlashArcEcho = 1,
        ShiftWave = 2,
        DashDust = 3,
        LandDust = 4,
        ImpactClean = 5,
        ImpactDeflect = 6,
        EnemyDeath = 7,
    }

    /// <summary>
    /// Plays one-shot particle prefabs. Gameplay code only says <c>VfxLibrary.Play(id, position)</c>; missing library
    /// or missing prefab is a silent no-op, so the game runs (without the effect) when art is not in yet.
    /// The prefabs destroy themselves (Stop Action = Destroy).
    /// </summary>
    public class VfxLibrary : MonoBehaviour
    {
        public static VfxLibrary Instance { get; private set; }

        [SerializeField] private GameObject[] prefabs = new GameObject[8];

        private RealmType _lastRealm = RealmType.Prime;
        private Transform _player;

        public static bool Has(VfxId id) => Instance != null && Instance.Get(id) != null;

        public static void Play(VfxId id, Vector3 position, bool flipX = false)
        {
            if (Instance != null) Instance.Spawn(id, position, flipX);
        }

        public void Configure(VfxId id, GameObject prefab)
        {
            int index = (int)id;
            if (prefabs == null || prefabs.Length <= index) System.Array.Resize(ref prefabs, System.Enum.GetValues(typeof(VfxId)).Length);
            prefabs[index] = prefab;
        }

        private GameObject Get(VfxId id)
        {
            int index = (int)id;
            return (prefabs != null && index >= 0 && index < prefabs.Length) ? prefabs[index] : null;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            RealityEventBus.OnRealmSwitched += HandleRealmSwitched;
        }

        private void OnDisable()
        {
            RealityEventBus.OnRealmSwitched -= HandleRealmSwitched;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            if (RealityManager.Instance != null) _lastRealm = RealityManager.Instance.CurrentRealm;
        }

        private void Spawn(VfxId id, Vector3 position, bool flipX)
        {
            var prefab = Get(id);
            if (prefab == null) return;

            var instance = Instantiate(prefab, position, Quaternion.identity);
            if (flipX)
            {
                Vector3 scale = instance.transform.localScale;
                scale.x = -scale.x;
                instance.transform.localScale = scale;
            }
        }

        private void HandleRealmSwitched(RealmType realm)
        {
            // The manager also broadcasts the initial realm on start-up: only a real change gets a shockwave
            if (realm == _lastRealm) return;
            _lastRealm = realm;

            if (_player == null)
            {
                var go = GameObject.FindGameObjectWithTag("Player");
                if (go != null) _player = go.transform;
            }
            if (_player != null) Spawn(VfxId.ShiftWave, _player.position, false);
        }
    }
}
