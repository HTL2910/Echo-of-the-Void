using UnityEngine;
using EchoOfTheVoid.Core;
using EchoOfTheVoid.Feedback;

namespace EchoOfTheVoid.Environment
{
    /// <summary>
    /// Spawns the 3-layer parallax background appropriate for the current level's zone.
    /// Place on any persistent GameObject in each level scene (or called by CampaignLevelBuilder).
    /// Falls back to procedural gradient backgrounds if sprites are not yet imported.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class BackgroundSetup : MonoBehaviour
    {
        [Header("Override (leave null to auto-detect zone from LevelManager)")]
        [SerializeField] private LevelTheme themeOverride = (LevelTheme)(-1);

        // ── Sprite references (assign in Inspector or auto-loaded) ────────────────
        [Header("Zone 1 – Aether Foundry")]
        [SerializeField] private Sprite z1_sky;
        [SerializeField] private Sprite z1_silhouette;
        [SerializeField] private Sprite z1_midground;

        [Header("Zone 2 – Void Depths")]
        [SerializeField] private Sprite z2_sky;

        [Header("Zone 3 – Crystal Archives (reuses z1 silhouette with tint)")]
        [SerializeField] private Sprite z3_sky;

        [Header("Zone 4 – Void Core")]
        [SerializeField] private Sprite z4_sky;

        // ── Background scale: how many world units wide each layer is ────────────
        private const float SKY_WIDTH       = 220f;
        private const float SILHOUETTE_WIDTH = 180f;
        private const float MIDGROUND_WIDTH  = 140f;

        // ── Parallax speeds (factor: 0 = camera-locked, 1 = world-static) ────────
        private const float SPEED_SKY  = 0.05f;
        private const float SPEED_SIL  = 0.2f;
        private const float SPEED_MID  = 0.45f;

        private void Awake()
        {
            AutoLoadSprites();
            SetupBackground();
        }

        // ── Public API ────────────────────────────────────────────────────────────
        public void SetupBackground()
        {
            LevelTheme theme = ResolveTheme();
            Color primary, secondary, dustColor;

            switch (theme)
            {
                case LevelTheme.Void:
                    primary     = new Color(0.52f, 0.16f, 0.86f);
                    secondary   = new Color(0.0f,  0.85f, 1.0f);
                    dustColor   = new Color(0.52f, 0.16f, 0.86f, 0.6f);
                    BuildLayers(z2_sky,    null,          null,          primary, secondary, theme);
                    break;

                case LevelTheme.Crystal:
                    primary     = new Color(0.18f, 0.9f, 0.55f);
                    secondary   = new Color(0.52f, 0.16f, 0.86f);
                    dustColor   = new Color(0.18f, 0.9f, 0.55f, 0.5f);
                    BuildLayers(z3_sky ?? z1_sky, z1_silhouette, null, primary, secondary, theme);
                    break;

                case LevelTheme.VoidCore:
                    primary     = new Color(1.0f, 0.42f, 0.0f);
                    secondary   = new Color(0.9f, 0.1f, 0.1f);
                    dustColor   = new Color(1.0f, 0.3f, 0.0f, 0.6f);
                    BuildLayers(z4_sky,    null,          null,          primary, secondary, theme);
                    break;

                default: // Foundry
                    primary     = new Color(0.0f, 0.85f, 1.0f);
                    secondary   = new Color(1.0f, 0.42f, 0.0f);
                    dustColor   = new Color(0.0f, 0.7f, 1.0f, 0.5f);
                    BuildLayers(z1_sky, z1_silhouette, z1_midground, primary, secondary, theme);
                    break;
            }
        }

        // ── Layer builder ─────────────────────────────────────────────────────────
        private void BuildLayers(Sprite sky, Sprite silhouette, Sprite midground,
                                 Color primary, Color secondary, LevelTheme theme)
        {
            var bgRoot = new GameObject("ParallaxBackground_Root");
            bgRoot.transform.SetParent(transform, false);

            var parallax = bgRoot.AddComponent<ParallaxBackground>();

            int layerCount = (sky != null ? 1 : 0) + (silhouette != null ? 1 : 0) + (midground != null ? 1 : 0);
            if (layerCount == 0) layerCount = 1; // always at least solid color

            var layerDefs = new System.Collections.Generic.List<(string name, Sprite spr, float speed, float z, float height, Color tint)>
            {
                ("Layer_Sky",       sky,        SPEED_SKY,  5f,  60f, Color.white),
                ("Layer_Silhouette",silhouette, SPEED_SIL,  3f,  36f, primary * 0.6f + Color.black * 0.4f),
                ("Layer_Midground", midground,  SPEED_MID,  2f,  30f, primary * 0.75f + Color.black * 0.25f),
            };

            var parallaxLayers = new System.Collections.Generic.List<ParallaxBackground.ParallaxLayer>();
            int sortOrder = -30;

            foreach (var (layerName, spr, speed, z, height, tint) in layerDefs)
            {
                var layerObj = new GameObject(layerName);
                layerObj.transform.SetParent(bgRoot.transform, false);
                layerObj.transform.position = new Vector3(50f, height * 0.5f, z);
                layerObj.transform.localScale = new Vector3(
                    spr != null ? SKY_WIDTH : SKY_WIDTH,
                    height,
                    1f);

                var sr = layerObj.AddComponent<SpriteRenderer>();
                sortOrder += 5;
                sr.sortingOrder = sortOrder;

                if (spr != null)
                {
                    sr.sprite = spr;
                    sr.color  = tint;
                    sr.drawMode = SpriteDrawMode.Tiled;
                    sr.size     = new Vector2(1f, 1f);
                }
                else
                {
                    // Procedural solid-color fallback
                    sr.sprite = GetWhiteSprite();
                    sr.color  = BuildGradientColor(layerName, primary, secondary, theme);
                }

                parallaxLayers.Add(new ParallaxBackground.ParallaxLayer
                {
                    transform       = layerObj.transform,
                    parallaxFactorX = speed,
                    parallaxFactorY = speed * 0.3f,
                    infiniteRepeatX = true,
                    repeatWidth     = SKY_WIDTH,
                });
            }

            // Inject layers into the ParallaxBackground via reflection (or just attach separately)
            // — use serialized field path trick since ParallaxBackground exposes Initialize()
            typeof(ParallaxBackground)
                .GetField("layers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(parallax, parallaxLayers.ToArray());

            parallax.Initialize();

            // Ambient dust particles
            Color dustCol = new Color(primary.r, primary.g, primary.b, 0.5f);
            ParallaxBackground.CreateAmbientDustParticles(bgRoot.transform, dustCol);
        }

        // ── Gradient fallback colors per layer ────────────────────────────────────
        private Color BuildGradientColor(string layerName, Color primary, Color secondary, LevelTheme theme)
        {
            var bg = new Color(0.04f, 0.05f, 0.12f); // base void dark
            return layerName switch
            {
                "Layer_Sky"       => Color.Lerp(bg,      primary  * 0.12f, 0.8f),
                "Layer_Silhouette"=> Color.Lerp(bg,      primary  * 0.08f, 0.6f),
                _                 => Color.Lerp(bg,      secondary * 0.06f, 0.4f),
            };
        }

        // ── Auto-load sprites from Resources ─────────────────────────────────────
        private void AutoLoadSprites()
        {
            if (z1_sky        == null) z1_sky        = LoadBgSprite("bg_z1_sky");
            if (z1_silhouette == null) z1_silhouette = LoadBgSprite("bg_z1_silhouette");
            if (z1_midground  == null) z1_midground  = LoadBgSprite("bg_z1_midground");
            if (z2_sky        == null) z2_sky        = LoadBgSprite("bg_z2_sky");
            if (z4_sky        == null) z4_sky        = LoadBgSprite("bg_z4_sky");
        }

        private static Sprite LoadBgSprite(string name)
        {
            // Try Resources folder first
            var spr = Resources.Load<Sprite>($"Backgrounds/{name}");
            if (spr != null) return spr;

            // Try loading from Art/Sprites/Backgrounds via AssetDatabase (Editor only)
#if UNITY_EDITOR
            var path = $"Assets/Art/Sprites/Backgrounds/{name}.jpg";
            spr = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (spr == null)
            {
                // Try .png too
                path = $"Assets/Art/Sprites/Backgrounds/{name}.png";
                spr = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
#endif
            return spr;
        }

        private LevelTheme ResolveTheme()
        {
            if ((int)themeOverride >= 0) return themeOverride;
            var mgr = LevelManager.Instance;
            return mgr?.CurrentLevel?.Theme ?? LevelTheme.Foundry;
        }

        private static Sprite _whiteSprite;
        private static Sprite GetWhiteSprite()
        {
            if (_whiteSprite != null) return _whiteSprite;
            var tex = new Texture2D(4, 4);
            for (int y = 0; y < 4; y++)
                for (int x = 0; x < 4; x++)
                    tex.SetPixel(x, y, Color.white);
            tex.Apply();
            _whiteSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
            return _whiteSprite;
        }
    }
}
