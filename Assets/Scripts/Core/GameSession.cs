using EchoOfTheVoid.Save;

namespace EchoOfTheVoid.Core
{
    /// <summary>
    /// The save the player is currently playing (spec 3.5b). Menus start it (New Game / Continue); gameplay code
    /// records progress into <see cref="Current"/>; Chrono Stations write it to disk.
    /// </summary>
    public static class GameSession
    {
        /// <summary>Progress being played. Never null.</summary>
        public static SaveData Current { get; private set; } = new SaveData();

        public static int ActiveSlot { get; private set; }

        /// <summary>Set by Continue: a save whose position/abilities <see cref="SaveBootstrap"/> applies once the scene loads.</summary>
        public static SaveData PendingLoad { get; private set; }

        public static void StartNewGame(int slot = 0)
        {
            ActiveSlot = slot;
            Current = new SaveData();
            PendingLoad = null;
        }

        /// <returns>false if the slot has no valid save.</returns>
        public static bool TryContinue(int slot)
        {
            if (!SaveService.TryLoad(slot, out var data)) return false;

            ActiveSlot = slot;
            Current = data;
            PendingLoad = data;
            return true;
        }

        /// <summary>Called by <see cref="SaveBootstrap"/> after it has applied the pending save.</summary>
        public static void ConsumePendingLoad() => PendingLoad = null;

        public static bool IsCollected(string id) => !string.IsNullOrEmpty(id) && Current.collectedIds.Contains(id);

        public static void MarkCollected(string id)
        {
            if (!string.IsNullOrEmpty(id) && !Current.collectedIds.Contains(id)) Current.collectedIds.Add(id);
        }

        public static bool IsBossDefeated(string id) => !string.IsNullOrEmpty(id) && Current.bossDefeated.Contains(id);

        public static void MarkBossDefeated(string id)
        {
            if (!string.IsNullOrEmpty(id) && !Current.bossDefeated.Contains(id)) Current.bossDefeated.Add(id);
        }

        /// <summary>Tests and "quit to menu" reset.</summary>
        public static void Reset()
        {
            Current = new SaveData();
            PendingLoad = null;
            ActiveSlot = 0;
        }
    }
}
