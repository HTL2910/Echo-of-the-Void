using System.Collections;
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
    /// Plays one-shot particle prefabs via object pooling (L14 Performance).
    /// Gameplay code only says <c>VfxLibrary.Play(id, position)</c>; missing library or prefab is a silent no-op.
    /// Each VFX prefab has 2-3 pooled instances reused via SetActive.
    /// </summary>
    public class VfxLibrary : MonoBehaviour
    {
        public static VfxLibrary Instance { get; private set; }

        [SerializeField] private GameObject[] prefabs = new GameObject[8];

        private RealmType _lastRealm = RealmType.Prime;
        private Transform _player;
        private ObjectPool<ParticleSystem>[] _pools;

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

            _pools ??= new ObjectPool<ParticleSystem>[prefabs.Length];
            if (_pools[index] != null) _pools[index].Clear();
            _pools[index] = null;
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
            _pools = new ObjectPool<ParticleSystem>[8];
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
            if (Instance == this)
            {
                if (_pools != null)
                    foreach (var pool in _pools)
                        pool?.Clear();
                Instance = null;
            }
        }

        private void Start()
        {
            if (RealityManager.Instance != null) _lastRealm = RealityManager.Instance.CurrentRealm;
        }

        private void Spawn(VfxId id, Vector3 position, bool flipX)
        {
            var prefab = Get(id);
            if (prefab == null) return;

            int index = (int)id;
            if (_pools[index] == null)
            {
                _pools[index] = new ObjectPool<ParticleSystem>(prefab, 2);
            }

            var ps = _pools[index].Get();
            ps.transform.position = position;
            ps.transform.rotation = Quaternion.identity;

            if (flipX)
            {
                Vector3 scale = ps.transform.localScale;
                scale.x = -Mathf.Abs(scale.x);
                ps.transform.localScale = scale;
            }

            ps.Play();
            StartCoroutine(StopAndReturn(ps, _pools[index]));
        }

        private IEnumerator StopAndReturn(ParticleSystem ps, ObjectPool<ParticleSystem> pool)
        {
            if (ps == null) yield break;
            yield return new WaitForSeconds(ps.main.duration + ps.main.startLifetimeMultiplier);
            ps.Stop();
            pool.Return(ps);
        }

        private void HandleRealmSwitched(RealmType realm)
        {
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
