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
        private const string SLASH_SPRITE_PATH = "Assets/Sprites/SlashArc.png";
        private const string SCENE_DIR = "Assets/Scenes";
        private const string SCENE_PATH = "Assets/Scenes/Prototype_Level1.unity";
        private const string ENEMY_DATA_DIR = "Assets/Settings/Enemies";

        [MenuItem("Tools/Echo of the Void/Generate Prototype Scene")]
        public static void GeneratePrototypeScene()
        {
            Debug.Log("[SceneGenerator] Starting Vertical Slice Prototype Level Generation...");

            // 1. Ensure Sprite & Slash Assets and Enemy ScriptableObjects
            Sprite boxSprite = EnsureSquareSprite();
            Sprite slashSprite = EnsureSlashSprite();
            AssetSetupUtility.EnsureEnemyDataAssets();

            // 2. Create new Empty Scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 3. Resolve Layers
            int neutralLayer = GetOrCreateLayer("Neutral", 6);
            int primeLayer = GetOrCreateLayer("PrimeSolid", 7);
            int echoLayer = GetOrCreateLayer("EchoSolid", 8);
            int playerLayer = GetOrCreateLayer("Player", 9);
            int enemyLayer = GetOrCreateLayer("Enemy", 10);

            // 4. Setup Camera & Managers
            GameObject cameraObj = new GameObject("Main Camera");
            cameraObj.tag = "MainCamera";
            cameraObj.transform.position = new Vector3(0f, 2f, -10f);
            Camera cam = cameraObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6.5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f, 0.06f, 0.09f, 1f); // Dark Void
            cameraObj.AddComponent<AudioListener>();

            var cameraData = cameraObj.AddComponent<UniversalAdditionalCameraData>();
            cameraData.renderPostProcessing = true;

            // Add direct CameraFollow2D (100% reliable tracking)
            var camFollow = cameraObj.AddComponent<CameraFollow2D>();

            // Cinemachine Brain integration
            var brainType = System.Type.GetType("Unity.Cinemachine.CinemachineBrain, Unity.Cinemachine") 
                         ?? System.Type.GetType("Cinemachine.CinemachineBrain, Cinemachine");
            if (brainType != null)
            {
                cameraObj.AddComponent(brainType);
            }

            // 5. Setup 2D Global Light (URP)
            GameObject lightObj = new GameObject("Global 2D Light");
            var light2D = lightObj.AddComponent<Light2D>();
            light2D.lightType = Light2D.LightType.Global;
            light2D.color = Color.white;
            light2D.intensity = 1.0f;

            // 6. Setup Managers
            GameObject managersObj = new GameObject("Managers");
            managersObj.AddComponent<RealityManager>();
            managersObj.AddComponent<HitStopManager>();
            managersObj.AddComponent<CameraShakeManager>();
            managersObj.AddComponent<RealityUIIndicator>();

            // 7. Setup Player
            GameObject playerObj = new GameObject("Player");
            playerObj.tag = "Player";
            playerObj.layer = playerLayer;
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
            var combat = playerObj.AddComponent<PlayerCombat>();
            combat.SetSlashSprite(slashSprite);
            combat.SetEnemyLayer(1 << enemyLayer);

            var playerCtrl = playerObj.AddComponent<PlayerController>();
            playerCtrl.SetGroundLayer((1 << neutralLayer) | (1 << primeLayer) | (1 << echoLayer));

            // Wire camera target
            camFollow.SetTarget(playerObj.transform);

            // Cinemachine Camera follow setup
            var cmCamType = System.Type.GetType("Unity.Cinemachine.CinemachineCamera, Unity.Cinemachine") 
                         ?? System.Type.GetType("Cinemachine.CinemachineVirtualCamera, Cinemachine");
            if (cmCamType != null)
            {
                GameObject cmObj = new GameObject("CinemachineCamera");
                var vcam = cmObj.AddComponent(cmCamType);
                var followProp = cmCamType.GetProperty("Follow") ?? cmCamType.GetProperty("Target");
                if (followProp != null) followProp.SetValue(vcam, playerObj.transform);
            }

            // 8. Build Environment (Level 1-1 Sandbox)
            GameObject levelRoot = new GameObject("Environment");

            Color neutralColor = new Color(0.2f, 0.24f, 0.28f, 1f);
            Color primeColor = new Color(0.0f, 0.85f, 1.0f, 1f);  // Cyan
            Color echoColor = new Color(0.85f, 0.25f, 1.0f, 1f); // Purple

            // Main Arena Floor (Wide: 64 units)
            CreatePlatform(levelRoot.transform, "Floor_Main", new Vector3(8f, -2.5f, 0f), new Vector3(64f, 1.5f, 1f), neutralColor, boxSprite, neutralLayer);

            // Boundary Walls
            CreatePlatform(levelRoot.transform, "Wall_Left_Outer", new Vector3(-24f, 6f, 0f), new Vector3(1.5f, 16f, 1f), neutralColor, boxSprite, neutralLayer);
            CreatePlatform(levelRoot.transform, "Wall_Right_Outer", new Vector3(38f, 6f, 0f), new Vector3(1.5f, 16f, 1f), neutralColor, boxSprite, neutralLayer);

            // Wall Jump Shaft (Vertical Chute for Wall Slide & Jump practice)
            CreatePlatform(levelRoot.transform, "Wall_Shaft_Left", new Vector3(-18f, 4f, 0f), new Vector3(1.2f, 10f, 1f), neutralColor, boxSprite, neutralLayer);
            CreatePlatform(levelRoot.transform, "Wall_Shaft_Right", new Vector3(-14.5f, 4f, 0f), new Vector3(1.2f, 10f, 1f), neutralColor, boxSprite, neutralLayer);

            // Platforming Section: Alternating Reality Platforms
            CreateRealityPlatform(levelRoot.transform, "Platform_Prime_1", new Vector3(-9f, 0f, 0f), new Vector3(4.5f, 0.6f, 1f), RealmType.Prime, primeColor, boxSprite, primeLayer);
            CreateRealityPlatform(levelRoot.transform, "Platform_Echo_1", new Vector3(-3f, 1.8f, 0f), new Vector3(4.5f, 0.6f, 1f), RealmType.Echo, echoColor, boxSprite, echoLayer);
            CreateRealityPlatform(levelRoot.transform, "Platform_Prime_2", new Vector3(3f, 3.5f, 0f), new Vector3(4.5f, 0.6f, 1f), RealmType.Prime, primeColor, boxSprite, primeLayer);
            CreateRealityPlatform(levelRoot.transform, "Platform_Echo_HighLedge", new Vector3(8f, 5.2f, 0f), new Vector3(5.5f, 0.6f, 1f), RealmType.Echo, echoColor, boxSprite, echoLayer);

            // 9. Combat Arena Section (Entities & Dummies)
            GameObject combatRoot = new GameObject("Combat_Entities");

            // Load EnemyData ScriptableObjects
            var dummyPrimeSO = AssetDatabase.LoadAssetAtPath<EnemyDataSO>($"{ENEMY_DATA_DIR}/EnemyData_DummyPrime.asset");
            var dummyEchoSO = AssetDatabase.LoadAssetAtPath<EnemyDataSO>($"{ENEMY_DATA_DIR}/EnemyData_DummyEcho.asset");
            var crawlerSO = AssetDatabase.LoadAssetAtPath<EnemyDataSO>($"{ENEMY_DATA_DIR}/EnemyData_ChronoCrawler.asset");
            var weaverSO = AssetDatabase.LoadAssetAtPath<EnemyDataSO>($"{ENEMY_DATA_DIR}/EnemyData_VoidWeaver.asset");

            // Training Dummy Prime (Cyan)
            CreateDummy(combatRoot.transform, "Dummy_Prime", new Vector3(10f, -1.2f, 0f), RealmType.Prime, boxSprite, enemyLayer, dummyPrimeSO);

            // Training Dummy Echo (Purple)
            CreateDummy(combatRoot.transform, "Dummy_Echo", new Vector3(14f, -1.2f, 0f), RealmType.Echo, boxSprite, enemyLayer, dummyEchoSO);

            // ChronoCrawler Mob (Prime Crawler)
            CreateCrawler(combatRoot.transform, "Crawler_Prime", new Vector3(20f, -1.2f, 0f), boxSprite, enemyLayer, crawlerSO);

            // VoidWeaver Mob (Echo Flying Sniper)
            CreateVoidWeaver(combatRoot.transform, "Weaver_Echo", new Vector3(26f, 3.0f, 0f), boxSprite, enemyLayer, weaverSO);

            // 10. Save Scene
            if (!Directory.Exists(SCENE_DIR)) Directory.CreateDirectory(SCENE_DIR);
            EditorSceneManager.SaveScene(scene, SCENE_PATH);
            Debug.Log($"[SceneGenerator] Saved Prototype Scene to {SCENE_PATH}");

            // 11. Fix Build Settings: Prototype_Level1.unity MUST be at Index 0!
            ConfigureBuildSettings(SCENE_PATH);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[SceneGenerator] Scene generation and Build Settings configured successfully!");
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

        private static Sprite EnsureSlashSprite()
        {
            if (File.Exists(SLASH_SPRITE_PATH))
            {
                TextureImporter importer = AssetImporter.GetAtPath(SLASH_SPRITE_PATH) as TextureImporter;
                if (importer != null && importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.filterMode = FilterMode.Point;
                    importer.SaveAndReimport();
                }
                return AssetDatabase.LoadAssetAtPath<Sprite>(SLASH_SPRITE_PATH);
            }
            return EnsureSquareSprite();
        }

        private static int GetOrCreateLayer(string name, int fallbackIndex)
        {
            int layer = LayerMask.NameToLayer(name);
            return (layer != -1) ? layer : fallbackIndex;
        }

        private static void CreatePlatform(Transform parent, string name, Vector3 pos, Vector3 size, Color color, Sprite sprite, int layer)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent);
            obj.transform.position = pos;
            obj.transform.localScale = size;
            obj.layer = layer;

            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;

            var col = obj.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;
        }

        private static void CreateRealityPlatform(Transform parent, string name, Vector3 pos, Vector3 size, RealmType realm, Color color, Sprite sprite, int layer)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent);
            obj.transform.position = pos;
            obj.transform.localScale = size;
            obj.layer = layer;

            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;

            var col = obj.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;

            var realityPlat = obj.AddComponent<RealityPlatform>();
            realityPlat.Configure(realm, color);
        }

        private static void CreateDummy(Transform parent, string name, Vector3 pos, RealmType realm, Sprite sprite, int layer, EnemyDataSO data)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent);
            obj.transform.position = pos;
            obj.transform.localScale = new Vector3(1.2f, 2.0f, 1f);
            obj.layer = layer;

            var rb = obj.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.freezeRotation = true;
            rb.mass = 5f;

            var col = obj.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;

            var sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;

            var dummy = obj.AddComponent<TrainingDummy>();
            SetEnemyDataField(dummy, data, realm);
        }

        private static void CreateCrawler(Transform parent, string name, Vector3 pos, Sprite sprite, int layer, EnemyDataSO data)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent);
            obj.transform.position = pos;
            obj.transform.localScale = new Vector3(1.4f, 1.0f, 1f);
            obj.layer = layer;

            var rb = obj.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.freezeRotation = true;

            var col = obj.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;

            var sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;

            var crawler = obj.AddComponent<ChronoCrawler>();
            SetEnemyDataField(crawler, data, RealmType.Prime);
        }

        private static void CreateVoidWeaver(Transform parent, string name, Vector3 pos, Sprite sprite, int layer, EnemyDataSO data)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent);
            obj.transform.position = pos;
            obj.transform.localScale = new Vector3(1.2f, 1.2f, 1f);
            obj.layer = layer;

            var rb = obj.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            rb.freezeRotation = true;

            var col = obj.AddComponent<CircleCollider2D>();
            col.radius = 0.6f;

            var sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;

            var weaver = obj.AddComponent<VoidWeaver>();
            SetEnemyDataField(weaver, data, RealmType.Echo);
        }

        private static void SetEnemyDataField(EnemyBase enemy, EnemyDataSO data, RealmType realm)
        {
            var field = typeof(EnemyBase).GetField("enemyData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null && data != null)
            {
                field.SetValue(enemy, data);
            }
            var realmField = typeof(EnemyBase).GetField("customRealm", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (realmField != null)
            {
                realmField.SetValue(enemy, realm);
            }
        }

        private static void ConfigureBuildSettings(string mainScenePath)
        {
            // Set Prototype_Level1.unity as Scene 0 (Default Scene for Builds)
            var newScenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene(mainScenePath, true)
            };
            EditorBuildSettings.scenes = newScenes;
            Debug.Log($"[SceneGenerator] Build Settings updated: Scene 0 is {mainScenePath}");
        }
    }
}
