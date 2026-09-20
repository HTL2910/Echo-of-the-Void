using System.IO;
using UnityEditor;
using UnityEngine;
using EchoOfTheVoid.Core;
using EchoOfTheVoid.Enemies;

namespace EchoOfTheVoid.Editor
{
    public class PixelArtPostprocessor : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (assetPath.Contains("Assets/Art/"))
            {
                TextureImporter importer = (TextureImporter)assetImporter;
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 16f; // Standard 16x16 from GDD
                importer.filterMode = FilterMode.Point; // Crisp pixel art
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
            }
        }
    }

    public static class AssetSetupUtility
    {
        private const string ENEMY_DATA_DIR = "Assets/Settings/Enemies";

        [MenuItem("Tools/Echo of the Void/Setup Pixel Art Textures")]
        public static void ReimportAllPixelAssets()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art" });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spritePixelsPerUnit = 16f;
                    importer.filterMode = FilterMode.Point;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.mipmapEnabled = false;
                    importer.SaveAndReimport();
                }
            }
            AssetDatabase.Refresh();
            Debug.Log("[PixelArtSetup] Finished configuring all pixel art assets in Assets/Art.");
        }

        public static void EnsureEnemyDataAssets()
        {
            if (!Directory.Exists(ENEMY_DATA_DIR))
            {
                Directory.CreateDirectory(ENEMY_DATA_DIR);
            }

            // 1. ChronoCrawler (Prime mob)
            CreateOrUpdateEnemySO($"{ENEMY_DATA_DIR}/EnemyData_ChronoCrawler.asset", data =>
            {
                data.EnemyName = "Chrono-Crawler";
                data.Realm = RealmType.Prime;
                data.MaxHealth = 40;
                data.ContactDamage = 10;
                data.MoveSpeed = 3.2f;
                data.AlertSpeed = 5.0f;
                data.DetectionRadius = 5.0f;
                data.PoiseMax = 40f;
                data.PatrolDistance = 6.0f;
            });

            // 2. VoidWeaver (Echo mob)
            CreateOrUpdateEnemySO($"{ENEMY_DATA_DIR}/EnemyData_VoidWeaver.asset", data =>
            {
                data.EnemyName = "Void Weaver";
                data.Realm = RealmType.Echo;
                data.MaxHealth = 35;
                data.ContactDamage = 0;
                data.MoveSpeed = 2.5f;
                data.AlertSpeed = 2.5f;
                data.DetectionRadius = 8.0f;
                data.PoiseMax = 30f;
                data.PatrolDistance = 4.0f;
            });

            // 3. Training Dummy Prime
            CreateOrUpdateEnemySO($"{ENEMY_DATA_DIR}/EnemyData_DummyPrime.asset", data =>
            {
                data.EnemyName = "Training Dummy (Prime)";
                data.Realm = RealmType.Prime;
                data.MaxHealth = 999;
                data.ContactDamage = 0;
                data.MoveSpeed = 0f;
                data.AlertSpeed = 0f;
                data.DetectionRadius = 0f;
                data.PoiseMax = 999f;
                data.PatrolDistance = 0f;
            });

            // 4. Training Dummy Echo
            CreateOrUpdateEnemySO($"{ENEMY_DATA_DIR}/EnemyData_DummyEcho.asset", data =>
            {
                data.EnemyName = "Training Dummy (Echo)";
                data.Realm = RealmType.Echo;
                data.MaxHealth = 999;
                data.ContactDamage = 0;
                data.MoveSpeed = 0f;
                data.AlertSpeed = 0f;
                data.DetectionRadius = 0f;
                data.PoiseMax = 999f;
                data.PatrolDistance = 0f;
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void CreateOrUpdateEnemySO(string path, System.Action<EnemyDataSO> configure)
        {
            var existing = AssetDatabase.LoadAssetAtPath<EnemyDataSO>(path);
            if (existing == null)
            {
                var newSO = ScriptableObject.CreateInstance<EnemyDataSO>();
                configure(newSO);
                AssetDatabase.CreateAsset(newSO, path);
            }
            else
            {
                configure(existing);
                EditorUtility.SetDirty(existing);
            }
        }
    }
}
