using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using EchoOfTheVoid.Core;
using EchoOfTheVoid.Environment;
using EchoOfTheVoid.Environment.Mechanics;
using EchoOfTheVoid.Enemies;
using System.IO;

namespace EchoOfTheVoid.EditorTools
{
    /// <summary>
    /// Builds all 20 campaign level scenes procedurally.
    /// Menu: Tools → Echo of the Void → Claude → Build All 20 Levels
    /// </summary>
    public static class CampaignLevelBuilder
    {
        private const string SCENE_DIR      = "Assets/Scenes";
        private const string ENEMY_DATA_DIR = "Assets/Settings/Enemies";

        // ─── GUID constants for sprites ──────────────────────────────────────────
        // Platform sprites — fallback if not found
        private static readonly string GUID_BOX_SPR       = "304b9871510734aa5919a2b1c89cd170";
        private const string DF = "Assets/Art/Sprites/Enemies/DarkFantasy";

        // ── Menu entry ────────────────────────────────────────────────────────────
        [MenuItem("Tools/Echo of the Void/Claude/Build All 20 Levels")]
        public static void BuildAll()
        {
            if (!EditorUtility.DisplayDialog(
                    "Build 20 Campaign Levels",
                    "This will create/overwrite 20 scene files in Assets/Scenes/.\nContinue?",
                    "Build", "Cancel"))
                return;

            if (!Directory.Exists(SCENE_DIR))
                Directory.CreateDirectory(SCENE_DIR);

            int built = 0;
            for (int i = 1; i <= LevelProgression.TotalLevels; i++)
            {
                try
                {
                    BuildLevel(LevelProgression.Get(i));
                    built++;
                    EditorUtility.DisplayProgressBar(
                        "Building Levels",
                        $"Level {i} / {LevelProgression.TotalLevels}: {LevelProgression.Get(i).DisplayName}",
                        i / (float)LevelProgression.TotalLevels);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[CampaignLevelBuilder] Failed on Level {i}: {ex}");
                }
            }

            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
            AddScenesToBuildSettings();

            EditorUtility.DisplayDialog("Done",
                $"Built {built} / {LevelProgression.TotalLevels} levels.\n" +
                "Scenes added to Build Settings.", "OK");

            Debug.Log($"[CampaignLevelBuilder] Campaign build complete. {built} scenes generated.");
        }

        // ── Per-level builder ─────────────────────────────────────────────────────
        private static void BuildLevel(LevelDef def)
        {
            string sceneName = LevelProgression.SceneName(def.LevelIndex);
            string scenePath = $"{SCENE_DIR}/{sceneName}.unity";

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Load sprites
            Sprite boxSprite = LoadOrCreateBox(def.PrimaryColor);
            Sprite platSprite = LoadSprite($"Assets/Art/Sprites/Environment/spr_platform_neutral.png") ?? boxSprite;
            Sprite primePlat  = LoadSprite($"Assets/Art/Sprites/Environment/spr_platform_prime.png")  ?? boxSprite;
            Sprite echoPlat   = LoadSprite($"Assets/Art/Sprites/Environment/spr_platform_echo.png")   ?? boxSprite;
            Sprite riftSprite = LoadSprite($"Assets/Art/Sprites/Environment/spr_level_goal_rift.png") ?? boxSprite;
            Sprite crawlerSpr = LoadSprite($"{DF}/chrono_crawler_prime_walk.png") ?? boxSprite;
            Sprite crawlerEcho= LoadSprite($"{DF}/chrono_crawler_echo_walk.png")  ?? boxSprite;
            Sprite weaverSpr  = LoadSprite($"{DF}/void_weaver_echo_idle.png")     ?? boxSprite;
            Sprite knightSpr  = LoadSprite($"{DF}/rift_knight_prime_idle.png")    ?? boxSprite;

            // Enemy SOs
            var crawlerSO = AssetDatabase.LoadAssetAtPath<EnemyDataSO>($"{ENEMY_DATA_DIR}/EnemyData_ChronoCrawler.asset");
            var weaverSO  = AssetDatabase.LoadAssetAtPath<EnemyDataSO>($"{ENEMY_DATA_DIR}/EnemyData_VoidWeaver.asset");
            var dummySO   = AssetDatabase.LoadAssetAtPath<EnemyDataSO>($"{ENEMY_DATA_DIR}/EnemyData_DummyPrime.asset");

            // Layers
            int neutralLayer = GetOrCreateLayer("Neutral", 6);
            int primeLayer   = GetOrCreateLayer("PrimeSolid", 7);
            int echoLayer    = GetOrCreateLayer("EchoSolid", 8);
            int enemyLayer   = GetOrCreateLayer("Enemy", 10);
            int hazardLayer  = GetOrCreateLayer("Hazard", 11);

            // Camera
            SetupCamera(def);
            SpawnBackground(def);

            // Environment root
            var levelRoot = new GameObject("Environment").transform;
            BuildBackground(levelRoot, boxSprite, def);

            // Generate rooms based on level def
            var ctx = new LevelBuildContext
            {
                Def        = def,
                BoxSpr     = boxSprite,
                PlatSpr    = platSprite,
                PrimePlat  = primePlat,
                EchoPlat   = echoPlat,
                CrawlerSpr = crawlerSpr,
                CrawlerEcho= crawlerEcho,
                WeaverSpr  = weaverSpr,
                KnightSpr  = knightSpr,
                RiftSpr    = riftSprite,
                LevelRoot  = levelRoot,
                NeutralLyr = neutralLayer,
                PrimeLyr   = primeLayer,
                EchoLyr    = echoLayer,
                EnemyLyr   = enemyLayer,
                HazardLyr  = hazardLayer,
                CrawlerSO  = crawlerSO,
                WeaverSO   = weaverSO,
                DummySO    = dummySO,
            };

            GenerateRooms(ctx);

            // Save scene
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[CampaignLevelBuilder] Saved: {scenePath}");
        }

        // ── Room generation ───────────────────────────────────────────────────────
        private static void GenerateRooms(LevelBuildContext c)
        {
            var def     = c.Def;
            int rooms   = def.RoomCount;
            float baseX = 0f;
            float roomW = 24f;      // room width in units
            float roomH = 16f;

            Color neutral  = Color.white;
            Color prime    = def.PrimaryColor;
            Color echo     = def.SecondaryColor;

            for (int r = 0; r < rooms; r++)
            {
                bool isFinalRoom = (r == rooms - 1);
                float cx = baseX + roomW * r + roomW * 0.5f;
                float cy = roomH * 0.5f;

                var roomObj = new GameObject($"Room_{r + 1:D2}_{(isFinalRoom ? (def.IsBoss ? "Boss" : "Exit") : "Combat")}");
                roomObj.transform.SetParent(c.LevelRoot, false);

                var geom   = new GameObject("Geometry").transform;
                var enemies = new GameObject("Enemies").transform;
                geom.SetParent(roomObj.transform, false);
                enemies.SetParent(roomObj.transform, false);

                float floorX = baseX + roomW * r + roomW * 0.5f;

                // Floor + Ceiling + Walls
                MakePlatform(geom, "Floor",   new Vector3(floorX, -0.5f, 0), new Vector3(roomW, 1, 1), neutral, c.PlatSpr, c.NeutralLyr);
                MakePlatform(geom, "Ceiling", new Vector3(floorX, roomH + 0.5f, 0), new Vector3(roomW, 1, 1), neutral, c.PlatSpr, c.NeutralLyr);

                // Left wall (skip first room left wall for entrance, add right wall only if not last room)
                if (r == 0)
                    MakePlatform(geom, "WallLeft",  new Vector3(baseX - 0.5f, cy, 0), new Vector3(1, roomH, 1), neutral, c.PlatSpr, c.NeutralLyr);
                if (isFinalRoom)
                    MakePlatform(geom, "WallRight", new Vector3(floorX + roomW * 0.5f + 0.5f, cy, 0), new Vector3(1, roomH, 1), neutral, c.PlatSpr, c.NeutralLyr);

                // Interior platforms — varies by room index and level theme
                SpawnInteriorPlatforms(geom, c, r, rooms, floorX, roomH, prime, echo);

                // Spawn enemies scaled to level difficulty
                if (!isFinalRoom || !def.IsBoss)
                    SpawnRoomEnemies(enemies, c, r, floorX, roomH);

                // Boss room
                if (isFinalRoom && def.IsBoss)
                    SpawnBossArena(roomObj.transform, c, floorX, roomH, prime, echo);

                // Exit rift in last non-boss room (or boss room after boss)
                if (isFinalRoom)
                    SpawnExitRift(roomObj.transform, c, floorX, roomH);
            }

            // Spawn player at level start
            SpawnPlayer(c, 0f, 1.5f);
        }

        private static void SpawnInteriorPlatforms(Transform parent, LevelBuildContext c, int roomIdx,
            int totalRooms, float roomCenterX, float roomH, Color prime, Color echo)
        {
            var def    = c.Def;
            float diff = def.Difficulty;  // 1.0 – 2.0
            int seed   = def.LevelIndex * 100 + roomIdx;
            Random.InitState(seed);

            // Number of platforms scales with difficulty
            int platCount = Mathf.Clamp(2 + roomIdx + (int)(diff * 1.5f), 2, 8);
            float roomLeft = roomCenterX - 12f;

            for (int p = 0; p < platCount; p++)
            {
                float px = roomLeft + 2f + Random.value * 20f;
                float py = 2f + Random.value * (roomH - 4f);
                float pw = 2f + Random.value * 3f;

                // Alternate prime/echo based on zone
                bool usePrime = (def.ZoneIndex % 2 == 1) ? (p % 2 == 0) : (p % 2 != 0);
                Sprite spr   = usePrime ? c.PrimePlat : c.EchoPlat;
                Color  col   = usePrime ? prime        : echo;
                int    layer = usePrime ? c.PrimeLyr   : c.EchoLyr;

                MakePlatform(parent, $"Plat_{p}", new Vector3(px, py, 0),
                    new Vector3(pw, 0.6f, 1), col, spr, layer);
            }

            // Hazard spikes — density increases with level
            int spikeGroups = (int)(diff * roomIdx * 0.4f);
            for (int s = 0; s < spikeGroups; s++)
            {
                float sx = roomLeft + 3f + Random.value * 18f;
                float sw = 1f + Random.value * 2f;
                var spikes = MakePlatform(parent, $"Spikes_{s}", new Vector3(sx, -0.1f, 0),
                    new Vector3(sw, 0.4f, 1), new Color(1f, 0.25f, 0.2f, 1f), c.BoxSpr, c.HazardLyr);
                // Add Spikes component
                spikes.AddComponent<Spikes>();
            }
        }

        private static void SpawnRoomEnemies(Transform parent, LevelBuildContext c, int roomIdx,
            float roomCenterX, float roomH)
        {
            var def    = c.Def;
            float diff = def.Difficulty;
            int seed   = def.LevelIndex * 1000 + roomIdx * 7;
            Random.InitState(seed);

            float roomLeft = roomCenterX - 10f;

            // Enemy count: 0 in room 0 (tutorial), scaling after
            int count = Mathf.Clamp(roomIdx + (int)(diff * 1.5f), roomIdx == 0 ? 0 : 1, 5);

            for (int e = 0; e < count; e++)
            {
                float ex  = roomLeft + 2f + Random.value * 16f;
                bool weaver = def.ZoneIndex >= 2 && e % 2 == 1 && def.LevelIndex >= 6;
                bool knight = def.ZoneIndex >= 3 && e % 3 == 2 && def.LevelIndex >= 11;

                if (knight && c.KnightSpr != null && c.DummySO != null)
                {
                    CreateEnemy(parent, $"RiftKnight_{e}", new Vector3(ex, 1f, 0),
                        c.KnightSpr, c.EnemyLyr, c.DummySO);
                }
                else if (weaver && c.WeaverSO != null)
                {
                    CreateWeaver(parent, $"Weaver_{e}", new Vector3(ex, 3f + Random.value * 4f, 0),
                        c.WeaverSpr, c.EnemyLyr, c.WeaverSO);
                }
                else if (c.CrawlerSO != null)
                {
                    CreateCrawler(parent, $"Crawler_{e}", new Vector3(ex, 0.8f, 0),
                        (e % 2 == 0) ? c.CrawlerSpr : c.CrawlerEcho, c.EnemyLyr, c.CrawlerSO);
                }
            }
        }

        private static void SpawnBossArena(Transform parent, LevelBuildContext c, float cx, float roomH,
            Color prime, Color echo)
        {
            var def = c.Def;
            // Boss arena: larger elevated platforms, minimal enemies (boss fight space)
            MakePlatform(parent, "Arena_Left",   new Vector3(cx - 6f, 2f, 0), new Vector3(5f, 0.6f, 1), prime, c.PrimePlat, c.PrimeLyr);
            MakePlatform(parent, "Arena_Right",  new Vector3(cx + 6f, 2f, 0), new Vector3(5f, 0.6f, 1), echo,  c.EchoPlat,  c.EchoLyr);
            MakePlatform(parent, "Arena_Center", new Vector3(cx, 6f, 0),       new Vector3(8f, 0.6f, 1), Color.white, c.PlatSpr, c.NeutralLyr);

            // Boss enemy (use Dummy as placeholder until Codex builds boss prefabs for each zone)
            string bossName = def.ZoneIndex switch { 1 => "Sentinel-01", 2 => "EchoWraith", 3 => "CrystalColossus", _ => "VoidSovereign" };
            if (c.DummySO != null)
                CreateEnemy(parent, $"Boss_{bossName}", new Vector3(cx, 1.5f, 0), c.KnightSpr ?? c.BoxSpr, c.EnemyLyr, c.DummySO);

            // Boss gate aura
            var gate = new GameObject("BossGate_Aura");
            gate.transform.SetParent(parent, false);
            gate.transform.position = new Vector3(cx - 11f, roomH * 0.5f, 0);
            gate.transform.localScale = new Vector3(0.5f, roomH, 1f);
            var sr = gate.AddComponent<SpriteRenderer>();
            sr.sprite = c.BoxSpr;
            sr.color  = new Color(def.PrimaryColor.r, def.PrimaryColor.g, def.PrimaryColor.b, 0.7f);
            sr.sortingOrder = 2;
        }

        private static void SpawnExitRift(Transform parent, LevelBuildContext c, float cx, float roomH)
        {
            // Place the exit rift at center-right of last room
            var rift = new GameObject("ExitRift");
            rift.transform.SetParent(parent, false);
            rift.transform.position = new Vector3(cx + 8f, 2f, 0);
            rift.transform.localScale = new Vector3(2f, 4f, 1f);

            var sr = rift.AddComponent<SpriteRenderer>();
            sr.sprite = c.RiftSpr ?? c.BoxSpr;
            sr.color  = c.Def.PrimaryColor;
            sr.sortingOrder = 3;

            var col = rift.AddComponent<BoxCollider2D>();
            col.isTrigger = true;

            rift.AddComponent<LevelGoalTrigger>();
        }

        private static void SpawnPlayer(LevelBuildContext c, float x, float y)
        {
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/Player.prefab");
            GameObject player;
            if (playerPrefab != null)
                player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            else
            {
                player = new GameObject("Player");
                player.tag = "Player";
            }
            player.transform.position = new Vector3(x, y, 0);
        }

        // ── Background Setup ──────────────────────────────────────────────────────
        private static void SpawnBackground(LevelDef def)
        {
            var bgGo = new GameObject("BackgroundSetup");
            var setup = bgGo.AddComponent<EchoOfTheVoid.Environment.BackgroundSetup>();

            // Set theme override via serialized property
            var so = new UnityEditor.SerializedObject(setup);
            var themeProp = so.FindProperty("themeOverride");
            if (themeProp != null)
            {
                themeProp.enumValueIndex = (int)def.Theme;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        // ── Camera Setup ─────────────────────────────────────────────────────────
        private static void SetupCamera(LevelDef def)
        {
            var camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            camObj.transform.position = new Vector3(0f, 5f, -10f);
            var cam = camObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 9f;
            cam.backgroundColor = new Color(0.04f, 0.05f, 0.1f);

            // Ambient glow hint via clear color
            var bg = def.PrimaryColor;
            cam.backgroundColor = new Color(bg.r * 0.05f, bg.g * 0.05f, bg.b * 0.05f);

            var al = camObj.AddComponent<AudioListener>();
        }

        // ── Background ────────────────────────────────────────────────────────────
        private static void BuildBackground(Transform parent, Sprite box, LevelDef def)
        {
            Color bg = def.PrimaryColor;
            float depth = 0f;

            var bgObj = new GameObject("BackgroundFill");
            bgObj.transform.SetParent(parent, false);
            bgObj.transform.position = new Vector3(50f, 8f, 10f);
            bgObj.transform.localScale = new Vector3(220f, 60f, 1f);
            var sr = bgObj.AddComponent<SpriteRenderer>();
            sr.sprite = box;
            sr.color  = new Color(bg.r * 0.06f, bg.g * 0.08f, bg.b * 0.15f, 1f);
            sr.sortingOrder = -20;
        }

        // ── Prefab helpers ────────────────────────────────────────────────────────
        private static void CreateCrawler(Transform parent, string name, Vector3 pos,
            Sprite spr, int layer, EnemyDataSO so)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Enemy_Crawler_Prime.prefab");
            GameObject obj;
            if (prefab != null)
            {
                obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                obj.name = name;
                obj.transform.position = pos;
            }
            else
            {
                obj = CreateEnemyInline(parent, name, pos, spr, layer, so, new Vector2(1f, 1f));
            }
        }

        private static void CreateWeaver(Transform parent, string name, Vector3 pos,
            Sprite spr, int layer, EnemyDataSO so)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Enemy_Weaver_Echo.prefab");
            GameObject obj;
            if (prefab != null)
            {
                obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                obj.name = name;
                obj.transform.position = pos;
            }
            else
            {
                obj = CreateEnemyInline(parent, name, pos, spr, layer, so, new Vector2(1.2f, 0.8f));
            }
        }

        private static void CreateEnemy(Transform parent, string name, Vector3 pos,
            Sprite spr, int layer, EnemyDataSO so)
        {
            CreateEnemyInline(parent, name, pos, spr, layer, so, new Vector2(1f, 1.2f));
        }

        private static GameObject CreateEnemyInline(Transform parent, string name, Vector3 pos,
            Sprite spr, int layer, EnemyDataSO so, Vector2 scale)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.transform.position = pos;
            obj.layer = layer;

            var visual = new GameObject("Visual");
            visual.transform.SetParent(obj.transform, false);
            visual.transform.localScale = new Vector3(scale.x, scale.y, 1f);
            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = spr;
            sr.sortingOrder = 1;
            return obj;
        }

        // ── Platform helpers ──────────────────────────────────────────────────────
        private static GameObject MakePlatform(Transform parent, string name, Vector3 pos,
            Vector3 scale, Color color, Sprite spr, int layer)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.transform.position    = pos;
            obj.transform.localScale  = scale;
            obj.layer = layer;

            var sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = spr;
            sr.color  = color;

            var col = obj.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;
            return obj;
        }

        // ── Build Settings ────────────────────────────────────────────────────────
        private static void AddScenesToBuildSettings()
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>();

            // Preserve MainMenu first
            foreach (var s in EditorBuildSettings.scenes)
                if (s.path.Contains("MainMenu") || s.path.Contains("Prototype_Level1"))
                    scenes.Add(s);

            // Add all 20 campaign levels
            for (int i = 1; i <= LevelProgression.TotalLevels; i++)
            {
                string path = $"{SCENE_DIR}/{LevelProgression.SceneName(i)}.unity";
                if (File.Exists(path))
                    scenes.Add(new EditorBuildSettingsScene(path, true));
            }

            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log($"[CampaignLevelBuilder] {scenes.Count} scenes added to Build Settings.");
        }

        // ── Sprite / Layer utilities ──────────────────────────────────────────────
        private static Sprite LoadSprite(string path)
        {
            var sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sp == null) sp = AssetDatabase.LoadAssetAtPath<Texture2D>(path) != null
                ? Sprite.Create(AssetDatabase.LoadAssetAtPath<Texture2D>(path),
                    new Rect(0, 0, AssetDatabase.LoadAssetAtPath<Texture2D>(path).width,
                                   AssetDatabase.LoadAssetAtPath<Texture2D>(path).height),
                    new Vector2(0.5f, 0.5f)) : null;
            return sp;
        }

        private static Sprite LoadOrCreateBox(Color tint)
        {
            var spr = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            if (spr != null) return spr;

            // Fallback: white 4x4 texture
            var tex = new Texture2D(4, 4);
            for (int y = 0; y < 4; y++)
                for (int x = 0; x < 4; x++)
                    tex.SetPixel(x, y, Color.white);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
        }

        private static int GetOrCreateLayer(string name, int defaultIndex)
        {
            for (int i = 0; i < 32; i++)
                if (LayerMask.LayerToName(i) == name) return i;
            return defaultIndex;
        }
    }

    // ── Data bag passed through room generation ───────────────────────────────────
    internal class LevelBuildContext
    {
        public LevelDef   Def;
        public Sprite     BoxSpr, PlatSpr, PrimePlat, EchoPlat;
        public Sprite     CrawlerSpr, CrawlerEcho, WeaverSpr, KnightSpr, RiftSpr;
        public Transform  LevelRoot;
        public int        NeutralLyr, PrimeLyr, EchoLyr, EnemyLyr, HazardLyr;
        public EnemyDataSO CrawlerSO, WeaverSO, DummySO;
    }
}
