using System;
using System.IO;
using UnityEngine;

namespace EchoOfTheVoid.Save
{
    /// <summary>JSON save files, one per slot. Writes are atomic so a crash mid-save never corrupts the previous save.</summary>
    public static class SaveService
    {
        public const int CurrentVersion = 1;
        public const int SlotCount = 3;

        /// <summary>Tests point this at a temp folder; null means Application.persistentDataPath.</summary>
        public static string DirectoryOverride;

        private static string Directory_ => DirectoryOverride ?? Application.persistentDataPath;

        public static string PathFor(int slot) => Path.Combine(Directory_, $"save_slot{slot}.json");

        public static bool Exists(int slot) => File.Exists(PathFor(slot));

        public static bool Save(SaveData data, int slot = 0)
        {
            if (data == null || slot < 0 || slot >= SlotCount) return false;

            try
            {
                data.version = CurrentVersion;
                data.slotId = slot;

                Directory.CreateDirectory(Directory_);
                string path = PathFor(slot);
                string temp = path + ".tmp";
                File.WriteAllText(temp, JsonUtility.ToJson(data, prettyPrint: true));

                if (File.Exists(path)) File.Delete(path);
                File.Move(temp, path);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveService] Save failed for slot {slot}: {e.Message}");
                return false;
            }
        }

        /// <summary>Returns false for a missing, unreadable, corrupt or too-new save (never throws).</summary>
        public static bool TryLoad(int slot, out SaveData data)
        {
            data = null;
            if (slot < 0 || slot >= SlotCount) return false;

            try
            {
                string path = PathFor(slot);
                if (!File.Exists(path)) return false;

                var loaded = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
                if (loaded == null || loaded.version <= 0 || loaded.version > CurrentVersion) return false;

                data = loaded;
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveService] Load failed for slot {slot}: {e.Message}");
                return false;
            }
        }

        public static void Delete(int slot)
        {
            try
            {
                string path = PathFor(slot);
                if (File.Exists(path)) File.Delete(path);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveService] Delete failed for slot {slot}: {e.Message}");
            }
        }
    }
}
