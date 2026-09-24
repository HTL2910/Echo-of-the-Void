using System;
using System.Collections.Generic;
using UnityEngine;

namespace EchoOfTheVoid.Core
{
    /// <summary>
    /// Centralized EN/VI string localization (spec 11: Iris dialogue, Monolith, endings).
    /// Strings are stored in Docs/noi_dung/strings.json, loaded at game start.
    /// All UI/dialogue systems query via GetString(key).
    /// </summary>
    public class LocalizationService : MonoBehaviour
    {
        public static LocalizationService Instance { get; private set; }

        [SerializeField] private TextAsset stringsAsset;

        private Dictionary<string, LocalizedEntry> _strings = new Dictionary<string, LocalizedEntry>();
        private string _currentLanguage = "en";

        public string CurrentLanguage => _currentLanguage;

        public static event Action<string> OnLanguageChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            Load();
            SetLanguage(SettingsService.Current.language);
        }

        private void Load()
        {
            if (stringsAsset == null)
            {
                Debug.LogWarning("LocalizationService: stringsAsset not assigned, using empty dictionary");
                return;
            }

            try
            {
                var root = JsonUtility.FromJson<StringsRoot>(stringsAsset.text);
                if (root?.entries != null)
                {
                    foreach (var entry in root.entries)
                    {
                        _strings[entry.key] = entry;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"LocalizationService: Failed to parse strings: {ex.Message}");
            }
        }

        public void SetLanguage(string lang)
        {
            if (lang != "en" && lang != "vi")
                lang = "en";

            _currentLanguage = lang;
            SettingsService.Current.language = lang;
            SettingsService.Apply();
            OnLanguageChanged?.Invoke(lang);
        }

        /// <summary>Get localized string; fallback to key if not found or language missing.</summary>
        public string GetString(string key)
        {
            if (!_strings.TryGetValue(key, out var entry))
                return $"[{key}]";

            if (_currentLanguage == "vi" && !string.IsNullOrEmpty(entry.vi))
                return entry.vi;

            return !string.IsNullOrEmpty(entry.en) ? entry.en : $"[{key}]";
        }

        [System.Serializable]
        public class StringsRoot
        {
            public LocalizedEntry[] entries = new LocalizedEntry[0];
        }

        [System.Serializable]
        public class LocalizedEntry
        {
            public string key;
            public string en;
            public string vi;
        }
    }
}
