using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using EchoOfTheVoid.Core;
using EchoOfTheVoid.Combat;
using EchoOfTheVoid.Environment;
using EchoOfTheVoid.Feedback;
using EchoOfTheVoid.Player;
using EchoOfTheVoid.Enemies;
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
            Debug.Log("[SceneGenerator] Generating Vertical Slice Prototype Level...");

            // 1. Ensure Sprite Asset
            Sprite boxSprite = EnsureSquareSprite();

            // 2. Create new Scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 3. Setup Camera & Managers
            GameObject cameraObj = new GameObject("Main Camera");
            cameraObj.tag = "MainCamera";
            cameraObj.transform.position = new Vector3(0f, 3f, -10f);
            Camera cam = cameraObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 7.0f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f, 0.06f, 0.09f, 1f); // Dark Void
            cameraObj.AddComponent<AudioListener>();

            var cameraData = cameraObj.AddComponent<UniversalAdditionalCameraData>();
            cameraData.renderPostProcessing = true;

            // Try adding Cinemachine Brain if assembly is loaded
            var brainType = System.Type.GetType("Unity.Cinemachine.CinemachineBrain, Unity.Cinemachine") 
                         ?? System.Type.GetType("Cinemachine.CinemachineBrain, Cinemachine");
            if (brainType != null)
            {
                cameraObj.AddComponent(brainType);
            }

            // 4. Setup 2D Global Light (URP)
            GameObject lightObj = new GameObject("Global 2D Light");
            var light2D = lightObj.AddComponent<Light2D>();
            light2D.lightType = Light2D.LightType.Global;
            light2D.color = Color.white;
            light2D.intensity = 1.0f;

            // 5. Setup Managers
            GameObject managersObj = new GameObject("Managers");
            managersObj.AddComponent<RealityManager>();
            managersObj.AddComponent<HitStopManager>();
            managersObj.AddComponent<CameraShakeManager>();
            managersObj.AddComponent<RealityUIIndicator>();

            // 6. Setup Player
            GameObject playerObj = new GameObject("Player");
            playerObj.tag = "Player";
            playerObj.transform.position = new Vector3(-12f, 0f, 0f);

            var rb = playerObj.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.freezeRotation = true;
            rb.gravityScale = 0f;

            var col = playerObj.AddComponent<BoxCollider2D>();
            // GDD metrics: 14px x 26px (~0.875 x 1.625)
            col.size = new Vector2(0.875f, 1.625f);

            var playerRenderer = playerObj.AddComponent<SpriteRenderer>();
            playerRenderer.sprite = boxSprite;
            playerRenderer.color = new Color(0.95f, 0.95f, 0.95f, 1f);
            playerObj.transform.localScale = new Vector3(0.875f, 1.625f, 1f);

            // Add Player Core Components
            playerObj.AddComponent<SquashAndStretch>();
            playerObj.AddComponent<GhostTrail>();
            playerObj.AddComponent<PlayerStats>();
            playerObj.AddComponent<PlayerCombat>();
            playerObj.AddComponent<PlayerController>();

            // Cinemachine Camera follow
            var cmCamType = System.Type.GetType("Unity.Cinemachine.CinemachineCamera, Unity.Cinemachine") 
                         ?? System.Type.GetType("Cinemachine.CinemachineVirtualCamera, Cinemachine");
            if (cmCamType != null)
            {
                GameObject cmObj = new GameObject("CinemachineCamera");
                var vcam = cmObj.AddComponent(cmCamType);
                var followProp = cmCamType.GetProperty("Follow");
                if (followProp != null) followProp.SetValue(vcam, playerObj.transform);
                var targetProp = cmCamType.GetProperty("Target");
                if (targetProp != null) targetProp.SetValue(vcam, playerObj.transform);
            }

            // 7. Build Environment (Level 1-1 Sandbox)
            GameObject levelRoot = new GameObject("Environment");

            Color neutralColor = new Color(0.2f, 0.24f, 0.28f, 1f);
            Color primeColor = new Color(0.0f, 0.85f, 1.0f, 1f);  // Cyan
            Color echoColor = new Color(0.85f, 0.25f, 1.0f, 1f); // Purple

            // Main Arena Floor (Wide: 60 units)
            CreatePlatform(levelRoot.transform, "Floor_Main", new Vector3(8f, -2.5f, 0f), new Vector3(64f, 1.5f, 1f), neutralColor, boxSprite);

            // Boundary Walls
            CreatePlatform(levelRoot.transform, "Wall_Left_Outer", new Vector3(-24f, 6f, 0f), new Vector3(1.5f, 16f, 1f), neutralColor, boxSprite);
            CreatePlatform(levelRoot.transform, "Wall_Right_Outer", new Vector3(38f, 6f, 0f), new Vector3(1.5f, 16f, 1f), neutralColor, boxSprite);

            // Wall Jump Shaft (Vertical Chute for Wall Slide & Jump practice)
            CreatePlatform(levelRoot.transform, "Wall_Shaft_Left", new Vector3(-18f, 4f, 0f), new Vector3(1.2f, 10f, 1f), neutralColor, boxSprite);
            CreatePlatform(levelRoot.transform, "Wall_Shaft_Right", new Vector3(-14.5f, 4f, 0f), new Vector3(1.2f, 10f, 1f), neutralColor, boxSprite);

            // Platforming Section 1: Alternating Reality Platforms
            CreateRealityPlatform(levelRoot.transform, "Platform_Prime_1", new Vector3(-9f, 0f, 0f), new Vector3(4.5f, 0.6f, 1f), RealmType.Prime, primeColor, boxSprite);
            CreateRealityPlatform(levelRoot.transform, "Platform_Echo_1", new Vector3(-3f, 1.8f, 0f), new Vector3(4.5f, 0.6f, 1f), RealmType.Echo, echoColor, boxSprite);
            CreateRealityPlatform(levelRoot.transform, "Platform_Prime_2", new Vector3(3f, 3.5f, 0f), new Vector3(4.5f, 0.6f, 1f), RealmType.Prime, primeColor, boxSprite);
            CreateRealityPlatform(levelRoot.transform, "Platform_Echo_HighLedge", new Vector3(8f, 5.2f, 0f), new Vector3(5.5f, 0.6f, 1f), RealmType.Echo, echoColor, boxSprite);

            // 8. Combat Arena Section (Entities & Dummies)
            GameObject combatRoot = new GameObject("Combat_Entities");

            // Training Dummy Prime (Cyan)
            CreateDummy(combatRoot.transform, "Dummy_Prime", new Vector3(10f, -1.2f, 0f), RealmType.Prime, boxSprite);

            // Training Dummy Echo (Purple)
            CreateDummy(combatRoot.transform, "Dummy_Echo", new Vector3(14f, -1.2f, 0f), RealmType.Echo, boxSprite);

            // ChronoCrawler Mob (Prime Crawler)
            CreateCrawler(combatRoot.transform, "Crawler_Prime", new Vector3(20f, -1.2f, 0f), boxSprite);

            // VoidWeaver Mob (Echo Flying Sniper)
            CreateVoidWeaver(combatRoot.transform, "Weaver_Echo", new Vector3(26f, 3.0f, 0f), boxSprite);

            // 9. Save Scene
            if (!Directory.Exists(SCENE_DIR)) Directory.CreateDirectory(SCENE_DIR);
            EditorSceneManager.SaveScene(scene, SCENE_PATH);
            Debug.Log($"[SceneGenerator] Saved Prototype Scene to {SCENE_PATH}");

            // Ensure in Build Settings
            EnsureBuildSettings(SCENE_PATH);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[SceneGenerator] Generation completed successfully!");
        }

        private static Sprite EnsureSquareSprite()
        {
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

        private static void CreatePlatform(Transform parent, string name, Vector3 pos, Vector3 size, Color color, Sprite sprite)
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

        private static void CreateDummy(Transform parent, string name, Vector3 pos, RealmType realm, Sprite sprite)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent);
            obj.transform.position = pos;
            obj.transform.localScale = new Vector3(1.2f, 2.0f, 1f);

            var rb = obj.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.freezeRotation = true;
            rb.mass = 5f;

            var col = obj.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;

            var sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;

            var dummy = obj.AddComponent<TrainingDummy>();
        }

        private static void CreateCrawler(Transform parent, string name, Vector3 pos, Sprite sprite)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent);
            obj.transform.position = pos;
            obj.transform.localScale = new Vector3(1.4f, 1.0f, 1f);

            var rb = obj.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.freezeRotation = true;

            var col = obj.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;

            var sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;

            obj.AddComponent<ChronoCrawler>();
        }

        private static void CreateVoidWeaver(Transform parent, string name, Vector3 pos, Sprite sprite)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent);
            obj.transform.position = pos;
            obj.transform.localScale = new Vector3(1.2f, 1.2f, 1f);

            var rb = obj.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            rb.freezeRotation = true;

            var col = obj.AddComponent<CircleCollider2D>();
            col.radius = 0.6f;

            var sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;

            obj.AddComponent<VoidWeaver>();
        }

        private static void EnsureBuildSettings(string scenePath)
        {
            var scenes = EditorBuildSettings.scenes;
            foreach (var s in scenes)
            {
                if (s.path == scenePath) return;
            }
            var newScenes = new EditorBuildSettingsScene[scenes.Length + 1];
            scenes.CopyTo(newScenes, 0);
            newScenes[newScenes.Length - 1] = new EditorBuildSettingsScene(scenePath, true);
            EditorBuildSettings.scenes = newScenes;
        }
    }
}
