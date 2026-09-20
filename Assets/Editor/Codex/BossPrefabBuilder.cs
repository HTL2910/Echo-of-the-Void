using UnityEngine;
using UnityEditor;
using EchoOfTheVoid.Bosses;
using EchoOfTheVoid.Core;

namespace EchoOfTheVoid.Editor.Codex
{
    /// <summary>
    /// Editor script to create boss prefabs.
    /// Menu: Tools/Echo of the Void/Codex/Create Boss Prefabs
    /// Only creates prefabs that don't already exist.
    /// </summary>
    public static class BossPrefabBuilder
    {
        [MenuItem("Tools/Echo of the Void/Codex/Create Boss Prefabs")]
        public static void CreateAllBossPrefabs()
        {
            CreateSentinel01Prefab();
            Debug.Log("[Codex] Boss prefabs created (skipped if already exist).");
        }

        private static void CreateSentinel01Prefab()
        {
            const string path = "Assets/Prefabs/Bosses/Sentinel01.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            {
                Debug.Log($"[Codex] Skipped Sentinel01 — prefab already exists at {path}");
                return;
            }

            var root = new GameObject("Sentinel01");
            root.transform.localScale = Vector3.one;
            root.tag = "Enemy";

            var rb = root.AddComponent<Rigidbody2D>();
            rb.constraints = RigidbodyConstraints2D.FreezePosition;

            var col = root.AddComponent<BoxCollider2D>();
            col.size = new Vector2(2.0f, 3.0f);

            root.AddComponent<Sentinel01>();

            // Visual child (placeholder rect — Anti replaces later)
            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform);
            visual.transform.localPosition = Vector3.zero;
            var sr = visual.AddComponent<SpriteRenderer>();
            sr.color = new Color(0.3f, 0.3f, 0.9f, 1f); // boss blue placeholder
            sr.drawMode = SpriteDrawMode.Sliced;
            visual.AddComponent<Animator>();

            // Laser origin child
            var laserOrigin = new GameObject("LaserOrigin");
            laserOrigin.transform.SetParent(root.transform);
            laserOrigin.transform.localPosition = new Vector3(0f, -1.5f, 0f);

            // Missile spawn point
            var missileSpawn = new GameObject("MissileSpawn");
            missileSpawn.transform.SetParent(root.transform);
            missileSpawn.transform.localPosition = new Vector3(0f, 1.5f, 0f);

            SavePrefab(root, path);
        }

        private static void SavePrefab(GameObject root, string path)
        {
            string dir = System.IO.Path.GetDirectoryName(path);
            if (!AssetDatabase.IsValidFolder(dir))
                System.IO.Directory.CreateDirectory(dir);

            bool success;
            PrefabUtility.SaveAsPrefabAsset(root, path, out success);
            Object.DestroyImmediate(root);

            if (success)
                Debug.Log($"[Codex] Created boss prefab: {path}");
            else
                Debug.LogError($"[Codex] Failed to create boss prefab: {path}");
        }
    }
}
