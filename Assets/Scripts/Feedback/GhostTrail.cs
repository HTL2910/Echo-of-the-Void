using System.Collections;
using UnityEngine;
using EchoOfTheVoid.Core;

namespace EchoOfTheVoid.Feedback
{
    public class GhostTrail : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer sourceRenderer;
        [SerializeField] private float spawnInterval = 0.03f;
        [SerializeField] private float ghostLifetime = 0.25f;

        private Coroutine _trailRoutine;
        private static ObjectPool<GhostInstance> _pool;

        private void Awake()
        {
            if (sourceRenderer == null) sourceRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        public void StartTrail(RealmType currentRealm)
        {
            StopTrail();
            _trailRoutine = StartCoroutine(EmitGhosts(currentRealm));
        }

        public void StopTrail()
        {
            if (_trailRoutine != null)
            {
                StopCoroutine(_trailRoutine);
                _trailRoutine = null;
            }
        }

        private IEnumerator EmitGhosts(RealmType realm)
        {
            Color ghostColor = (realm == RealmType.Prime)
                ? new Color(0.0f, 0.9f, 1.0f, 0.7f)   // Cyan
                : new Color(0.8f, 0.2f, 1.0f, 0.7f);  // Purple

            while (true)
            {
                SpawnSingleGhost(ghostColor);
                yield return new WaitForSeconds(spawnInterval);
            }
        }

        private void SpawnSingleGhost(Color baseColor)
        {
            if (sourceRenderer == null || sourceRenderer.sprite == null) return;

            if (_pool == null)
            {
                var ghostPrefab = new GameObject("GhostTrail_Clone");
                ghostPrefab.AddComponent<SpriteRenderer>();
                ghostPrefab.AddComponent<GhostInstance>();
                _pool = new ObjectPool<GhostInstance>(ghostPrefab, 20);
                Destroy(ghostPrefab);
            }

            var ghost = _pool.Get();
            Transform source = sourceRenderer.transform;
            ghost.transform.position = source.position;
            ghost.transform.rotation = source.rotation;
            ghost.transform.localScale = source.lossyScale;

            ghost.Configure(sourceRenderer.sprite, baseColor, sourceRenderer.sortingLayerID, sourceRenderer.sortingOrder - 1, sourceRenderer.flipX, ghostLifetime, _pool);
        }
    }

    public class GhostInstance : MonoBehaviour
    {
        private SpriteRenderer _sr;
        private ObjectPool<GhostInstance> _pool;
        private Coroutine _fadeRoutine;

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            if (_sr == null) _sr = gameObject.AddComponent<SpriteRenderer>();
        }

        public void Configure(Sprite sprite, Color color, int sortingLayerId, int sortingOrder, bool flipX, float lifetime, ObjectPool<GhostInstance> pool)
        {
            _pool = pool;
            _sr.sprite = sprite;
            _sr.color = color;
            _sr.sortingLayerID = sortingLayerId;
            _sr.sortingOrder = sortingOrder;

            var scale = transform.localScale;
            scale.x = flipX ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
            transform.localScale = scale;

            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            _fadeRoutine = StartCoroutine(FadeAndReturn(color, lifetime));
        }

        private IEnumerator FadeAndReturn(Color startColor, float lifetime)
        {
            float elapsed = 0f;
            while (elapsed < lifetime)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(startColor.a, 0f, elapsed / lifetime);
                Color c = startColor;
                c.a = alpha;
                _sr.color = c;
                yield return null;
            }

            if (_pool != null) _pool.Return(this);
        }
    }
}
