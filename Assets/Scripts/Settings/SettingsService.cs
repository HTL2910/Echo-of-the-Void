using System;
using System.IO;
using UnityEngine;

namespace EchoOfTheVoid.Settings
{
    /// <summary>Loads/saves <see cref="SettingsData"/> as JSON (atomic write). <see cref="Current"/> is what the game reads.</summary>
    public static class SettingsService
    {
        public const int CurrentVersion = 1;

        /// <summary>Tests point this at a temp folder; null means Application.persistentDataPath.</summary>
        public static string DirectoryOverride;

        private static SettingsData _current;

        public static event Action Changed;

        public static string FilePath => Path.Combine(DirectoryOverride ?? Application.persistentDataPath, "settings.json");

        public static SettingsData Current
        {
            get
            {
                if (_current == null) _current = LoadOrDefaults();
                return _current;
            }
        }

        /// <summary>Call after editing <see cref="Current"/>: clamps values, notifies listeners and writes the file.</summary>
        public static void Apply()
        {
            Sanitize(Current);
            ApplyMasterVolume();
            Changed?.Invoke();
            Save();
        }

        public static bool Save()
        {
            try
            {
                Current.version = CurrentVersion;
                string path = FilePath;
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                string temp = path + ".tmp";
                File.WriteAllText(temp, JsonUtility.ToJson(Current, prettyPrint: true));
                if (File.Exists(path)) File.Delete(path);
                File.Move(temp, path);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Settings] Save failed: {e.Message}");
                return false;
            }
        }

        /// <summary>The master slider is a plain global: AudioListener.volume.</summary>
        public static void ApplyMasterVolume()
        {
            AudioListener.volume = Current.masterVolume;
        }

        /// <summary>Forget the in-memory copy so the next access reads the file again (tests, "revert").</summary>
        public static void Reload()
        {
            _current = null;
        }

        public static void ResetToDefaults()
        {
            _current = new SettingsData();
            Changed?.Invoke();
        }

        private static SettingsData LoadOrDefaults()
        {
            try
            {
                string path = FilePath;
                if (File.Exists(path))
                {
                    var loaded = JsonUtility.FromJson<SettingsData>(File.ReadAllText(path));
                    if (loaded != null && loaded.version > 0 && loaded.version <= CurrentVersion)
                    {
                        Sanitize(loaded);
                        return loaded;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Settings] Load failed, using defaults: {e.Message}");
            }
            return new SettingsData();
        }

        private static void Sanitize(SettingsData d)
        {
            d.masterVolume = Mathf.Clamp01(d.masterVolume);
            d.musicVolume = Mathf.Clamp01(d.musicVolume);
            d.sfxVolume = Mathf.Clamp01(d.sfxVolume);
            d.uiVolume = Mathf.Clamp01(d.uiVolume);
            d.screenShake = Mathf.Clamp01(d.screenShake);
            d.damageTakenMultiplier = Mathf.Clamp(d.damageTakenMultiplier, 0.25f, 1f);
            if (d.keyboardBindings == null) d.keyboardBindings = new System.Collections.Generic.List<BindingEntry>();
            if (d.gamepadBindings == null) d.gamepadBindings = new System.Collections.Generic.List<BindingEntry>();
        }
    }
}
