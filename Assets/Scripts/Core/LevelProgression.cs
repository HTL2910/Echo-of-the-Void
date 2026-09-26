using System.Collections.Generic;
using UnityEngine;

namespace EchoOfTheVoid.Core
{
    /// <summary>
    /// Static table of all 20 campaign levels. Zones 1-4, each with 5 levels and a boss at level 5.
    /// SceneName is used by GameFlow/SceneManager to load the correct scene.
    /// </summary>
    public static class LevelProgression
    {
        public const int TotalLevels = 20;

        public static readonly IReadOnlyList<LevelDef> All = new LevelDef[]
        {
            // ─── ZONE 1 – Aether Foundry ────────────────────────────────────────
            new LevelDef(1,  1, "Foundry Entrance",         LevelTheme.Foundry,  isBoss: false),
            new LevelDef(2,  1, "Outer Bastion",            LevelTheme.Foundry,  isBoss: false),
            new LevelDef(3,  1, "Broken Conduits",          LevelTheme.Foundry,  isBoss: false),
            new LevelDef(4,  1, "Piston Gallery",           LevelTheme.Foundry,  isBoss: false),
            new LevelDef(5,  1, "Sentinel Foundry",         LevelTheme.Foundry,  isBoss: true),

            // ─── ZONE 2 – Void Depths ────────────────────────────────────────────
            new LevelDef(6,  2, "Abyssal Approach",         LevelTheme.Void,     isBoss: false),
            new LevelDef(7,  2, "Gravity Inversion Field",  LevelTheme.Void,     isBoss: false),
            new LevelDef(8,  2, "Sentry Corridors",         LevelTheme.Void,     isBoss: false),
            new LevelDef(9,  2, "Collapsing Nexus",         LevelTheme.Void,     isBoss: false),
            new LevelDef(10, 2, "Echo Wraith Lair",         LevelTheme.Void,     isBoss: true),

            // ─── ZONE 3 – Crystal Archives ───────────────────────────────────────
            new LevelDef(11, 3, "Prismatic Atrium",         LevelTheme.Crystal,  isBoss: false),
            new LevelDef(12, 3, "Temporal Vaults",          LevelTheme.Crystal,  isBoss: false),
            new LevelDef(13, 3, "Rift Knight Citadel",      LevelTheme.Crystal,  isBoss: false),
            new LevelDef(14, 3, "Gargoyle Spires",          LevelTheme.Crystal,  isBoss: false),
            new LevelDef(15, 3, "Crystal Colossus Chamber", LevelTheme.Crystal,  isBoss: true),

            // ─── ZONE 4 – Void Core ──────────────────────────────────────────────
            new LevelDef(16, 4, "Fracture Point",           LevelTheme.VoidCore, isBoss: false),
            new LevelDef(17, 4, "Abyss Gauntlet",           LevelTheme.VoidCore, isBoss: false),
            new LevelDef(18, 4, "Collider Ruins",           LevelTheme.VoidCore, isBoss: false),
            new LevelDef(19, 4, "Final Approach",           LevelTheme.VoidCore, isBoss: false),
            new LevelDef(20, 4, "Void Sovereign",           LevelTheme.VoidCore, isBoss: true),
        };

        /// <summary>Return the LevelDef for a 1-based level index (1–20). Returns null if out of range.</summary>
        public static LevelDef Get(int levelIndex) =>
            (levelIndex >= 1 && levelIndex <= TotalLevels) ? All[levelIndex - 1] : null;

        /// <summary>Scene name matching the Unity Build Settings entry for a given level index.</summary>
        public static string SceneName(int levelIndex) => $"Level_{levelIndex:D2}";
    }

    public enum LevelTheme { Foundry, Void, Crystal, VoidCore }

    /// <summary>Immutable descriptor for a single campaign level.</summary>
    public sealed class LevelDef
    {
        public readonly int    LevelIndex;   // 1–20
        public readonly int    ZoneIndex;    // 1–4
        public readonly string DisplayName;
        public readonly LevelTheme Theme;
        public readonly bool   IsBoss;

        /// <summary>Number of regular rooms (boss levels have fewer, harder rooms).</summary>
        public int RoomCount => IsBoss ? 2 : Mathf.Clamp(3 + (LevelIndex / 4), 3, 6);

        /// <summary>Enemy difficulty multiplier (1.0 at L1, ~2.0 at L20).</summary>
        public float Difficulty => 1f + (LevelIndex - 1) / 19f;

        /// <summary>Primary neon accent colour for this zone's UI and geometry glow.</summary>
        public Color PrimaryColor => Theme switch
        {
            LevelTheme.Foundry  => new Color(0.0f, 0.85f, 1.0f),   // Cyan
            LevelTheme.Void     => new Color(0.85f, 0.25f, 1.0f),   // Purple
            LevelTheme.Crystal  => new Color(0.3f,  1.0f,  0.55f),  // Teal-green
            LevelTheme.VoidCore => new Color(1.0f,  0.42f, 0.0f),   // Orange
            _                   => Color.white,
        };

        public Color SecondaryColor => Theme switch
        {
            LevelTheme.Foundry  => new Color(1.0f, 0.42f, 0.0f),
            LevelTheme.Void     => new Color(0.0f, 0.85f, 1.0f),
            LevelTheme.Crystal  => new Color(0.85f, 0.25f, 1.0f),
            LevelTheme.VoidCore => new Color(0.85f, 0.25f, 1.0f),
            _                   => Color.gray,
        };

        public LevelDef(int levelIndex, int zoneIndex, string displayName, LevelTheme theme, bool isBoss)
        {
            LevelIndex  = levelIndex;
            ZoneIndex   = zoneIndex;
            DisplayName = displayName;
            Theme       = theme;
            IsBoss      = isBoss;
        }
    }
}
