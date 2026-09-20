using System;

namespace EchoOfTheVoid.Core
{
    /// <summary>Unlockable abilities (spec section 4). Stored as a bitmask in the save file: never reorder or reuse bits.</summary>
    [Flags]
    public enum AbilityFlags
    {
        None = 0,
        RealityShift = 1 << 0,      // Chrono Gauntlet (start of Z1)
        PhaseDash = 1 << 1,         // unlocked early in Z1 (spec D8)
        WallJump = 1 << 2,          // Piston Boots, boss Sentinel-01
        GravityInversion = 1 << 3,  // Graviton Core, boss Keeper Myra
        EchoAnchor = 1 << 4,        // boss Mirror Doppelganger
        ResonanceStrike = 1 << 5,   // mini-boss Rift Knight Prime
    }
}
