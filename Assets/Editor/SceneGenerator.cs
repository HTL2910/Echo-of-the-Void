using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using EchoOfTheVoid.Core;
using EchoOfTheVoid.Combat;
using EchoOfTheVoid.Environment;
using EchoOfTheVoid.Feedback;
using EchoOfTheVoid.Player;
using EchoOfTheVoid.Enemies;
using EchoOfTheVoid.Bosses;
using EchoOfTheVoid.Environment.Mechanics;
using EchoOfTheVoid.UI;

namespace EchoOfTheVoid.Editor
{
    public static class SceneGenerator
    {
        private const string SPRITE_PATH = "Assets/Sprites/square.png";
        private const string SLASH_SPRITE_PATH = "Assets/Sprites/SlashArc.png";
        private const string SCENE_DIR = "Assets/Scenes";
        private const string SCENE_PATH = "Assets/Scenes/Prototype_Level1.unity";
        private const string MENU_SCENE_PATH = "Assets/Scenes/MainMenu.unity";
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

            BuildPrototypeSceneInternal();
        }

        [MenuItem("Tools/Echo of the Void/Force Rebuild Scene (No Prompt) %#r")]
        public static void ForceRebuildScene()
        {
            BuildPrototypeSceneInternal();
        }

        private static void BuildPrototypeSceneInternal()
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

            // 5. Setup Post-Processing Global Volume & 2D Lighting (URP)
            GameObject volumeObj = new GameObject("Global Volume");
            var globalVolume = volumeObj.AddComponent<Volume>();
            globalVolume.isGlobal = true;
            globalVolume.profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/DefaultVolumeProfile.asset");

            GameObject lightObj = new GameObject("Global 2D Light");
            var light2D = lightObj.AddComponent<Light2D>();
            light2D.lightType = Light2D.LightType.Global;
            light2D.color = new Color(0.24f, 0.30f, 0.44f, 1f); // Deep atmospheric moody void blue
            light2D.intensity = 0.55f;

            // 6. Setup Managers & Audio
            GameObject managersObj = new GameObject("Managers");
            managersObj.AddComponent<RealityManager>();
            managersObj.AddComponent<HitStopManager>();
            managersObj.AddComponent<CameraShakeManager>();
            managersObj.AddComponent<RoomManager>();
            managersObj.AddComponent<BossHealthBar>().SetFont(AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Orbitron-Variable.ttf"));
            managersObj.AddComponent<PauseController>().SetFont(AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/SpaceMono-Regular.ttf"));
            managersObj.AddComponent<EchoOfTheVoid.Save.SaveBootstrap>(); // applies Continue on scene start

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

            // Adaptive music: silent until the two stems exist (Assets/Audio/Music)
            var music = managersObj.AddComponent<MusicLayerController>();
            music.Configure(AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Music/MUS_Z1_Prime.ogg"),
                            AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Music/MUS_Z1_Echo.ogg"));

            // One-shot particle effects (Assets/Prefabs/VFX); missing prefabs are simply skipped
            var vfx = managersObj.AddComponent<VfxLibrary>();
            foreach (VfxId id in System.Enum.GetValues(typeof(VfxId)))
            {
                vfx.Configure(id, AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/VFX/{VfxPrefabName(id)}.prefab"));
            }
            audioMgr.ConfigureShiftDenied(AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/InterfaceSounds/Audio/error_001.ogg"));

            // Artist-recorded variants (Assets/Audio/SFX/Custom): footsteps, landings, hurt, death, station...
            foreach (var entry in new[]
            {
                (SfxGroup.FootstepPrime, "SFX_Footstep_Prime_"), (SfxGroup.FootstepEcho, "SFX_Footstep_Echo_"),
                (SfxGroup.LandSoft, "SFX_Land_Soft_"), (SfxGroup.LandHard, "SFX_Land_Hard_"),
                (SfxGroup.Hurt, "SFX_Hurt_"), (SfxGroup.Death, "SFX_Death_"),
                (SfxGroup.StationActivate, "SFX_Station_Activate_"),
                (SfxGroup.AnchorPlace, "SFX_Anchor_Place_"), (SfxGroup.AnchorSwap, "SFX_Anchor_Swap_")
            })
            {
                audioMgr.ConfigureGroup(entry.Item1, LoadVariants(entry.Item2));
            }
            managersObj.AddComponent<LowHealthAudio>().Configure(
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/Custom/SFX_Heartbeat_Loop_01.ogg"));
            var postProcessCtrl = managersObj.AddComponent<RealityPostProcessController>();

            // 7. Setup Player
            GameObject playerObj = new GameObject("Player");
            playerObj.tag = "Player";
            playerObj.layer = playerLayer;
            playerObj.transform.position = new Vector3(-25f, 1.5f, 0f);

            var rb = playerObj.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.freezeRotation = true;
            rb.gravityScale = 0f;

            var col = playerObj.AddComponent<BoxCollider2D>();
            // GDD metrics: 14px x 26px (~0.875 x 1.625). Root keeps scale 1 so the collider is in real units.
            col.size = new Vector2(0.875f, 1.625f);

            // Sprite and Animator live on Visual child
            Sprite kaelSprite = LoadSprite("Assets/Art/Sprites/Kael/spr_kael_idle_0.png") ?? boxSprite;
            var visual = new GameObject("Visual");
            visual.transform.SetParent(playerObj.transform, false);
            visual.transform.localScale = Vector3.one;
            visual.layer = playerLayer;

            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = kaelSprite;
            sr.color = Color.white;

            var animController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Art/Animations/Kael/Kael.controller");
            if (animController != null)
            {
                var animator = visual.AddComponent<Animator>();
                animator.runtimeAnimatorController = animController;
            }

            // Add Player Core Components
            playerObj.AddComponent<SquashAndStretch>();
            playerObj.AddComponent<GhostTrail>();
            playerObj.AddComponent<PlayerStats>();
            playerObj.AddComponent<PlayerRespawn>();
            playerObj.AddComponent<PlayerAnimationDriver>();
            playerObj.AddComponent<AbilitySet>();
            playerObj.AddComponent<EchoAnchor>();
            var combat = playerObj.AddComponent<PlayerCombat>();
            combat.SetSlashSprite(slashSprite);
            combat.SetEnemyLayer(1 << enemyLayer);

            // PlayerRespawn has [RequireComponent(PlayerController)], so the controller already exists
            var playerCtrl = playerObj.GetComponent<PlayerController>() ?? playerObj.AddComponent<PlayerController>();
            playerCtrl.SetGroundLayer((1 << neutralLayer) | (1 << primeLayer) | (1 << echoLayer));

            // Add Player Aura Point Light (Cyan/Emerald Chrono glow in Prime, Amethyst in Echo)
            var auraObj = new GameObject("KaelAuraLight");
            auraObj.transform.SetParent(playerObj.transform, false);
            auraObj.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            var playerAuraLight = auraObj.AddComponent<Light2D>();
            playerAuraLight.lightType = Light2D.LightType.Point;
            playerAuraLight.color = new Color(0.15f, 0.95f, 0.85f, 1f);
            playerAuraLight.pointLightInnerRadius = 0.8f;
            playerAuraLight.pointLightOuterRadius = 6.0f;
            playerAuraLight.intensity = 1.3f;
            playerAuraLight.falloffIntensity = 0.65f;

            postProcessCtrl.Configure(globalVolume, light2D, playerAuraLight);

            // Player becomes a prefab (reused on later runs so artist edits survive)
            playerObj = SaveOrReusePrefab(playerObj, "Player", "Player",
                typeof(PlayerAnimationDriver), typeof(AbilitySet), typeof(EchoAnchor));

            // Wire camera target directly
            camFollow.SetTarget(playerObj.transform);

            CreateRoomTemplate();

            // 8. Build Environment (Level 1-1 Sandbox)
            GameObject levelRoot = new GameObject("Environment");
            CreateAtmosphericBackground(levelRoot.transform, boxSprite);

            // Load Environment Sprites
            Sprite sprNeutralPlat = LoadSprite("Assets/Art/Sprites/Environment/spr_platform_neutral.png") ?? boxSprite;
            Sprite sprPrimePlat = LoadSprite("Assets/Art/Sprites/Environment/spr_platform_prime.png") ?? boxSprite;
            Sprite sprEchoPlat = LoadSprite("Assets/Art/Sprites/Environment/spr_platform_echo.png") ?? boxSprite;
            Sprite sprStation = LoadSprite("Assets/Art/Sprites/Environment/spr_chrono_station.png") ?? boxSprite;
            Sprite sprGoalRift = LoadSprite("Assets/Art/Sprites/Environment/spr_level_goal_rift.png") ?? boxSprite;

            Sprite iconWallJump = LoadSprite("Assets/Art/UI/Icons/icon_ability_walljump.png") ?? boxSprite;
            Sprite iconResonance = LoadSprite("Assets/Art/UI/Icons/icon_ability_resonance.png") ?? boxSprite;

            // Dark Fantasy Enemy Sprites (Prime Realm idle as default SpriteRenderer)
            const string DF = "Assets/Art/Sprites/Enemies/DarkFantasy";
            Sprite sprCrawlerPrime  = LoadSprite($"{DF}/chrono_crawler_prime_walk.png")   ?? boxSprite;
            Sprite sprCrawlerEcho   = LoadSprite($"{DF}/chrono_crawler_echo_walk.png")    ?? boxSprite;
            Sprite sprWeaverPrime   = LoadSprite($"{DF}/void_weaver_prime_idle.png")      ?? boxSprite;
            Sprite sprWeaverEcho    = LoadSprite($"{DF}/void_weaver_echo_idle.png")       ?? boxSprite;
            Sprite sprKnightPrime   = LoadSprite($"{DF}/rift_knight_prime_idle.png")      ?? boxSprite;
            Sprite sprKnightEcho    = LoadSprite($"{DF}/rift_knight_echo_idle.png")       ?? boxSprite;

            Color neutralColor = Color.white;
            Color primeColor = new Color(0.0f, 0.85f, 1.0f, 1f);  // Cyan
            Color echoColor = new Color(0.85f, 0.25f, 1.0f, 1f); // Purple

            // 9. Combat Arena Section (Entities & Dummies)
            GameObject combatRoot = new GameObject("Combat_Entities");

            // Load EnemyData ScriptableObjects
            var dummyPrimeSO = AssetDatabase.LoadAssetAtPath<EnemyDataSO>($"{ENEMY_DATA_DIR}/EnemyData_DummyPrime.asset");
            var dummyEchoSO = AssetDatabase.LoadAssetAtPath<EnemyDataSO>($"{ENEMY_DATA_DIR}/EnemyData_DummyEcho.asset");
            var crawlerSO = AssetDatabase.LoadAssetAtPath<EnemyDataSO>($"{ENEMY_DATA_DIR}/EnemyData_ChronoCrawler.asset");
            var weaverSO = AssetDatabase.LoadAssetAtPath<EnemyDataSO>($"{ENEMY_DATA_DIR}/EnemyData_VoidWeaver.asset");

            Codex.EnemyPrefabBuilder.CreateAllEnemyPrefabs();
            Codex.BossPrefabBuilder.CreateAllBossPrefabs();
            AssetDatabase.Refresh();

            int hazardLayer = GetOrCreateLayer("Hazard", 11);
            int interactableLayer = GetOrCreateLayer("Interactable", 12);

            // Prefab templates for mechanics (doors, plates, levers)
            var mechTemplates = new GameObject("Mechanic_Templates").transform;
            mechTemplates.SetParent(levelRoot.transform, false);
            BuildMechanic<PressurePlate>(mechTemplates, "PressurePlate", Vector3.zero, new Vector2(1.5f, 0.2f), new Color(1f, 0.85f, 0.2f, 1f), boxSprite, neutralLayer, true, false);
            BuildMechanic<Door>(mechTemplates, "Door", Vector3.zero, new Vector2(0.8f, 4f), new Color(0.5f, 0.55f, 0.65f, 1f), boxSprite, neutralLayer, false, false);
            BuildMechanic<Lever>(mechTemplates, "Lever", Vector3.zero, new Vector2(0.8f, 1f), new Color(0.9f, 0.9f, 0.4f, 1f), boxSprite, interactableLayer, true, false);
            Object.DestroyImmediate(mechTemplates.gameObject);

            // Build Zone 1 Continuous Metroidvania Layout: Rooms 1 to 10 + Boss Arena
            CreateZone1Rooms1To10(
                levelRoot.transform,
                combatRoot.transform,
                sprNeutralPlat, sprPrimePlat, sprEchoPlat, sprStation, sprGoalRift,
                iconWallJump, iconResonance, boxSprite,
                sprCrawlerPrime, sprCrawlerEcho, sprWeaverPrime, sprWeaverEcho, sprKnightPrime, sprKnightEcho,
                neutralColor, primeColor, echoColor,
                neutralLayer, primeLayer, echoLayer, enemyLayer, hazardLayer, interactableLayer,
                dummyPrimeSO, dummyEchoSO, crawlerSO, weaverSO
            );

            // 10. Setup Player HUD (Canvas UI)
            CreateHUD();

            // 11. Save Scene
            if (!Directory.Exists(SCENE_DIR)) Directory.CreateDirectory(SCENE_DIR);
            EditorSceneManager.SaveScene(scene, SCENE_PATH);
            Debug.Log($"[SceneGenerator] Saved Prototype Scene to {SCENE_PATH}");

            // 11. Fix Build Settings: Prototype_Level1.unity MUST be at Index 0!
            GenerateMainMenuScene();
            ConfigureBuildSettings(MENU_SCENE_PATH, SCENE_PATH);

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

        private static Sprite LoadSprite(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null) return sprite;
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            if (assets != null)
            {
                foreach (var asset in assets)
                {
                    if (asset is Sprite s) return s;
                }
            }
            return null;
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
            obj.layer = layer;

            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;

            if (sprite != null && sprite.border != Vector4.zero)
            {
                renderer.drawMode = SpriteDrawMode.Sliced;
                renderer.size = new Vector2(size.x, size.y);
                obj.transform.localScale = Vector3.one;

                var col = obj.AddComponent<BoxCollider2D>();
                col.size = new Vector2(size.x, size.y);
            }
            else
            {
                obj.transform.localScale = size;
                var col = obj.AddComponent<BoxCollider2D>();
                col.size = Vector2.one;
            }
        }

        private static void CreateRealityPlatform(Transform parent, string name, Vector3 pos, Vector3 size, RealmType realm, Color color, Sprite sprite, int layer)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent);
            obj.transform.position = pos;
            obj.layer = layer;

            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;

            bool isCustomTexture = (sprite != null && sprite.border != Vector4.zero);
            Color renderColor = isCustomTexture ? Color.white : color;
            renderer.color = renderColor;

            if (isCustomTexture)
            {
                renderer.drawMode = SpriteDrawMode.Sliced;
                renderer.size = new Vector2(size.x, size.y);
                obj.transform.localScale = Vector3.one;

                var col = obj.AddComponent<BoxCollider2D>();
                col.size = new Vector2(size.x, size.y);
            }
            else
            {
                obj.transform.localScale = size;
                var col = obj.AddComponent<BoxCollider2D>();
                col.size = Vector2.one;
            }

            var realityPlat = obj.AddComponent<RealityPlatform>();
            realityPlat.Configure(realm, renderColor);
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
            obj.AddComponent<EnemyRespawner>();
            SaveOrReusePrefab(obj, "Enemy_" + name, "Enemies", typeof(EnemyRespawner));
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
            obj.AddComponent<EnemyRespawner>();
            SaveOrReusePrefab(obj, "Enemy_" + name, "Enemies", typeof(EnemyRespawner));
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
            obj.AddComponent<EnemyRespawner>();
            SaveOrReusePrefab(obj, "Enemy_" + name, "Enemies", typeof(EnemyRespawner));
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

        private static AudioClip[] LoadVariants(string prefix)
        {
            var clips = new System.Collections.Generic.List<AudioClip>();
            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio/SFX/Custom" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!Path.GetFileName(path).StartsWith(prefix)) continue;
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip != null) clips.Add(clip);
            }
            clips.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            return clips.ToArray();
        }

        /// <summary>Instantiates a prefab (keeps the link) under a parent. Null if the prefab does not exist.</summary>
        private static GameObject PlacePrefab(string path, Transform parent, string name, Vector3 position)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogWarning($"[SceneGenerator] Missing prefab {path}");
                return null;
            }
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.SetParent(parent, false);
            instance.transform.position = position;
            instance.name = name;
            return instance;
        }

        /// <summary>Builds a one-collider mechanic and saves it as a prefab; optionally places one instance in the level.</summary>
        private static GameObject BuildMechanic<T>(Transform parent, string name, Vector3 position, Vector2 size, Color color,
            Sprite sprite, int layer, bool trigger, bool place) where T : Component
        {
            var go = new GameObject(name);
            go.layer = layer;
            go.transform.position = position;
            var col = go.AddComponent<BoxCollider2D>();
            col.size = size;
            col.isTrigger = trigger;
            AddVisual(go, sprite, size, color);
            go.AddComponent<T>();

            var instance = SaveOrReusePrefab(go, "Mech_" + typeof(T).Name, "Mechanics");
            if (!place)
            {
                Object.DestroyImmediate(instance);
                return null;
            }
            instance.transform.SetParent(parent, true);
            instance.transform.position = position;
            instance.name = name;
            return instance;
        }

        /// <summary>Makes sure a pickup prefab for <paramref name="ability"/> exists and returns the asset (no scene instance).</summary>
        private static GameObject EnsurePickupPrefab(Sprite sprite, AbilityFlags ability, string displayName, Color color)
        {
            var go = new GameObject("Pickup_" + ability);
            go.layer = GetOrCreateLayer("Interactable", 12);
            var trigger = go.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = 0.7f;
            AddVisual(go, sprite, new Vector2(0.7f, 0.7f), color);
            go.AddComponent<PersistentId>().SetId("pickup_" + ability.ToString().ToLowerInvariant());
            go.AddComponent<AbilityPickup>().Configure(ability, displayName);

            var instance = SaveOrReusePrefab(go, "Pickup_" + ability, "Environment");
            Object.DestroyImmediate(instance);
            return AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_DIR}/Environment/Pickup_{ability}.prefab");
        }

        private static GameObject EnsureMissileWarningPrefab(Sprite sprite)
        {
            var go = new GameObject("Fx_MissileWarning");
            AddVisual(go, sprite, new Vector2(2f, 2f), new Color(1f, 0.2f, 0.2f, 0.4f));
            go.GetComponentInChildren<SpriteRenderer>().sortingOrder = 5;
            go.AddComponent<TimedDestroy>().SetLifetime(1.4f);

            var instance = SaveOrReusePrefab(go, "Fx_MissileWarning", "Bosses");
            Object.DestroyImmediate(instance);
            return AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_DIR}/Bosses/Fx_MissileWarning.prefab");
        }

        /// <summary>Sets a private serialized object reference (the components expose no setter).</summary>
        private static void SetPrivate(Object target, string property, Object value)
        {
            if (target == null) return;
            var so = new SerializedObject(target);
            var prop = so.FindProperty(property);
            if (prop == null)
            {
                Debug.LogWarning($"[SceneGenerator] {target.GetType().Name} has no serialized field '{property}'");
                return;
            }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateHUD()
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

            // Load HUD Sprites (Fantasy Gothic Aether-punk Theme)
            Sprite sprPanel = LoadSprite("Assets/Art/UI/panel_frame_neon.png") ?? LoadSprite("Assets/Art/UI/HUD/hud_panel_frame.png");
            Sprite sprDial = LoadSprite("Assets/Art/UI/HUD/hud_realm_dial.png");
            Sprite sprBarFrame = LoadSprite("Assets/Art/UI/hud_bar_bg.png") ?? LoadSprite("Assets/Art/UI/HUD/hud_bar_frame.png");
            Sprite sprHpFill = LoadSprite("Assets/Art/UI/hud_health_fill.png") ?? LoadSprite("Assets/Art/UI/HUD/hud_bar_hp_fill.png");
            Sprite sprCeFill = LoadSprite("Assets/Art/UI/hud_energy_fill.png") ?? LoadSprite("Assets/Art/UI/HUD/hud_bar_ce_fill.png");
            Sprite sprSlotFrame = LoadSprite("Assets/Art/UI/icon_frame.png") ?? LoadSprite("Assets/Art/UI/HUD/hud_slot_frame.png");
            Sprite sprGuideBg = LoadSprite("Assets/Art/UI/HUD/hud_guide_bg.png");

            Sprite iconDash = LoadSprite("Assets/Art/UI/Icons/icon_ability_gravity.png");
            Sprite iconWallJump = LoadSprite("Assets/Art/UI/Icons/icon_ability_walljump.png");
            Sprite iconResonance = LoadSprite("Assets/Art/UI/Icons/icon_ability_resonance.png");
            Sprite iconAnchor = LoadSprite("Assets/Art/UI/Icons/icon_ability_echoanchor.png");

            // --- HUD Main Frame Panel ---
            GameObject panel = new GameObject("HUD_Panel");
            panel.transform.SetParent(canvasObj.transform, false);
            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.anchoredPosition = new Vector2(24f, -24f);
            panelRect.sizeDelta = new Vector2(430f, 168f);

            if (sprPanel != null)
            {
                Image panelImg = panel.AddComponent<Image>();
                panelImg.sprite = sprPanel;
                panelImg.type = Image.Type.Sliced;
                panelImg.color = Color.white;
            }

            // Chrono Dial Badge (Emblem)
            GameObject dialObj = new GameObject("Chrono_Dial_Badge");
            dialObj.transform.SetParent(panel.transform, false);
            RectTransform dialRect = dialObj.AddComponent<RectTransform>();
            dialRect.anchorMin = new Vector2(0f, 1f);
            dialRect.anchorMax = new Vector2(0f, 1f);
            dialRect.pivot = new Vector2(0f, 1f);
            dialRect.anchoredPosition = new Vector2(16f, -16f);
            dialRect.sizeDelta = new Vector2(54f, 54f);
            Image dialImg = dialObj.AddComponent<Image>();
            dialImg.sprite = sprDial;
            dialImg.color = new Color(0f, 0.85f, 1f, 1f); // Initial Prime Cyan

            // Realm Indicator Text (Orbitron)
            GameObject realmTextObj = new GameObject("Text_Realm");
            realmTextObj.transform.SetParent(panel.transform, false);
            RectTransform realmRect = realmTextObj.AddComponent<RectTransform>();
            realmRect.anchorMin = new Vector2(0f, 1f);
            realmRect.anchorMax = new Vector2(0f, 1f);
            realmRect.pivot = new Vector2(0f, 1f);
            realmRect.anchoredPosition = new Vector2(80f, -16f);
            realmRect.sizeDelta = new Vector2(330f, 26f);
            Text realmText = realmTextObj.AddComponent<Text>();
            realmText.font = fontOrbitron;
            realmText.fontSize = 18;
            realmText.fontStyle = FontStyle.Bold;
            realmText.alignment = TextAnchor.MiddleLeft;
            realmText.text = "REALM: PRIME";
            realmText.color = new Color(0f, 0.85f, 1f, 1f);

            // Health Bar Background / Frame
            GameObject hpBgObj = new GameObject("HP_Bar");
            hpBgObj.transform.SetParent(panel.transform, false);
            RectTransform hpBgRect = hpBgObj.AddComponent<RectTransform>();
            hpBgRect.anchorMin = new Vector2(0f, 1f);
            hpBgRect.anchorMax = new Vector2(0f, 1f);
            hpBgRect.pivot = new Vector2(0f, 1f);
            hpBgRect.anchoredPosition = new Vector2(80f, -44f);
            hpBgRect.sizeDelta = new Vector2(330f, 20f);
            Image hpBg = hpBgObj.AddComponent<Image>();
            hpBg.sprite = sprBarFrame;
            hpBg.type = Image.Type.Sliced;
            hpBg.color = Color.white;

            // Health Bar Fill
            GameObject hpFillObj = new GameObject("HP_Fill");
            hpFillObj.transform.SetParent(hpBgObj.transform, false);
            RectTransform hpFillRect = hpFillObj.AddComponent<RectTransform>();
            hpFillRect.anchorMin = Vector2.zero;
            hpFillRect.anchorMax = Vector2.one;
            hpFillRect.offsetMin = new Vector2(3f, 3f);
            hpFillRect.offsetMax = new Vector2(-3f, -3f);
            Image hpFill = hpFillObj.AddComponent<Image>();
            hpFill.sprite = sprHpFill;
            hpFill.type = Image.Type.Filled;
            hpFill.fillMethod = Image.FillMethod.Horizontal;
            hpFill.color = Color.white;

            // Health Value Text
            GameObject hpValObj = new GameObject("HP_Value");
            hpValObj.transform.SetParent(hpBgObj.transform, false);
            RectTransform hpValRect = hpValObj.AddComponent<RectTransform>();
            hpValRect.anchorMin = Vector2.zero;
            hpValRect.anchorMax = Vector2.one;
            hpValRect.sizeDelta = Vector2.zero;
            Text hpValueText = hpValObj.AddComponent<Text>();
            hpValueText.font = fontSpaceMono;
            hpValueText.fontSize = 11;
            hpValueText.fontStyle = FontStyle.Bold;
            hpValueText.alignment = TextAnchor.MiddleCenter;
            hpValueText.text = "100 / 100";
            hpValueText.color = new Color(0.95f, 1f, 0.95f, 0.95f);

            // Chrono Energy Bar Background / Frame
            GameObject ceBgObj = new GameObject("CE_Bar");
            ceBgObj.transform.SetParent(panel.transform, false);
            RectTransform ceBgRect = ceBgObj.AddComponent<RectTransform>();
            ceBgRect.anchorMin = new Vector2(0f, 1f);
            ceBgRect.anchorMax = new Vector2(0f, 1f);
            ceBgRect.pivot = new Vector2(0f, 1f);
            ceBgRect.anchoredPosition = new Vector2(80f, -68f);
            ceBgRect.sizeDelta = new Vector2(330f, 18f);
            Image ceBg = ceBgObj.AddComponent<Image>();
            ceBg.sprite = sprBarFrame;
            ceBg.type = Image.Type.Sliced;
            ceBg.color = Color.white;

            // Energy Bar Fill
            GameObject ceFillObj = new GameObject("CE_Fill");
            ceFillObj.transform.SetParent(ceBgObj.transform, false);
            RectTransform ceFillRect = ceFillObj.AddComponent<RectTransform>();
            ceFillRect.anchorMin = Vector2.zero;
            ceFillRect.anchorMax = Vector2.one;
            ceFillRect.offsetMin = new Vector2(3f, 3f);
            ceFillRect.offsetMax = new Vector2(-3f, -3f);
            Image ceFill = ceFillObj.AddComponent<Image>();
            ceFill.sprite = sprCeFill;
            ceFill.type = Image.Type.Filled;
            ceFill.fillMethod = Image.FillMethod.Horizontal;
            ceFill.color = Color.white;

            // Energy Value Text
            GameObject ceValObj = new GameObject("CE_Value");
            ceValObj.transform.SetParent(ceBgObj.transform, false);
            RectTransform ceValRect = ceValObj.AddComponent<RectTransform>();
            ceValRect.anchorMin = Vector2.zero;
            ceValRect.anchorMax = Vector2.one;
            ceValRect.sizeDelta = Vector2.zero;
            Text ceValueText = ceValObj.AddComponent<Text>();
            ceValueText.font = fontSpaceMono;
            ceValueText.fontSize = 11;
            ceValueText.fontStyle = FontStyle.Bold;
            ceValueText.alignment = TextAnchor.MiddleCenter;
            ceValueText.text = "100 / 100";
            ceValueText.color = new Color(1f, 0.95f, 0.85f, 0.95f);

            // --- Skill Tray (4 Ability Slots: Dash, WallJump, Resonance, EchoAnchor) ---
            GameObject trayObj = new GameObject("Skill_Tray");
            trayObj.transform.SetParent(panel.transform, false);
            RectTransform trayRect = trayObj.AddComponent<RectTransform>();
            trayRect.anchorMin = new Vector2(0f, 1f);
            trayRect.anchorMax = new Vector2(0f, 1f);
            trayRect.pivot = new Vector2(0f, 1f);
            trayRect.anchoredPosition = new Vector2(16f, -94f);
            trayRect.sizeDelta = new Vector2(394f, 60f);

            // Helper to build ability slot
            Image imgDash = null;
            Image imgWallJump = null;
            Image imgResonance = null;
            Image imgAnchor = null;
            Image dashCooldownRadial = null;

            string[] slotKeys = { "[K] DASH", "[SPACE] WALL", "[U] STRIKE", "[E] ANCHOR" };
            Sprite[] slotIcons = { iconDash, iconWallJump, iconResonance, iconAnchor };

            for (int i = 0; i < 4; i++)
            {
                float posX = i * 100f;
                GameObject slot = new GameObject($"Slot_{i}");
                slot.transform.SetParent(trayObj.transform, false);
                RectTransform slotRect = slot.AddComponent<RectTransform>();
                slotRect.anchorMin = new Vector2(0f, 0.5f);
                slotRect.anchorMax = new Vector2(0f, 0.5f);
                slotRect.pivot = new Vector2(0f, 0.5f);
                slotRect.anchoredPosition = new Vector2(posX, 0f);
                slotRect.sizeDelta = new Vector2(90f, 54f);

                Image slotBg = slot.AddComponent<Image>();
                slotBg.sprite = sprSlotFrame;
                slotBg.type = Image.Type.Sliced;
                slotBg.color = Color.white;

                // Ability Icon
                GameObject iconObj = new GameObject("Icon");
                iconObj.transform.SetParent(slot.transform, false);
                RectTransform iconRect = iconObj.AddComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0.5f, 0.5f);
                iconRect.anchorMax = new Vector2(0.5f, 0.5f);
                iconRect.pivot = new Vector2(0.5f, 0.5f);
                iconRect.anchoredPosition = new Vector2(0f, 6f);
                iconRect.sizeDelta = new Vector2(28f, 28f);

                Image iconImg = iconObj.AddComponent<Image>();
                iconImg.sprite = slotIcons[i];
                iconImg.color = Color.white;

                if (i == 0) imgDash = iconImg;
                else if (i == 1) imgWallJump = iconImg;
                else if (i == 2) imgResonance = iconImg;
                else if (i == 3) imgAnchor = iconImg;

                // Dash Cooldown Overlay on Slot 0
                if (i == 0)
                {
                    GameObject cdObj = new GameObject("Cooldown_Overlay");
                    cdObj.transform.SetParent(iconObj.transform, false);
                    RectTransform cdRect = cdObj.AddComponent<RectTransform>();
                    cdRect.anchorMin = Vector2.zero;
                    cdRect.anchorMax = Vector2.one;
                    cdRect.sizeDelta = Vector2.zero;
                    dashCooldownRadial = cdObj.AddComponent<Image>();
                    dashCooldownRadial.sprite = iconDash;
                    dashCooldownRadial.type = Image.Type.Filled;
                    dashCooldownRadial.fillMethod = Image.FillMethod.Radial360;
                    dashCooldownRadial.color = new Color(0f, 0f, 0f, 0.75f);
                }

                // Key Label Text
                GameObject keyObj = new GameObject("Key_Label");
                keyObj.transform.SetParent(slot.transform, false);
                RectTransform keyRect = keyObj.AddComponent<RectTransform>();
                keyRect.anchorMin = new Vector2(0.5f, 0f);
                keyRect.anchorMax = new Vector2(0.5f, 0f);
                keyRect.pivot = new Vector2(0.5f, 0f);
                keyRect.anchoredPosition = new Vector2(0f, 3f);
                keyRect.sizeDelta = new Vector2(88f, 16f);
                Text keyText = keyObj.AddComponent<Text>();
                keyText.font = fontSpaceMono;
                keyText.fontSize = 9;
                keyText.fontStyle = FontStyle.Bold;
                keyText.alignment = TextAnchor.MiddleCenter;
                keyText.text = slotKeys[i];
                keyText.color = new Color(0.7f, 0.8f, 0.9f, 0.85f);
            }

            // Announcement Banner (Center Screen)
            GameObject annObj = new GameObject("Text_Announcement");
            annObj.transform.SetParent(canvasObj.transform, false);
            RectTransform annRect = annObj.AddComponent<RectTransform>();
            annRect.anchorMin = new Vector2(0.5f, 0.5f);
            annRect.anchorMax = new Vector2(0.5f, 0.5f);
            annRect.pivot = new Vector2(0.5f, 0.5f);
            annRect.anchoredPosition = new Vector2(0f, 100f);
            annRect.sizeDelta = new Vector2(900f, 80f);
            Text annText = annObj.AddComponent<Text>();
            annText.font = fontOrbitron;
            annText.fontSize = 28;
            annText.fontStyle = FontStyle.Bold;
            annText.alignment = TextAnchor.MiddleCenter;
            annText.color = new Color(1f, 0.85f, 0.2f, 1f);
            annObj.SetActive(false);

            // Controls Guide (Bottom Center with pill background)
            GameObject guideObj = new GameObject("Controls_Guide");
            guideObj.transform.SetParent(canvasObj.transform, false);
            RectTransform guideRect = guideObj.AddComponent<RectTransform>();
            guideRect.anchorMin = new Vector2(0.5f, 0f);
            guideRect.anchorMax = new Vector2(0.5f, 0f);
            guideRect.pivot = new Vector2(0.5f, 0f);
            guideRect.anchoredPosition = new Vector2(0f, 40f);
            guideRect.sizeDelta = new Vector2(1060f, 40f);

            if (sprGuideBg != null)
            {
                Image guideBg = guideObj.AddComponent<Image>();
                guideBg.sprite = sprGuideBg;
                guideBg.type = Image.Type.Sliced;
                guideBg.color = Color.white;
            }

            GameObject guideTextObj = new GameObject("Text");
            guideTextObj.transform.SetParent(guideObj.transform, false);
            RectTransform gtRect = guideTextObj.AddComponent<RectTransform>();
            gtRect.anchorMin = Vector2.zero;
            gtRect.anchorMax = Vector2.one;
            gtRect.sizeDelta = Vector2.zero;

            Text guideText = guideTextObj.AddComponent<Text>();
            guideText.font = fontSpaceMono;
            guideText.fontSize = 13;
            guideText.alignment = TextAnchor.MiddleCenter;
            guideText.color = new Color(0.85f, 0.92f, 1f, 0.95f);
            guideText.text = "[A / D] Move   |   [SPACE] Jump   |   [K] Dash   |   [J] Attack   |   [SHIFT] Reality Shift   |   [U] Resonance  |  [F] Anchor  |  [Q] Gravity";

            // Attach PlayerHUD component
            var hud = canvasObj.AddComponent<PlayerHUD>();
            hud.BindElements(hpFill, ceFill, dashCooldownRadial, realmText, dialImg, annText,
                hpValueText, ceValueText, imgDash, imgWallJump, imgResonance, imgAnchor);
        }

        private static string VfxPrefabName(VfxId id)
        {
            switch (id)
            {
                case VfxId.SlashArcPrime: return "VFX_SlashArc_Prime";
                case VfxId.SlashArcEcho: return "VFX_SlashArc_Echo";
                case VfxId.ShiftWave: return "VFX_ShiftWave";
                case VfxId.DashDust: return "VFX_DashDust";
                case VfxId.LandDust: return "VFX_LandDust";
                case VfxId.ImpactClean: return "VFX_Impact_Clean";
                case VfxId.ImpactDeflect: return "VFX_Impact_Deflect";
                default: return "VFX_EnemyDeath";
            }
        }

        private static GameObject SaveOrUpdatePrefab(GameObject built, string prefabName, string subFolder)
        {
            string dir = $"{PREFAB_DIR}/{subFolder}";
            EnsureFolder(dir);
            string path = $"{dir}/{prefabName}.prefab";
            return PrefabUtility.SaveAsPrefabAssetAndConnect(built, path, InteractionMode.AutomatedAction);
        }

        private static void CreatePickup(Transform parent, string name, Vector3 pos, Sprite sprite,
            AbilityFlags ability, string displayName, Color glowColor)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = pos;
            go.layer = GetOrCreateLayer("Interactable", 12);

            var trigger = go.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = 0.8f;

            // Outer ethereal aura
            Sprite auraSprite = LoadSprite("Assets/Art/Particles/circle_05.png");
            if (auraSprite != null)
            {
                var aura = new GameObject("Aura");
                aura.transform.SetParent(go.transform, false);
                aura.transform.localScale = new Vector3(1.2f, 1.2f, 1f);
                var srAura = aura.AddComponent<SpriteRenderer>();
                srAura.sprite = auraSprite;
                srAura.color = new Color(glowColor.r, glowColor.g, glowColor.b, 0.35f);
                srAura.sortingOrder = 2;
            }

            // Artifact Icon Visual
            var visual = new GameObject("Visual");
            visual.transform.SetParent(go.transform, false);
            visual.transform.localScale = Vector3.one;
            visual.layer = go.layer;
            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = Color.white;
            sr.sortingOrder = 3;

            // Point Light2D for luminous glow
            var lightObj = new GameObject("Glow_Light");
            lightObj.transform.SetParent(go.transform, false);
            var light = lightObj.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.pointLightOuterRadius = 2.2f;
            light.pointLightInnerRadius = 0.3f;
            light.color = glowColor;
            light.intensity = 1.2f;

            go.AddComponent<PersistentId>().SetId("pickup_" + ability.ToString().ToLowerInvariant());
            go.AddComponent<AbilityPickup>().Configure(ability, displayName);
            SaveOrUpdatePrefab(go, "Pickup_" + ability, "Environment");
        }

        private static void CreateStation(Transform parent, string name, Vector3 floorPoint, Sprite sprite, string id)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = floorPoint;
            go.layer = GetOrCreateLayer("Interactable", 12);

            var trigger = go.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(2.5f, 3.2f);
            trigger.offset = new Vector2(0f, 1.6f);

            var visual = new GameObject("Visual");
            visual.transform.SetParent(go.transform, false);
            visual.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            visual.transform.localScale = Vector3.one;
            visual.layer = go.layer;

            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = Color.white;
            sr.sortingOrder = 1;

            // Subtle cyan station glow
            var lightObj = new GameObject("Station_Light");
            lightObj.transform.SetParent(go.transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 1.8f, 0f);
            var light = lightObj.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.pointLightOuterRadius = 3.5f;
            light.pointLightInnerRadius = 0.5f;
            light.color = new Color(0f, 0.9f, 1f, 1f);
            light.intensity = 0.9f;

            go.AddComponent<ChronoStation>().Configure(id, 25);
            SaveOrUpdatePrefab(go, "Station_Chrono_" + id, "Environment");
        }

        private static void CreateLevelGoal(Transform parent, string name, Vector3 pos, Sprite sprite)
        {
            GameObject goalObj = new GameObject(name);
            goalObj.transform.SetParent(parent);
            goalObj.transform.position = pos;
            goalObj.transform.localScale = Vector3.one;

            var sr = goalObj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = Color.white;
            sr.sortingOrder = 2;

            var col = goalObj.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(2.5f, 3.5f);

            // Goal ambient rift light
            var lightObj = new GameObject("Rift_Light");
            lightObj.transform.SetParent(goalObj.transform, false);
            var light = lightObj.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.pointLightOuterRadius = 4f;
            light.pointLightInnerRadius = 0.8f;
            light.color = new Color(0.85f, 0.4f, 1f, 1f);
            light.intensity = 1.5f;

            goalObj.AddComponent<LevelGoalTrigger>();
            SaveOrUpdatePrefab(goalObj, "Level_Goal_Rift", "Environment");
        }

        /// <summary>Builds the title-screen scene: a camera and the self-building <see cref="MainMenuController"/>.</summary>
        [MenuItem("Tools/Echo of the Void/Generate Main Menu Scene")]
        public static void GenerateMainMenuScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObj = new GameObject("Main Camera");
            cameraObj.tag = "MainCamera";
            cameraObj.transform.position = new Vector3(0f, 0f, -10f);
            var cam = cameraObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6.5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.03f, 0.04f, 0.08f, 1f);
            cameraObj.AddComponent<AudioListener>();
            cameraObj.AddComponent<UniversalAdditionalCameraData>();

            var menuObj = new GameObject("MainMenu");
            menuObj.AddComponent<MainMenuController>().SetFont(AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/SpaceMono-Regular.ttf"));

            if (!Directory.Exists(SCENE_DIR)) Directory.CreateDirectory(SCENE_DIR);
            EditorSceneManager.SaveScene(scene, MENU_SCENE_PATH);
            Debug.Log($"[SceneGenerator] Saved Main Menu Scene to {MENU_SCENE_PATH}");
        }

        /// <summary>Builds atmospheric multi-layer background with parallax and ambient particles.</summary>
        private static void CreateAtmosphericBackground(Transform parent, Sprite boxSprite)
        {
            var bgRoot = new GameObject("Background_Parallax");
            bgRoot.transform.SetParent(parent, false);
            bgRoot.AddComponent<ParallaxBackground>();

            // Layer 0: Deep Void starry nebula backdrop (immense distance)
            var deepSky = new GameObject("Layer_0_DeepVoid");
            deepSky.transform.SetParent(bgRoot.transform, false);
            deepSky.transform.position = new Vector3(70f, 22f, 0f);
            var deepSr = deepSky.AddComponent<SpriteRenderer>();
            deepSr.sprite = boxSprite;
            deepSr.color = new Color(0.04f, 0.06f, 0.12f, 1f); // Deep Cosmic Navy Void
            deepSky.transform.localScale = new Vector3(250f, 65f, 1f);
            deepSr.sortingOrder = -30;

            // Layer 0.5: Ethereal Chrono Nebula glow band
            var nebula = new GameObject("Layer_0_NebulaBand");
            nebula.transform.SetParent(bgRoot.transform, false);
            nebula.transform.position = new Vector3(70f, 26f, 0f);
            var nebSr = nebula.AddComponent<SpriteRenderer>();
            nebSr.sprite = boxSprite;
            nebSr.color = new Color(0.07f, 0.16f, 0.28f, 0.75f); // Luminous cyan/teal nebula band
            nebula.transform.localScale = new Vector3(250f, 30f, 1f);
            nebSr.sortingOrder = -25;

            // Layer 1: Distant Ancient Megastructure Silhouettes (towers, clockwork pillars)
            var distantPillars = new GameObject("Layer_1_DistantSilhouettes");
            distantPillars.transform.SetParent(bgRoot.transform, false);
            distantPillars.transform.position = new Vector3(0f, 0f, 0f);
            Color silhouetteColor = new Color(0.06f, 0.09f, 0.16f, 0.85f);
            for (int i = -3; i <= 10; i++)
            {
                float x = i * 18f;
                var colObj = new GameObject($"DistantPillar_{i}");
                colObj.transform.SetParent(distantPillars.transform, false);
                colObj.transform.position = new Vector3(x, 22f, 0f);
                colObj.transform.localScale = new Vector3(6f, 50f, 1f);
                var sr = colObj.AddComponent<SpriteRenderer>();
                sr.sprite = boxSprite;
                sr.color = silhouetteColor;
                sr.sortingOrder = -20;
            }

            // Layer 2: Midground Gothic Tech Beams & Conduits
            var midgroundBeams = new GameObject("Layer_2_MidBeams");
            midgroundBeams.transform.SetParent(bgRoot.transform, false);
            midgroundBeams.transform.position = new Vector3(0f, 0f, 0f);
            Color beamColor = new Color(0.09f, 0.13f, 0.22f, 0.75f);
            for (int i = -2; i <= 9; i++)
            {
                float x = i * 22f - 6f;
                var beamObj = new GameObject($"GothicBeam_{i}");
                beamObj.transform.SetParent(midgroundBeams.transform, false);
                beamObj.transform.position = new Vector3(x, 20f, 0f);
                beamObj.transform.localScale = new Vector3(2.5f, 40f, 1f);
                var sr = beamObj.AddComponent<SpriteRenderer>();
                sr.sprite = boxSprite;
                sr.color = beamColor;
                sr.sortingOrder = -12;
            }

            // Layer 3: Floating Ambient Dust Motes (glowing specks catching URP light)
            ParallaxBackground.CreateAmbientDustParticles(bgRoot.transform, new Color(0.35f, 0.85f, 1f, 0.7f));
        }

        private static RoomBounds CreateRoomBounds(Transform parent, string roomId, string displayName, Vector3 center, Vector2 size)
        {
            var boundsObj = new GameObject("RoomBounds");
            boundsObj.transform.SetParent(parent, false);
            boundsObj.transform.position = center;
            var box = boundsObj.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = size;
            var roomBounds = boundsObj.AddComponent<RoomBounds>();
            roomBounds.Configure(roomId, displayName);
            return roomBounds;
        }

        private static void CreateZone1Rooms1To10(
            Transform levelRoot,
            Transform combatRoot,
            Sprite sprNeutralPlat, Sprite sprPrimePlat, Sprite sprEchoPlat, Sprite sprStation, Sprite sprGoalRift,
            Sprite iconWallJump, Sprite iconResonance, Sprite boxSprite,
            Sprite sprCrawlerPrime, Sprite sprCrawlerEcho, Sprite sprWeaverPrime, Sprite sprWeaverEcho, Sprite sprKnightPrime, Sprite sprKnightEcho,
            Color neutralColor, Color primeColor, Color echoColor,
            int neutralLayer, int primeLayer, int echoLayer, int enemyLayer, int hazardLayer, int interactableLayer,
            EnemyDataSO dummyPrimeSO, EnemyDataSO dummyEchoSO, EnemyDataSO crawlerSO, EnemyDataSO weaverSO)
        {
            // -------------------------------------------------------------
            // [Row 1] Z1_R01: Foundry Entrance (20x14 m, X: -30..-10, Y: 0..14)
            // -------------------------------------------------------------
            var r01Obj = new GameObject("Z1_R01_FoundryEntrance");
            r01Obj.transform.SetParent(levelRoot, false);
            CreateRoomBounds(r01Obj.transform, "Z1_R01", "Foundry Entrance", new Vector3(-20f, 7f, 0f), new Vector2(20f, 14f));
            var geom01 = new GameObject("Geometry").transform; geom01.SetParent(r01Obj.transform, false);
            var enemy01 = new GameObject("Enemies").transform; enemy01.SetParent(r01Obj.transform, false);

            CreatePlatform(geom01, "Floor_R01", new Vector3(-20f, -0.5f, 0f), new Vector3(20f, 1f, 1f), neutralColor, sprNeutralPlat, neutralLayer);
            CreatePlatform(geom01, "Ceiling_R01", new Vector3(-20f, 14.5f, 0f), new Vector3(20f, 1f, 1f), neutralColor, sprNeutralPlat, neutralLayer);
            CreatePlatform(geom01, "Wall_West_R01", new Vector3(-30.5f, 7f, 0f), new Vector3(1f, 14f, 1f), neutralColor, sprNeutralPlat, neutralLayer);
            CreatePlatform(geom01, "Wall_East_Upper_R01", new Vector3(-9.5f, 9f, 0f), new Vector3(1f, 10f, 1f), neutralColor, sprNeutralPlat, neutralLayer); // doorway Y: 0..4

            CreatePlatform(geom01, "Plat_Tutorial_1", new Vector3(-20f, 2.5f, 0f), new Vector3(3f, 0.6f, 1f), neutralColor, sprNeutralPlat, neutralLayer);
            CreatePlatform(geom01, "Plat_Tutorial_2", new Vector3(-15f, 4.5f, 0f), new Vector3(3f, 0.6f, 1f), neutralColor, sprNeutralPlat, neutralLayer);
            CreateDummy(enemy01, "Dummy_Prime", new Vector3(-13f, 1.0f, 0f), RealmType.Prime, sprKnightPrime, enemyLayer, dummyPrimeSO);

            // -------------------------------------------------------------
            // [Row 1] Z1_R02: Outer Bastion (24x14 m, X: -10..14, Y: 0..14)
            // -------------------------------------------------------------
            var r02Obj = new GameObject("Z1_R02_OuterBastion");
            r02Obj.transform.SetParent(levelRoot, false);
            CreateRoomBounds(r02Obj.transform, "Z1_R02", "Outer Bastion", new Vector3(2f, 7f, 0f), new Vector2(24f, 14f));
            var geom02 = new GameObject("Geometry").transform; geom02.SetParent(r02Obj.transform, false);
            var interact02 = new GameObject("Interactive").transform; interact02.SetParent(r02Obj.transform, false);

            CreatePlatform(geom02, "Floor_R02", new Vector3(2f, -0.5f, 0f), new Vector3(24f, 1f, 1f), neutralColor, sprNeutralPlat, neutralLayer);
            CreatePlatform(geom02, "Ceiling_R02", new Vector3(6f, 14.5f, 0f), new Vector3(16f, 1f, 1f), neutralColor, sprNeutralPlat, neutralLayer); // opening X: -10..-2 up to R05
            CreatePlatform(geom02, "Wall_East_Upper_R02", new Vector3(14.5f, 9f, 0f), new Vector3(1f, 10f, 1f), neutralColor, sprNeutralPlat, neutralLayer); // doorway Y: 0..4

            CreateStation(interact02, "Station_Start", new Vector3(-6f, 0f, 0f), sprStation, "station_start");
            CreatePlatform(geom02, "Wall_Barrier_R02", new Vector3(0f, 3.5f, 0f), new Vector3(1f, 7f, 1f), neutralColor, sprNeutralPlat, neutralLayer);
            CreateRealityPlatform(geom02, "Plat_Prime_R02", new Vector3(-3.5f, 2.8f, 0f), new Vector3(3.5f, 0.6f, 1f), RealmType.Prime, primeColor, sprPrimePlat, primeLayer);
            CreateRealityPlatform(geom02, "Plat_Echo_R02", new Vector3(3.5f, 5.2f, 0f), new Vector3(3.5f, 0.6f, 1f), RealmType.Echo, echoColor, sprEchoPlat, echoLayer);
            CreatePlatform(geom02, "Ledge_Upper_R02", new Vector3(8.5f, 7.5f, 0f), new Vector3(5f, 0.6f, 1f), neutralColor, sprNeutralPlat, neutralLayer);

            // -------------------------------------------------------------
            // [Row 1] Z1_R03: Broken Conduits (30x14 m, X: 14..44, Y: 0..14)
            // -------------------------------------------------------------
            var r03Obj = new GameObject("Z1_R03_BrokenConduits");
            r03Obj.transform.SetParent(levelRoot, false);
            CreateRoomBounds(r03Obj.transform, "Z1_R03", "Broken Conduits", new Vector3(29f, 7f, 0f), new Vector2(30f, 14f));
            var geom03 = new GameObject("Geometry").transform; geom03.SetParent(r03Obj.transform, false);
            var interact03 = new GameObject("Interactive").transform; interact03.SetParent(r03Obj.transform, false);
            var enemy03 = new GameObject("Enemies").transform; enemy03.SetParent(r03Obj.transform, false);

            CreatePlatform(geom03, "Floor_R03", new Vector3(29f, -0.5f, 0f), new Vector3(30f, 1f, 1f), neutralColor, sprNeutralPlat, neutralLayer);
            CreatePlatform(geom03, "Ceiling_R03", new Vector3(29f, 14.5f, 0f), new Vector3(30f, 1f, 1f), neutralColor, sprNeutralPlat, neutralLayer);
            CreatePlatform(geom03, "Wall_East_Upper_R03", new Vector3(44.5f, 9f, 0f), new Vector3(1f, 10f, 1f), neutralColor, sprNeutralPlat, neutralLayer); // doorway Y: 0..4

            // Wall Jump Shaft
            CreatePlatform(geom03, "Wall_Shaft_Left_R03", new Vector3(31.5f, 8.25f, 0f), new Vector3(1f, 10.5f, 1f), neutralColor, sprNeutralPlat, neutralLayer); // opening below Y=3
            CreatePlatform(geom03, "Wall_Shaft_Right_R03", new Vector3(35f, 7f, 0f), new Vector3(1f, 14f, 1f), neutralColor, sprNeutralPlat, neutralLayer);
            CreatePlatform(geom03, "Plat_ShaftTop_R03", new Vector3(33.25f, 10.5f, 0f), new Vector3(3.5f, 0.6f, 1f), neutralColor, sprNeutralPlat, neutralLayer);
            CreatePickup(interact03, "Pickup_PistonBoots", new Vector3(33.25f, 11.4f, 0f), iconWallJump, AbilityFlags.WallJump, "PISTON BOOTS", new Color(1f, 0.85f, 0.2f, 1f));

            CreateCrawler(enemy03, "Crawler_Prime_R03", new Vector3(22f, 0.6f, 0f), sprCrawlerPrime, enemyLayer, crawlerSO);

            // -------------------------------------------------------------
            // [Row 1] Z1_R04: Steam Vent Shaft (16x14 m, X: 44..60, Y: 0..14)
            // -------------------------------------------------------------
            var r04Obj = new GameObject("Z1_R04_SteamVentShaft");
            r04Obj.transform.SetParent(levelRoot, false);
            CreateRoomBounds(r04Obj.transform, "Z1_R04", "Steam Vent Shaft", new Vector3(52f, 7f, 0f), new Vector2(16f, 14f));
            var geom04 = new GameObject("Geometry").transform; geom04.SetParent(r04Obj.transform, false);

            CreatePlatform(geom04, "Floor_R04", new Vector3(52f, -0.5f, 0f), new Vector3(16f, 1f, 1f), neutralColor, sprNeutralPlat, neutralLayer);
            CreatePlatform(geom04, "Wall_East_Outer_R04", new Vector3(60.5f, 7f, 0f), new Vector3(1f, 14f, 1f), neutralColor, sprNeutralPlat, neutralLayer);
            CreatePlatform(geom04, "Ceiling_R04_East", new Vector3(57f, 14.5f, 0f), new Vector3(6f, 1f, 1f), neutralColor, sprNeutralPlat, neutralLayer); // opening X: 48..54 up to R07

            CreatePlatform(geom04, "Plat_R04_1", new Vector3(48f, 3.5f, 0f), new Vector3(3.5f, 0.6f, 1f), neutralColor, sprNeutralPlat, neutralLayer);
            CreateRealityPlatform(geom04, "Plat_R04_2", new Vector3(55f, 6.5f, 0f), new Vector3(3.5f, 0.6f, 1f), RealmType.Prime, primeColor, sprPrimePlat, primeLayer);
            CreateRealityPlatform(geom04, "Plat_R04_3", new Vector3(48f, 9.5f, 0f), new Vector3(3.5f, 0.6f, 1f), RealmType.Echo, echoColor, sprEchoPlat, echoLayer);
            CreateRealityPlatform(geom04, "Plat_R04_4", new Vector3(55f, 12.5f, 0f), new Vector3(3.5f, 0.6f, 1f), RealmType.Prime, primeColor, sprPrimePlat, primeLayer);

            // -------------------------------------------------------------
            // [Row 2] Z1_R05: Cooling Core West (26x14 m, X: -10..16, Y: 14..28)
            // -------------------------------------------------------------
            var r05Obj = new GameObject("Z1_R05_CoolingCoreWest");
            r05Obj.transform.SetParent(levelRoot, false);
            CreateRoomBounds(r05Obj.transform, "Z1_R05", "Cooling Core West", new Vector3(3f, 21f, 0f), new Vector2(26f, 14f));
            var geom05 = new GameObject("Geometry").transform; geom05.SetParent(r05Obj.transform, false);
            var mech05 = new GameObject("Mechanics").transform; mech05.SetParent(r05Obj.transform, false);
            var enemy05 = new GameObject("Enemies").transform; enemy05.SetParent(r05Obj.transform, false);

            CreatePlatform(geom05, "Floor_R05", new Vector3(8f, 13.5f, 0f), new Vector3(16f, 1f, 1f), neutralColor, sprNeutralPlat, neutralLayer); // opening X: -10..0 down to R02
            CreatePlatform(geom05, "Wall_West_Outer_R05", new Vector3(-10.5f, 21f, 0f), new Vector3(1f, 14f, 1f), neutralColor, sprNeutralPlat, neutralLayer);
            CreatePlatform(geom05, "Ceiling_R05", new Vector3(3f, 28.5f, 0f), new Vector3(26f, 1f, 1f), neutralColor, sprNeutralPlat, neutralLayer);
            CreatePlatform(geom05, "Wall_East_Upper_R05", new Vector3(16.5f, 23f, 0f), new Vector3(1f, 10f, 1f), neutralColor, sprNeutralPlat, neutralLayer); // doorway Y: 14..18

            CreatePlatform(geom05, "Ledge_Climb_R05", new Vector3(-5f, 16.5f, 0f), new Vector3(4f, 0.6f, 1f), neutralColor, sprNeutralPlat, neutralLayer);
            BuildMechanic<EnergyGate>(mech05, "EnergyGate_R05", new Vector3(12f, 16f, 0f), new Vector2(0.5f, 4f), new Color(0.3f, 0.8f, 1f, 0.7f), boxSprite, hazardLayer, true, true);
            CreateVoidWeaver(enemy05, "Weaver_Echo_R05", new Vector3(6f, 22f, 0f), sprWeaverEcho, enemyLayer, weaverSO);

            // -------------------------------------------------------------
            // [Row 2] Z1_R06: Piston Gallery (32x14 m, X: 16..48, Y: 14..28)
            // -------------------------------------------------------------
            var r06Obj = new GameObject("Z1_R06_PistonGallery");
            r06Obj.transform.SetParent(levelRoot, false);
            CreateRoomBounds(r06Obj.transform, "Z1_R06", "Piston Gallery", new Vector3(32f, 21f, 0f), new Vector2(32f, 14f));
            var geom06 = new GameObject("Geometry").transform; geom06.SetParent(r06Obj.transform, false);
            var mech06 = new GameObject("Mechanics").transform; mech06.SetParent(r06Obj.transform, false);
            var interact06 = new GameObject("Interactive").transform; interact06.SetParent(r06Obj.transform, false);

            CreatePlatform(geom06, "Floor_West_R06", new Vector3(18f, 13.5f, 0f), new Vector3(4f, 1f, 1f), neutralColor, sprNeutralPlat, neutralLayer);
            CreatePlatform(geom06, "Floor_East_R06", new Vector3(46f, 13.5f, 0f), new Vector3(4f, 1f, 1f), neutralColor, sprNeutralPlat, neutralLayer);
            CreatePlatform(geom06, "Ceiling_R06", new Vector3(32f, 28.5f, 0f), new Vector3(32f, 1f, 1f), neutralColor, sprNeutralPlat, neutralLayer);
            CreatePlatform(geom06, "Wall_East_Upper_R06", new Vector3(48.5f, 23f, 0f), new Vector3(1f, 10f, 1f), neutralColor, sprNeutralPlat, neutralLayer); // doorway Y: 14..18

            BuildMechanic<Spikes>(mech06, "Spikes_R06", new Vector3(32f, 13.8f, 0f), new Vector2(24f, 0.6f), new Color(1f, 0.3f, 0.2f, 1f), boxSprite, hazardLayer, true, true);
            CreateRealityPlatform(geom06, "Plat_Prime_R06_1", new Vector3(24f, 16.5f, 0f), new Vector3(3.5f, 0.6f, 1f), RealmType.Prime, primeColor, sprPrimePlat, primeLayer);
            CreateRealityPlatform(geom06, "Plat_Echo_R06_2", new Vector3(32f, 18.0f, 0f), new Vector3(4.5f, 0.6f, 1f), RealmType.Echo, echoColor, sprEchoPlat, echoLayer);
            CreateRealityPlatform(geom06, "Plat_Prime_R06_3", new Vector3(40f, 16.5f, 0f), new Vector3(3.5f, 0.6f, 1f), RealmType.Prime, primeColor, sprPrimePlat, primeLayer);

            CreatePickup(interact06, "Pickup_EchoAnchor", new Vector3(32f, 19.2f, 0f), boxSprite, AbilityFlags.EchoAnchor, "ECHO ANCHOR", new Color(0.4f, 0.7f, 1f, 1f));

            // -------------------------------------------------------------
            // [Row 2] Z1_R07: Upper Conduit Cross (20x14 m, X: 48..68, Y: 14..28)
            // -------------------------------------------------------------
            var r07Obj = new GameObject("Z1_R07_UpperConduitCross");
            r07Obj.transform.SetParent(levelRoot, false);
            CreateRoomBounds(r07Obj.transform, "Z1_R07", "Upper Conduit Cross", new Vector3(58f, 21f, 0f), new Vector2(20f, 14f));
            var geom07 = new GameObject("Geometry").transform; geom07.SetParent(r07Obj.transform, false);
            var enemy07 = new GameObject("Enemies").transform; enemy07.SetParent(r07Obj.transform, false);

            CreatePlatform(geom07, "Floor_East_R07", new Vector3(61f, 13.5f, 0f), new Vector3(14f, 1f, 1f), neutralColor, sprNeutralPlat, neutralLayer); // opening X: 48..54 down to R04
            CreatePlatform(geom07, "Ceiling_West_R07", new Vector3(52f, 28.5f, 0f), new Vector3(8f, 1f, 1f), neutralColor, sprNeutralPlat, neutralLayer); // opening X: 56..68 up to R08
            CreatePlatform(geom07, "Wall_East_Outer_R07", new Vector3(68.5f, 21f, 0f), new Vector3(1f, 14f, 1f), neutralColor, sprNeutralPlat, neutralLayer);

            CreatePlatform(geom07, "Plat_Central_R07", new Vector3(58f, 18.5f, 0f), new Vector3(6f, 0.8f, 1f), neutralColor, sprNeutralPlat, neutralLayer);
            CreateRealityPlatform(geom07, "Plat_Upper_R07", new Vector3(62f, 23.5f, 0f), new Vector3(5f, 0.6f, 1f), RealmType.Prime, primeColor, sprPrimePlat, primeLayer);
            CreateCrawler(enemy07, "Crawler_Prime_R07", new Vector3(58f, 19.3f, 0f), sprCrawlerPrime, enemyLayer, crawlerSO);

            // -------------------------------------------------------------
            // [Row 3] Z1_R08: Smelting Chamber (36x16 m, X: 48..84, Y: 28..44)
            // -------------------------------------------------------------
            var r08Obj = new GameObject("Z1_R08_SmeltingChamber");
            r08Obj.transform.SetParent(levelRoot, false);
            CreateRoomBounds(r08Obj.transform, "Z1_R08", "Smelting Chamber", new Vector3(66f, 36f, 0f), new Vector2(36f, 16f));
            var geom08 = new GameObject("Geometry").transform; geom08.SetParent(r08Obj.transform, false);
            var mech08 = new GameObject("Mechanics").transform; mech08.SetParent(r08Obj.transform, false);

            CreatePlatform(geom08, "Floor_West_R08", new Vector3(52f, 27.5f, 0f), new Vector3(8f, 1f, 1f), neutralColor, sprNeutralPlat, neutralLayer); // opening X: 56..64 down from R07
            CreatePlatform(geom08, "Floor_East_R08", new Vector3(81f, 29.5f, 0f), new Vector3(6f, 1f, 1f), neutralColor, sprNeutralPlat, neutralLayer);
            CreatePlatform(geom08, "Wall_West_Outer_R08", new Vector3(47.5f, 36f, 0f), new Vector3(1f, 16f, 1f), neutralColor, sprNeutralPlat, neutralLayer);
            CreatePlatform(geom08, "Ceiling_R08", new Vector3(66f, 44.5f, 0f), new Vector3(36f, 1f, 1f), neutralColor, sprNeutralPlat, neutralLayer);
            CreatePlatform(geom08, "Wall_East_Upper_R08", new Vector3(84.5f, 39f, 0f), new Vector3(1f, 10f, 1f), neutralColor, sprNeutralPlat, neutralLayer); // doorway Y: 30..34
            CreatePlatform(geom08, "Wall_East_Lower_R08", new Vector3(84.5f, 29f, 0f), new Vector3(1f, 2f, 1f), neutralColor, sprNeutralPlat, neutralLayer);

            BuildMechanic<BouncePad>(mech08, "BouncePad_R08", new Vector3(60f, 28.2f, 0f), new Vector2(2f, 0.5f), new Color(0.4f, 1f, 0.6f, 1f), boxSprite, neutralLayer, false, true);
            BuildMechanic<Spikes>(mech08, "Spikes_R08", new Vector3(71f, 27.3f, 0f), new Vector2(14f, 0.6f), new Color(1f, 0.3f, 0.2f, 1f), boxSprite, hazardLayer, true, true);
            CreateRealityPlatform(geom08, "Plat_High_Echo_R08", new Vector3(70f, 36.5f, 0f), new Vector3(5f, 0.6f, 1f), RealmType.Echo, echoColor, sprEchoPlat, echoLayer);

            // -------------------------------------------------------------
            // [Row 3] Z1_R09: Resonance Archive (24x16 m, X: 84..108, Y: 28..44)
            // -------------------------------------------------------------
            var r09Obj = new GameObject("Z1_R09_ResonanceArchive");
            r09Obj.transform.SetParent(levelRoot, false);
            CreateRoomBounds(r09Obj.transform, "Z1_R09", "Resonance Archive", new Vector3(96f, 36f, 0f), new Vector2(24f, 16f));
            var geom09 = new GameObject("Geometry").transform; geom09.SetParent(r09Obj.transform, false);
            var interact09 = new GameObject("Interactive").transform; interact09.SetParent(r09Obj.transform, false);
            var enemy09 = new GameObject("Enemies").transform; enemy09.SetParent(r09Obj.transform, false);

            CreatePlatform(geom09, "Floor_R09", new Vector3(96f, 27.5f, 0f), new Vector3(24f, 1f, 1f), neutralColor, sprNeutralPlat, neutralLayer);
            CreatePlatform(geom09, "Ceiling_R09", new Vector3(96f, 44.5f, 0f), new Vector3(24f, 1f, 1f), neutralColor, sprNeutralPlat, neutralLayer);
            CreatePlatform(geom09, "Wall_East_Upper_R09", new Vector3(108.5f, 38f, 0f), new Vector3(1f, 12f, 1f), neutralColor, sprNeutralPlat, neutralLayer); // doorway Y: 28..32

            CreateStation(interact09, "Station_Archive", new Vector3(91f, 28f, 0f), sprStation, "station_archive");
            CreatePickup(interact09, "Pickup_ResonanceCore", new Vector3(100f, 29.5f, 0f), iconResonance, AbilityFlags.ResonanceStrike, "RESONANCE CORE", new Color(0.95f, 0.4f, 0.9f, 1f));
            PlacePrefab("Assets/Prefabs/Enemies/VoidStrider.prefab", enemy09, "Strider_R09", new Vector3(96f, 28.5f, 0f));

            // -------------------------------------------------------------
            // [Row 3] Z1_R10: Central Maintenance (24x16 m, X: 108..132, Y: 28..44)
            // -------------------------------------------------------------
            var r10Obj = new GameObject("Z1_R10_CentralMaintenance");
            r10Obj.transform.SetParent(levelRoot, false);
            CreateRoomBounds(r10Obj.transform, "Z1_R10", "Central Maintenance", new Vector3(120f, 36f, 0f), new Vector2(24f, 16f));
            var geom10 = new GameObject("Geometry").transform; geom10.SetParent(r10Obj.transform, false);
            var interact10 = new GameObject("Interactive").transform; interact10.SetParent(r10Obj.transform, false);

            CreatePlatform(geom10, "Floor_R10", new Vector3(120f, 27.5f, 0f), new Vector3(24f, 1f, 1f), neutralColor, sprNeutralPlat, neutralLayer);
            CreatePlatform(geom10, "Ceiling_R10", new Vector3(120f, 44.5f, 0f), new Vector3(24f, 1f, 1f), neutralColor, sprNeutralPlat, neutralLayer);
            CreatePlatform(geom10, "Wall_East_Upper_R10", new Vector3(132.5f, 39f, 0f), new Vector3(1f, 10f, 1f), neutralColor, sprNeutralPlat, neutralLayer); // doorway Y: 28..34 into Boss Arena

            CreateStation(interact10, "Station_Maintenance", new Vector3(114f, 28f, 0f), sprStation, "station_maintenance");
            CreatePlatform(geom10, "Goal_Altar_R10", new Vector3(124f, 29.5f, 0f), new Vector3(5f, 0.8f, 1f), neutralColor, sprNeutralPlat, neutralLayer);
            CreateLevelGoal(interact10, "Level_Goal_Rift", new Vector3(124f, 31.0f, 0f), sprGoalRift);

            // -------------------------------------------------------------
            // [Boss Arena] Z1_R18: Sentinel Foundry (44x16 m, X: 132..176, Y: 28..44)
            // -------------------------------------------------------------
            var r18Obj = new GameObject("Z1_R18_SentinelFoundry");
            r18Obj.transform.SetParent(levelRoot, false);
            CreateRoomBounds(r18Obj.transform, "Z1_R18", "Sentinel Foundry", new Vector3(154f, 36f, 0f), new Vector2(44f, 16f));
            var geom18 = new GameObject("Geometry").transform; geom18.SetParent(r18Obj.transform, false);
            var interact18 = new GameObject("Interactive").transform; interact18.SetParent(r18Obj.transform, false);

            CreatePlatform(geom18, "Floor_Arena", new Vector3(154f, 27.5f, 0f), new Vector3(44f, 1f, 1f), neutralColor, sprNeutralPlat, neutralLayer);
            CreatePlatform(geom18, "Ceiling_Arena", new Vector3(154f, 44.5f, 0f), new Vector3(44f, 1f, 1f), neutralColor, sprNeutralPlat, neutralLayer);
            CreatePlatform(geom18, "Wall_East_Outer", new Vector3(176.5f, 36f, 0f), new Vector3(1f, 16f, 1f), neutralColor, sprNeutralPlat, neutralLayer);

            CreateStation(interact18, "Station_BossGate", new Vector3(130f, 28f, 0f), sprStation, "station_boss");

            var gate = new GameObject("Arena_Gate");
            gate.transform.SetParent(r18Obj.transform, false);
            gate.transform.position = new Vector3(133f, 34f, 0f);
            gate.layer = neutralLayer;
            gate.AddComponent<BoxCollider2D>().size = new Vector2(1f, 12f);
            AddVisual(gate, boxSprite, new Vector2(1f, 12f), new Color(0.85f, 0.25f, 1f, 0.9f));
            gate.SetActive(false); // the arena locks it when the fight starts

            var bossGo = PlacePrefab("Assets/Prefabs/Bosses/Sentinel01.prefab", combatRoot, "Sentinel01", new Vector3(156f, 30.25f, 0f));
            var arenaGo = new GameObject("BossArena_Sentinel01");
            arenaGo.transform.SetParent(r18Obj.transform, false);
            arenaGo.transform.position = new Vector3(154f, 36f, 0f);
            var arenaBox = arenaGo.AddComponent<BoxCollider2D>();
            arenaBox.isTrigger = true;
            arenaBox.size = new Vector2(42f, 16f);

            if (bossGo != null)
            {
                var sentinel = bossGo.GetComponent<Sentinel01>();
                SetPrivate(sentinel, "missileWarningPrefab", EnsureMissileWarningPrefab(boxSprite));
                SetPrivate(sentinel, "missileSpawnPoint", bossGo.transform.Find("MissileSpawn"));
                SetPrivate(sentinel, "laserOrigin", bossGo.transform.Find("LaserOrigin"));

                var rewardPoint = new GameObject("RewardPoint").transform;
                rewardPoint.SetParent(arenaGo.transform, false);
                rewardPoint.position = new Vector3(156f, 29f, 0f);
                var stationPoint = new GameObject("StationPoint").transform;
                stationPoint.SetParent(arenaGo.transform, false);
                stationPoint.position = new Vector3(140f, 28f, 0f);

                var arena = arenaGo.AddComponent<BossArena>();
                arena.Configure(sentinel, gate,
                    EnsurePickupPrefab(boxSprite, AbilityFlags.GravityInversion, "GRAVITON CORE (test reward)", new Color(0.4f, 1f, 0.7f, 1f)),
                    rewardPoint,
                    AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_DIR}/Environment/Station_Chrono_station_boss.prefab"),
                    stationPoint);
            }
        }

        /// <summary>Build order: the title screen first, then the game.</summary>
        private static void ConfigureBuildSettings(params string[] scenePaths)
        {
            var scenes = new EditorBuildSettingsScene[scenePaths.Length];
            for (int i = 0; i < scenePaths.Length; i++) scenes[i] = new EditorBuildSettingsScene(scenePaths[i], true);
            EditorBuildSettings.scenes = scenes;
            Debug.Log($"[SceneGenerator] Build Settings updated: Scene 0 is {scenePaths[0]}");
        }
    }

    [InitializeOnLoad]
    public static class AutoRebuildHook
    {
        private const string MARKER_PATH = "Temp/RebuildRequested.marker";

        static AutoRebuildHook()
        {
            EditorApplication.update += CheckRebuild;
        }

        private static void CheckRebuild()
        {
            if (File.Exists(MARKER_PATH))
            {
                try { File.Delete(MARKER_PATH); } catch { }
                Debug.Log("[AutoRebuildHook] Marker detected. Automatically rebuilding Prototype Scene with new textures and UI...");
                SceneGenerator.ForceRebuildScene();
            }
        }
    }
}
