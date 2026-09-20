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

            GameObject ghostObj = new GameObject("GhostTrail_Clone");
            Transform source = sourceRenderer.transform;
            ghostObj.transform.position = source.position;
            ghostObj.transform.rotation = source.rotation;
            ghostObj.transform.localScale = source.lossyScale;

            SpriteRenderer sr = ghostObj.AddComponent<SpriteRenderer>();
            sr.sprite = sourceRenderer.sprite;
            sr.color = baseColor;
            sr.sortingLayerID = sourceRenderer.sortingLayerID;
            sr.sortingOrder = sourceRenderer.sortingOrder - 1;
            sr.flipX = sourceRenderer.flipX;

            StartCoroutine(FadeAndDestroy(ghostObj, sr, baseColor, ghostLifetime));
        }

        private IEnumerator FadeAndDestroy(GameObject obj, SpriteRenderer sr, Color startColor, float lifetime)
        {
            float elapsed = 0f;
            while (elapsed < lifetime)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(startColor.a, 0f, elapsed / lifetime);
                Color c = startColor;
                c.a = alpha;
                if (sr != null) sr.color = c;
                yield return null;
            }

            if (obj != null) Destroy(obj);
        }
    }
}
