using System;
using System.Collections.Generic;

namespace EchoOfTheVoid.Save
{
    /// <summary>Serialized game progress for one save slot (spec 3.5b). Keep fields additive; bump SaveService.CurrentVersion on breaking changes.</summary>
    [Serializable]
    public class SaveData
    {
        public int version;
        public int slotId;
        public float playtimeSeconds;
        public string sceneName;

        // Checkpoint (Chrono Station)
        public string checkpointId;
        public float checkpointX;
        public float checkpointY;
        public int checkpointRealm;      // 0 = Prime, 1 = Echo

        // Player progress
        public int maxHealth = 100;
        public int currentHealth = 100;
        public int abilityFlags;         // bitmask, see AbilityFlags when the ability system lands

        // Persistent world state (stable ids from PersistentId components)
        public List<string> collectedIds = new List<string>();
        public List<string> bossDefeated = new List<string>();
        public List<string> visitedRooms = new List<string>();   // for the map (spec 9.3)
        public List<string> unlockedDoorIds = new List<string>(); // for map locks (spec 9.3)

        // Ending state
        public string endingChosen = "";  // "reset", "convergence", "sovereign" - filled when player picks an ending
    }
}
