using System;
using System.Collections.Generic;

namespace EchoOfTheVoid.Settings
{
    /// <summary>One rebindable action and the inputs (Key / GamepadButton names) currently assigned to it.</summary>
    [Serializable]
    public class BindingEntry
    {
        public string action;
        public List<string> inputs = new List<string>();
    }

    /// <summary>Player preferences (spec 9.5, 9.6). Stored separately from save slots. Fields are additive.</summary>
    [Serializable]
    public class SettingsData
    {
        public int version = 1;

        // Audio (0..1)
        public float masterVolume = 1f;
        public float musicVolume = 0.8f;
        public float sfxVolume = 1f;
        public float uiVolume = 1f;

        // Accessibility (spec 9.6)
        public float screenShake = 1f;          // 0 = off, 1 = full
        public bool reduceFlashing;
        public bool colorblindPalette;
        public bool disableHitstop;
        public bool accessibleHud;              // fixed HP/CE bars

        // Assist mode (each option independent)
        public float damageTakenMultiplier = 1f; // 1, 0.75, 0.5
        public bool extendedCoyoteTime;
        public bool extendedIFrames;

        // Rebinding (empty = defaults)
        public List<BindingEntry> keyboardBindings = new List<BindingEntry>();
        public List<BindingEntry> gamepadBindings = new List<BindingEntry>();

        public string language = "en";
    }
}
