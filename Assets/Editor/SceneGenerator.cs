using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
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
        private const string PREFAB_DIR = "Assets/Prefabs";

        [MenuItem("Tools/Echo of the Void/Generate Prototype Scene")]
        public static void GeneratePrototypeScene()
        {
            // The scene is fully rebuilt: protect hand-made level edits from an accidental click
            if (!Application.isBatchMode && File.Exists(SCENE_PATH) &&
                !EditorUtility.DisplayDialog("Generate Prototype Scene",
                    "This REBUILDS Prototype_Level1.unity and discards any manual edits to it.\n\nPrefabs (Player, Enemies) are kept.",
                    "Rebuild scene", "Cancel"))
            {
                return;
            }

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

            // 5. Setup 2D Global Light (URP)
            GameObject lightObj = new GameObject("Global 2D Light");
            var light2D = lightObj.AddComponent<Light2D>();
            light2D.lightType = Light2D.LightType.Global;
            light2D.color = Color.white;
            light2D.intensity = 1.0f;

            // 6. Setup Managers & Audio
            GameObject managersObj = new GameObject("Managers");
            managersObj.AddComponent<RealityManager>();
            managersObj.AddComponent<HitStopManager>();
            managersObj.AddComponent<CameraShakeManager>();

            var audioMgr = managersObj.AddComponent<AudioManager>();
            AudioClip[] slashes = new AudioClip[]
            {
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SciFiSounds/Audio/laserSmall_000.ogg"),
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SciFiSounds/Audio/laserSmall_001.ogg"),
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SciFiSounds/Audio/laserSmall_002.ogg")
            };
            AudioClip hitClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SciFiSounds/Audio/impactMetal_001.ogg");
            AudioClip dashClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SciFiSounds/Audio/thrusterFire_000.ogg");
            AudioClip shiftClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SciFiSounds/Audio/forceField_000.ogg");
            AudioClip jumpClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SciFiSounds/Audio/spaceEngineSmall_001.ogg");
            AudioClip resClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SciFiSounds/Audio/laserLarge_000.ogg");
            audioMgr.ConfigureClips(slashes, hitClip, dashClip, shiftClip, jumpClip, resClip);
            audioMgr.ConfigureShiftDenied(AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/InterfaceSounds/Audio/error_001.ogg"));

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
            // GDD metrics: 14px x 26px (~0.875 x 1.625). Root keeps scale 1 so the collider is in real units.
            col.size = new Vector2(0.875f, 1.625f);

            // Sprite lives on a child so artists can swap it / add an Animator without touching physics
            AddVisual(playerObj, boxSprite, new Vector2(0.875f, 1.625f), new Color(0.95f, 0.95f, 0.95f, 1f));

            // Add Player Core Components
            playerObj.AddComponent<SquashAndStretch>();
            playerObj.AddComponent<GhostTrail>();
            playerObj.AddComponent<PlayerStats>();
            playerObj.AddComponent<PlayerRespawn>();
            playerObj.AddComponent<PlayerAnimationDriver>();
            playerObj.AddComponent<AbilitySet>();
            var combat = playerObj.AddComponent<PlayerCombat>();
            combat.SetSlashSprite(slashSprite);
            combat.SetEnemyLayer(1 << enemyLayer);

            // PlayerRespawn has [RequireComponent(PlayerController)], so the controller already exists
            var playerCtrl = playerObj.GetComponent<PlayerController>() ?? playerObj.AddComponent<PlayerController>();
            playerCtrl.SetGroundLayer((1 << neutralLayer) | (1 << primeLayer) | (1 << echoLayer));

            // Player becomes a prefab (reused on later runs so artist edits survive)
            playerObj = SaveOrReusePrefab(playerObj, "Player", "Player",
                typeof(PlayerAnimationDriver), typeof(AbilitySet));

            // Wire camera target directly
            camFollow.SetTarget(playerObj.transform);

            CreateRoomTemplate();

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

            // Wall Jump Shaft (Vertical Chute with open lower entrance)
            CreatePlatform(levelRoot.transform, "Wall_Shaft_Left", new Vector3(-18f, 4.5f, 0f), new Vector3(1.2f, 11f, 1f), neutralColor, boxSprite, neutralLayer);
            // Right wall starts at y=1.5, leaving 3.25m clearance entrance at bottom
            CreatePlatform(levelRoot.transform, "Wall_Shaft_Right", new Vector3(-15f, 5.75f, 0f), new Vector3(1.2f, 8.5f, 1f), neutralColor, boxSprite, neutralLayer);
            // Top Reward Ledge
            CreatePlatform(levelRoot.transform, "Shaft_Top_Ledge", new Vector3(-20.5f, 9.5f, 0f), new Vector3(4.5f, 0.8f, 1f), neutralColor, boxSprite, neutralLayer);

            // Platforming Section: Alternating Reality Platforms leading to Exit Rift
            CreateRealityPlatform(levelRoot.transform, "Platform_Prime_1", new Vector3(-9f, 0f, 0f), new Vector3(4.5f, 0.6f, 1f), RealmType.Prime, primeColor, boxSprite, primeLayer);
            CreateRealityPlatform(levelRoot.transform, "Platform_Echo_1", new Vector3(-3f, 1.8f, 0f), new Vector3(4.5f, 0.6f, 1f), RealmType.Echo, echoColor, boxSprite, echoLayer);
            CreateRealityPlatform(levelRoot.transform, "Platform_Prime_2", new Vector3(3f, 3.5f, 0f), new Vector3(4.5f, 0.6f, 1f), RealmType.Prime, primeColor, boxSprite, primeLayer);
            CreateRealityPlatform(levelRoot.transform, "Platform_Echo_HighLedge", new Vector3(8f, 5.2f, 0f), new Vector3(5.5f, 0.6f, 1f), RealmType.Echo, echoColor, boxSprite, echoLayer);
            CreateRealityPlatform(levelRoot.transform, "Platform_Prime_Final", new Vector3(14.5f, 6.8f, 0f), new Vector3(4.5f, 0.6f, 1f), RealmType.Prime, primeColor, boxSprite, primeLayer);
            CreatePlatform(levelRoot.transform, "Goal_Altar", new Vector3(21f, 8.0f, 0f), new Vector3(6.5f, 0.8f, 1f), neutralColor, boxSprite, neutralLayer);
            CreateLevelGoal(levelRoot.transform, "Level_Goal_Rift", new Vector3(21f, 9.6f, 0f), boxSprite);

            // Chrono Stations (checkpoint + save): start, before the combat arena, and the shaft reward ledge
            CreateStation(levelRoot.transform, "Station_Start", new Vector3(-7.5f, -1.75f, 0f), boxSprite, "station_start");
            CreateStation(levelRoot.transform, "Station_Arena", new Vector3(6.5f, -1.75f, 0f), boxSprite, "station_arena");
            CreateStation(levelRoot.transform, "Station_ShaftTop", new Vector3(-20.5f, 9.9f, 0f), boxSprite, "station_shaft_top");

            // Key items (spec 4): Wall Jump is locked until the Piston Boots; Resonance Strike until its core
            CreatePickup(levelRoot.transform, "Pickup_PistonBoots", new Vector3(-10.5f, -0.9f, 0f), boxSprite,
                AbilityFlags.WallJump, "PISTON BOOTS", new Color(1f, 0.8f, 0.2f, 1f));
            CreatePickup(levelRoot.transform, "Pickup_ResonanceCore", new Vector3(8.5f, -0.9f, 0f), boxSprite,
                AbilityFlags.ResonanceStrike, "RESONANCE CORE", new Color(0.95f, 0.4f, 0.9f, 1f));

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

            // 10. Setup Player HUD (Canvas UI)
            CreateHUD(boxSprite);

            // 11. Save Scene
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
            obj.layer = layer;

            var rb = obj.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.freezeRotation = true;
            rb.mass = 5f;

            var col = obj.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.2f, 2.0f);
            AddVisual(obj, sprite, new Vector2(1.2f, 2.0f), Color.white);

            var dummy = obj.AddComponent<TrainingDummy>();
            SetEnemyDataField(dummy, data, realm);
            SaveOrReusePrefab(obj, "Enemy_" + name, "Enemies");
        }

        private static void CreateCrawler(Transform parent, string name, Vector3 pos, Sprite sprite, int layer, EnemyDataSO data)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent);
            obj.transform.position = pos;
            obj.layer = layer;

            var rb = obj.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.freezeRotation = true;

            var col = obj.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.4f, 1.0f);
            AddVisual(obj, sprite, new Vector2(1.4f, 1.0f), Color.white);

            var crawler = obj.AddComponent<ChronoCrawler>();
            SetEnemyDataField(crawler, data, RealmType.Prime);
            SaveOrReusePrefab(obj, "Enemy_" + name, "Enemies");
        }

        private static void CreateVoidWeaver(Transform parent, string name, Vector3 pos, Sprite sprite, int layer, EnemyDataSO data)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent);
            obj.transform.position = pos;
            obj.layer = layer;

            var rb = obj.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            rb.freezeRotation = true;

            var col = obj.AddComponent<CircleCollider2D>();
            col.radius = 0.6f;
            AddVisual(obj, sprite, new Vector2(1.2f, 1.2f), Color.white);

            var weaver = obj.AddComponent<VoidWeaver>();
            SetEnemyDataField(weaver, data, RealmType.Echo);
            SaveOrReusePrefab(obj, "Enemy_" + name, "Enemies");
        }

        /// <summary>
        /// Prefab with a Grid and three ready-made tilemaps (Neutral / Prime / Echo) so artists only paint tiles.
        /// Never overwrites an existing template.
        /// </summary>
        [MenuItem("Tools/Echo of the Void/Create Room Template Prefab")]
        public static void CreateRoomTemplate()
        {
            const string path = PREFAB_DIR + "/Levels/Room_Template.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;
            EnsureFolder(PREFAB_DIR + "/Levels");

            var grid = new GameObject("Room_Template");
            grid.AddComponent<Grid>().cellSize = Vector3.one; // 1 tile = 1 unit = 16 px

            BuildTilemapLayer(grid.transform, "Tilemap_Neutral", "Neutral", 6, null, 0);
            BuildTilemapLayer(grid.transform, "Tilemap_Prime", "PrimeSolid", 7, RealmType.Prime, 1);
            BuildTilemapLayer(grid.transform, "Tilemap_Echo", "EchoSolid", 8, RealmType.Echo, 2);

            PrefabUtility.SaveAsPrefabAsset(grid, path);
            Object.DestroyImmediate(grid);
            AssetDatabase.SaveAssets();
            Debug.Log("[SceneGenerator] Created " + path);
        }

        private static void BuildTilemapLayer(Transform parent, string name, string layerName, int fallbackLayer,
            RealmType? realm, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.layer = GetOrCreateLayer(layerName, fallbackLayer);

            go.AddComponent<Tilemap>();
            go.AddComponent<TilemapRenderer>().sortingOrder = sortingOrder;

            var body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Static;

            var tilemapCollider = go.AddComponent<TilemapCollider2D>();
            tilemapCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
            go.AddComponent<CompositeCollider2D>();

            if (realm.HasValue)
            {
                go.AddComponent<RealityTilemap>().Configure(realm.Value, Color.white);
            }
        }

        /// <summary>Adds a child "Visual" carrying the sprite, sized in world units (root keeps scale 1).</summary>
        private static SpriteRenderer AddVisual(GameObject root, Sprite sprite, Vector2 size, Color color)
        {
            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            visual.transform.localScale = new Vector3(size.x, size.y, 1f);
            visual.layer = root.layer;

            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            return sr;
        }

        /// <summary>
        /// First run: saves <paramref name="built"/> as a prefab and connects the scene object to it.
        /// Later runs: reuses the existing prefab so hand edits (sprites, animators) are kept.
        /// Returns the scene instance to use from now on.
        /// </summary>
        private static GameObject SaveOrReusePrefab(GameObject built, string prefabName, string subFolder,
            params System.Type[] requiredComponents)
        {
            string dir = $"{PREFAB_DIR}/{subFolder}";
            EnsureFolder(dir);
            string path = $"{dir}/{prefabName}.prefab";

            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing == null)
            {
                return PrefabUtility.SaveAsPrefabAssetAndConnect(built, path, InteractionMode.AutomatedAction);
            }

            Transform parent = built.transform.parent;
            Vector3 position = built.transform.position;
            string instanceName = built.name;
            Object.DestroyImmediate(built);

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(existing);
            if (parent != null) instance.transform.SetParent(parent, false);
            instance.transform.position = position;
            instance.name = instanceName;

            // Older prefabs may predate a code component: add what is missing and push it into the prefab asset
            bool added = false;
            foreach (var type in requiredComponents)
            {
                if (instance.GetComponent(type) == null)
                {
                    instance.AddComponent(type);
                    added = true;
                }
            }
            if (added)
            {
                PrefabUtility.ApplyPrefabInstance(instance, InteractionMode.AutomatedAction);
                Debug.Log($"[SceneGenerator] Added missing components to prefab {prefabName}");
            }
            return instance;
        }

        private static void EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath)) return;
            string parent = Path.GetDirectoryName(assetPath).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(assetPath));
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

        private static void CreateHUD(Sprite boxSprite)
        {
            GameObject canvasObj = new GameObject("Canvas_HUD");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasObj.AddComponent<GraphicRaycaster>();

            Font fontOrbitron = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Orbitron-Variable.ttf")
                             ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Font fontSpaceMono = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/SpaceMono-Regular.ttf")
                              ?? fontOrbitron;

            // Panel Root
            GameObject panel = new GameObject("HUD_Panel");
            panel.transform.SetParent(canvasObj.transform, false);
            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.anchoredPosition = new Vector2(30f, -30f);
            panelRect.sizeDelta = new Vector2(350f, 120f);

            // Realm Indicator Text (Orbitron - English)
            GameObject realmTextObj = new GameObject("Text_Realm");
            realmTextObj.transform.SetParent(panel.transform, false);
            RectTransform realmRect = realmTextObj.AddComponent<RectTransform>();
            realmRect.anchorMin = new Vector2(0f, 1f);
            realmRect.anchorMax = new Vector2(0f, 1f);
            realmRect.pivot = new Vector2(0f, 1f);
            realmRect.anchoredPosition = new Vector2(0f, 0f);
            realmRect.sizeDelta = new Vector2(300f, 30f);
            Text realmText = realmTextObj.AddComponent<Text>();
            realmText.font = fontOrbitron;
            realmText.fontSize = 20;
            realmText.fontStyle = FontStyle.Bold;
            realmText.alignment = TextAnchor.MiddleLeft;
            realmText.text = "REALM: PRIME";
            realmText.color = new Color(0f, 0.85f, 1f, 1f);

            // Health Bar Background
            GameObject hpBgObj = new GameObject("HP_Background");
            hpBgObj.transform.SetParent(panel.transform, false);
            RectTransform hpBgRect = hpBgObj.AddComponent<RectTransform>();
            hpBgRect.anchorMin = new Vector2(0f, 1f);
            hpBgRect.anchorMax = new Vector2(0f, 1f);
            hpBgRect.pivot = new Vector2(0f, 1f);
            hpBgRect.anchoredPosition = new Vector2(0f, -32f);
            hpBgRect.sizeDelta = new Vector2(240f, 18f);
            Image hpBg = hpBgObj.AddComponent<Image>();
            hpBg.sprite = boxSprite;
            hpBg.color = new Color(0.1f, 0.1f, 0.12f, 0.85f);

            // Health Bar Fill
            GameObject hpFillObj = new GameObject("HP_Fill");
            hpFillObj.transform.SetParent(hpBgObj.transform, false);
            RectTransform hpFillRect = hpFillObj.AddComponent<RectTransform>();
            hpFillRect.anchorMin = Vector2.zero;
            hpFillRect.anchorMax = Vector2.one;
            hpFillRect.sizeDelta = Vector2.zero;
            Image hpFill = hpFillObj.AddComponent<Image>();
            hpFill.sprite = boxSprite;
            hpFill.type = Image.Type.Filled;
            hpFill.fillMethod = Image.FillMethod.Horizontal;
            hpFill.color = new Color(0.18f, 0.8f, 0.44f, 1f); // Emerald Green

            // Energy Bar Background
            GameObject ceBgObj = new GameObject("CE_Background");
            ceBgObj.transform.SetParent(panel.transform, false);
            RectTransform ceBgRect = ceBgObj.AddComponent<RectTransform>();
            ceBgRect.anchorMin = new Vector2(0f, 1f);
            ceBgRect.anchorMax = new Vector2(0f, 1f);
            ceBgRect.pivot = new Vector2(0f, 1f);
            ceBgRect.anchoredPosition = new Vector2(0f, -54f);
            ceBgRect.sizeDelta = new Vector2(240f, 14f);
            Image ceBg = ceBgObj.AddComponent<Image>();
            ceBg.sprite = boxSprite;
            ceBg.color = new Color(0.1f, 0.1f, 0.12f, 0.85f);

            // Energy Bar Fill
            GameObject ceFillObj = new GameObject("CE_Fill");
            ceFillObj.transform.SetParent(ceBgObj.transform, false);
            RectTransform ceFillRect = ceFillObj.AddComponent<RectTransform>();
            ceFillRect.anchorMin = Vector2.zero;
            ceFillRect.anchorMax = Vector2.one;
            ceFillRect.sizeDelta = Vector2.zero;
            Image ceFill = ceFillObj.AddComponent<Image>();
            ceFill.sprite = boxSprite;
            ceFill.type = Image.Type.Filled;
            ceFill.fillMethod = Image.FillMethod.Horizontal;
            ceFill.color = new Color(0.95f, 0.77f, 0.06f, 1f); // Amber Gold

            // Dash Indicator
            GameObject dashObj = new GameObject("Dash_Indicator");
            dashObj.transform.SetParent(panel.transform, false);
            RectTransform dashRect = dashObj.AddComponent<RectTransform>();
            dashRect.anchorMin = new Vector2(0f, 1f);
            dashRect.anchorMax = new Vector2(0f, 1f);
            dashRect.pivot = new Vector2(0f, 1f);
            dashRect.anchoredPosition = new Vector2(252f, -32f);
            dashRect.sizeDelta = new Vector2(36f, 36f);
            Image dashBg = dashObj.AddComponent<Image>();
            dashBg.sprite = boxSprite;
            dashBg.color = new Color(0.2f, 0.25f, 0.32f, 0.9f);

            GameObject dashFillObj = new GameObject("Dash_CooldownOverlay");
            dashFillObj.transform.SetParent(dashObj.transform, false);
            RectTransform dashFillRect = dashFillObj.AddComponent<RectTransform>();
            dashFillRect.anchorMin = Vector2.zero;
            dashFillRect.anchorMax = Vector2.one;
            dashFillRect.sizeDelta = Vector2.zero;
            Image dashFill = dashFillObj.AddComponent<Image>();
            dashFill.sprite = boxSprite;
            dashFill.type = Image.Type.Filled;
            dashFill.fillMethod = Image.FillMethod.Radial360;
            dashFill.color = new Color(0f, 0f, 0f, 0.75f);

            // Announcement Banner (Center Screen)
            GameObject annObj = new GameObject("Text_Announcement");
            annObj.transform.SetParent(canvasObj.transform, false);
            RectTransform annRect = annObj.AddComponent<RectTransform>();
            annRect.anchorMin = new Vector2(0.5f, 0.5f);
            annRect.anchorMax = new Vector2(0.5f, 0.5f);
            annRect.pivot = new Vector2(0.5f, 0.5f);
            annRect.anchoredPosition = new Vector2(0f, 80f);
            annRect.sizeDelta = new Vector2(900f, 100f);
            Text annText = annObj.AddComponent<Text>();
            annText.font = fontOrbitron;
            annText.fontSize = 28;
            annText.fontStyle = FontStyle.Bold;
            annText.alignment = TextAnchor.MiddleCenter;
            annText.color = new Color(1f, 0.85f, 0.2f, 1f);
            annObj.SetActive(false);

            // Controls Guide (Bottom Center - SpaceMono font per D11)
            GameObject guideObj = new GameObject("Controls_Guide");
            guideObj.transform.SetParent(canvasObj.transform, false);
            RectTransform guideRect = guideObj.AddComponent<RectTransform>();
            guideRect.anchorMin = new Vector2(0.5f, 0f);
            guideRect.anchorMax = new Vector2(0.5f, 0f);
            guideRect.pivot = new Vector2(0.5f, 0f);
            guideRect.anchoredPosition = new Vector2(0f, 25f);
            guideRect.sizeDelta = new Vector2(1100f, 35f);
            Text guideText = guideObj.AddComponent<Text>();
            guideText.font = fontSpaceMono;
            guideText.fontSize = 14;
            guideText.alignment = TextAnchor.MiddleCenter;
            guideText.color = new Color(0.85f, 0.9f, 0.95f, 0.85f);
            guideText.text = "[A / D] Move  |  [SPACE] Jump  |  [K] Dash  |  [J] Attack  |  [SHIFT] Reality Shift  |  [U] Resonance";

            // Attach PlayerHUD component
            var hud = canvasObj.AddComponent<PlayerHUD>();
            hud.BindElements(hpFill, ceFill, dashFill, realmText, null, annText);
        }

        private static void CreatePickup(Transform parent, string name, Vector3 pos, Sprite sprite,
            AbilityFlags ability, string displayName, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = pos;
            go.layer = GetOrCreateLayer("Interactable", 12);

            var trigger = go.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = 0.7f;

            AddVisual(go, sprite, new Vector2(0.7f, 0.7f), color);
            go.AddComponent<AbilityPickup>().Configure(ability, displayName);
            SaveOrReusePrefab(go, "Pickup_" + ability, "Environment");
        }

        private static void CreateStation(Transform parent, string name, Vector3 floorPoint, Sprite sprite, string id)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = floorPoint;
            go.layer = GetOrCreateLayer("Interactable", 12);

            var trigger = go.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(3f, 2.5f);
            trigger.offset = new Vector2(0f, 1.25f);

            var visual = AddVisual(go, sprite, new Vector2(0.8f, 2f), new Color(0.3f, 0.9f, 0.8f, 0.6f));
            visual.transform.localPosition = new Vector3(0f, 1f, 0f);

            go.AddComponent<ChronoStation>().Configure(id, 25);
            SaveOrReusePrefab(go, "Station_Chrono_" + id, "Environment");
        }

        private static void CreateLevelGoal(Transform parent, string name, Vector3 pos, Sprite sprite)
        {
            GameObject goalObj = new GameObject(name);
            goalObj.transform.SetParent(parent);
            goalObj.transform.position = pos;
            goalObj.transform.localScale = new Vector3(1.6f, 2.8f, 1f);

            var sr = goalObj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = new Color(1f, 0.84f, 0.0f, 0.85f); // Golden Exit Rift
            sr.sortingOrder = 5;

            var col = goalObj.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = Vector2.one;

            goalObj.AddComponent<LevelGoalTrigger>();
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
