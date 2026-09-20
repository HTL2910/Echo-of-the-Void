using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using EchoOfTheVoid.Core;
using EchoOfTheVoid.Environment;
using EchoOfTheVoid.Player;
using EchoOfTheVoid.UI;

namespace EchoOfTheVoid.Editor
{
    public static class SceneGenerator
    {
        private const string SPRITE_PATH = "Assets/Sprites/square.png";
        private const string SCENE_DIR = "Assets/Scenes";
        private const string SCENE_PATH = "Assets/Scenes/Prototype_Level1.unity";

        [MenuItem("Tools/Echo of the Void/Generate Prototype Scene")]
        public static void GeneratePrototypeScene()
        {
            Debug.Log("[SceneGenerator] Starting generation of Prototype Level 1...");

            // 1. Ensure Sprite Asset exists
            Sprite boxSprite = EnsureSquareSprite();

            // 2. Create new Empty Scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 3. Setup Camera
            GameObject cameraObj = new GameObject("Main Camera");
            cameraObj.tag = "MainCamera";
            cameraObj.transform.position = new Vector3(0f, 3f, -10f);
            Camera cam = cameraObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6.5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.07f, 0.11f, 1f); // Dark Void theme
            cameraObj.AddComponent<AudioListener>();

            // URP 2D Camera Data
            var cameraData = cameraObj.AddComponent<UniversalAdditionalCameraData>();
            cameraData.renderPostProcessing = true;

            // 4. Setup 2D Global Light (URP)
            GameObject lightObj = new GameObject("Global 2D Light");
            var light2D = lightObj.AddComponent<Light2D>();
            light2D.lightType = Light2D.LightType.Global;
            light2D.color = Color.white;
            light2D.intensity = 1.0f;

            // 5. Setup Managers & HUD
            GameObject managerObj = new GameObject("RealityManager");
            managerObj.AddComponent<RealityManager>();
            managerObj.AddComponent<RealityUIIndicator>();

            // 6. Build Environment (Hierarchy: Level)
            GameObject levelRoot = new GameObject("Environment");

            // Neutral Floor (Always solid)
            CreatePlatform(levelRoot.transform, "Floor_Neutral", 
                new Vector3(0f, -2.5f, 0f), new Vector3(36f, 1.5f, 1f), 
                new Color(0.2f, 0.25f, 0.3f, 1f), boxSprite, isRealitySensitive: false);

            // Left & Right boundary walls
            CreatePlatform(levelRoot.transform, "Wall_Left", 
                new Vector3(-17.5f, 4f, 0f), new Vector3(1.5f, 12f, 1f), 
                new Color(0.15f, 0.18f, 0.22f, 1f), boxSprite, isRealitySensitive: false);

            CreatePlatform(levelRoot.transform, "Wall_Right", 
                new Vector3(17.5f, 4f, 0f), new Vector3(1.5f, 12f, 1f), 
                new Color(0.15f, 0.18f, 0.22f, 1f), boxSprite, isRealitySensitive: false);

            // GDD Sandbox Section: Alternating Prime (Cyan) and Echo (Purple) platforms
            Color primeColor = new Color(0.0f, 0.85f, 1.0f, 1.0f);  // Cyan
            Color echoColor = new Color(0.8f, 0.2f, 1.0f, 1.0f);   // Purple

            // Step 1: Prime Platform (Safe first jump)
            CreateRealityPlatform(levelRoot.transform, "Platform_Prime_1",
                new Vector3(-8f, 0f, 0f), new Vector3(4.5f, 0.6f, 1f),
                RealmType.Prime, primeColor, boxSprite);

            // Step 2: Echo Platform (Requires shift in mid-air to land)
            CreateRealityPlatform(levelRoot.transform, "Platform_Echo_1",
                new Vector3(-2f, 1.8f, 0f), new Vector3(4.5f, 0.6f, 1f),
                RealmType.Echo, echoColor, boxSprite);

            // Step 3: Prime Platform 2
            CreateRealityPlatform(levelRoot.transform, "Platform_Prime_2",
                new Vector3(4f, 3.5f, 0f), new Vector3(4.5f, 0.6f, 1f),
                RealmType.Prime, primeColor, boxSprite);

            // Step 4: High Echo Ledge / Goal
            CreateRealityPlatform(levelRoot.transform, "Platform_Echo_HighLedge",
                new Vector3(10.5f, 5.2f, 0f), new Vector3(6f, 0.6f, 1f),
                RealmType.Echo, echoColor, boxSprite);

            // 7. Setup Player
            GameObject playerObj = new GameObject("Player");
            playerObj.transform.position = new Vector3(-13f, -1f, 0f);

            var rb = playerObj.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.freezeRotation = true;
            rb.gravityScale = 0f;

            var col = playerObj.AddComponent<BoxCollider2D>();
            // GDD specs: 14px width x 26px height (0.875 x 1.625 units)
            col.size = new Vector2(0.875f, 1.625f);

            var playerRenderer = playerObj.AddComponent<SpriteRenderer>();
            playerRenderer.sprite = boxSprite;
            playerRenderer.color = new Color(0.95f, 0.95f, 0.95f, 1.0f);
            playerObj.transform.localScale = new Vector3(0.875f, 1.625f, 1f);

            playerObj.AddComponent<PlayerController>();

            // 8. Save Scene
            if (!Directory.Exists(SCENE_DIR))
            {
                Directory.CreateDirectory(SCENE_DIR);
            }

            bool saveSuccess = EditorSceneManager.SaveScene(scene, SCENE_PATH);
            Debug.Log($"[SceneGenerator] SaveScene success: {saveSuccess} at {SCENE_PATH}");

            // 9. Add scene to EditorBuildSettings
            EditorBuildSettingsScene[] originalScenes = EditorBuildSettings.scenes;
            bool existsInBuild = false;
            foreach (var s in originalScenes)
            {
                if (s.path == SCENE_PATH)
                {
                    existsInBuild = true;
                    break;
                }
            }

            if (!existsInBuild)
            {
                var newScenes = new EditorBuildSettingsScene[originalScenes.Length + 1];
                originalScenes.CopyTo(newScenes, 0);
                newScenes[newScenes.Length - 1] = new EditorBuildSettingsScene(SCENE_PATH, true);
                EditorBuildSettings.scenes = newScenes;
                Debug.Log($"[SceneGenerator] Added {SCENE_PATH} to EditorBuildSettings.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[SceneGenerator] Scene generation completed successfully!");
        }

        private static Sprite EnsureSquareSprite()
        {
            string spriteDir = Path.GetDirectoryName(SPRITE_PATH);
            if (!Directory.Exists(spriteDir))
            {
                Directory.CreateDirectory(spriteDir);
            }

            if (!File.Exists(SPRITE_PATH))
            {
                Texture2D tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
                Color[] colors = new Color[16 * 16];
                for (int i = 0; i < colors.Length; i++) colors[i] = Color.white;
                tex.SetPixels(colors);
                tex.Apply();

                File.WriteAllBytes(SPRITE_PATH, tex.EncodeToPNG());
                AssetDatabase.ImportAsset(SPRITE_PATH, ImportAssetOptions.ForceUpdate);

                TextureImporter importer = AssetImporter.GetAtPath(SPRITE_PATH) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spritePixelsPerUnit = 16f;
                    importer.filterMode = FilterMode.Point;
                    importer.SaveAndReimport();
                }
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_PATH);
        }

        private static void CreatePlatform(Transform parent, string name, Vector3 pos, Vector3 size, Color color, Sprite sprite, bool isRealitySensitive)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent);
            obj.transform.position = pos;
            obj.transform.localScale = size;

            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;

            var col = obj.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;
        }

        private static void CreateRealityPlatform(Transform parent, string name, Vector3 pos, Vector3 size, RealmType realm, Color color, Sprite sprite)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent);
            obj.transform.position = pos;
            obj.transform.localScale = size;

            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;

            var col = obj.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;

            var realityPlat = obj.AddComponent<RealityPlatform>();
            realityPlat.Configure(realm, color);
        }
    }
}
