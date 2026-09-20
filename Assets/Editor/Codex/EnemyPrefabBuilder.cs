using UnityEngine;
using UnityEditor;
using EchoOfTheVoid.Enemies;
using EchoOfTheVoid.Core;

namespace EchoOfTheVoid.Editor.Codex
{
    /// <summary>
    /// Editor script to create enemy prefabs.
    /// Menu: Tools/Echo of the Void/Codex/Create Enemy Prefabs
    /// Only creates prefabs that don't already exist to preserve Anti's art changes.
    /// </summary>
    public static class EnemyPrefabBuilder
    {
        [MenuItem("Tools/Echo of the Void/Codex/Create Enemy Prefabs")]
        public static void CreateAllEnemyPrefabs()
        {
            CreateVoidStriderPrefab();
            CreatePrismSentryPrefab();
            Debug.Log("[Codex] Enemy prefabs created (skipped if already exist).");
        }

        private static void CreateVoidStriderPrefab()
        {
            const string path = "Assets/Prefabs/Enemies/VoidStrider.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            {
                Debug.Log($"[Codex] Skipped VoidStrider — prefab already exists at {path}");
                return;
            }

            var root = new GameObject("VoidStrider");
            root.transform.localScale = Vector3.one;
            root.tag = "Enemy";
            root.layer = LayerMask.NameToLayer("Enemy") != -1 ? LayerMask.NameToLayer("Enemy") : 0;

            root.AddComponent<Rigidbody2D>();
            var col = root.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.8f, 1.0f);

            root.AddComponent<EnemyAnimationDriver>();
            root.AddComponent<EnemyRespawner>();
            root.AddComponent<VoidStrider>();

            // Visual child (Anti replaces sprite/anim here)
            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform);
            visual.transform.localPosition = Vector3.zero;
            var sr = visual.AddComponent<SpriteRenderer>();
            sr.color = new Color(0.85f, 0.25f, 1.0f, 1f); // Echo purple placeholder
            visual.AddComponent<Animator>();

            SavePrefab(root, path);
        }

        private static void CreatePrismSentryPrefab()
        {
            const string path = "Assets/Prefabs/Enemies/PrismSentry.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            {
                Debug.Log($"[Codex] Skipped PrismSentry — prefab already exists at {path}");
                return;
            }

            var root = new GameObject("PrismSentry");
            root.transform.localScale = Vector3.one;
            root.tag = "Enemy";
            root.layer = LayerMask.NameToLayer("Enemy") != -1 ? LayerMask.NameToLayer("Enemy") : 0;

            var rb = root.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeAll;
            var col = root.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.6f, 0.6f);

            root.AddComponent<EnemyAnimationDriver>();
            root.AddComponent<EnemyRespawner>();

            // RailCable child
            var cable = new GameObject("RailCable");
            cable.transform.SetParent(root.transform);
            var cableCol = cable.AddComponent<BoxCollider2D>();
            cableCol.isTrigger = true;
            cable.AddComponent<EchoOfTheVoid.Enemies.RailCable>();

            root.AddComponent<PrismSentry>();

            // Visual child
            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform);
            visual.transform.localPosition = Vector3.zero;
            var sr = visual.AddComponent<SpriteRenderer>();
            sr.color = new Color(0.2f, 0.9f, 1.0f, 1f); // Prime cyan placeholder
            visual.AddComponent<Animator>();

            SavePrefab(root, path);
        }

        private static void SavePrefab(GameObject root, string path)
        {
            // Ensure directory exists
            string dir = System.IO.Path.GetDirectoryName(path);
            if (!AssetDatabase.IsValidFolder(dir))
                System.IO.Directory.CreateDirectory(dir);

            bool success;
            PrefabUtility.SaveAsPrefabAsset(root, path, out success);
            Object.DestroyImmediate(root);

            if (success)
                Debug.Log($"[Codex] Created prefab: {path}");
            else
                Debug.LogError($"[Codex] Failed to create prefab: {path}");
        }
    }
}
