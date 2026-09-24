using System;
using System.Collections.Generic;
using UnityEngine;
using EchoOfTheVoid.Core;

namespace EchoOfTheVoid.UI.Map
{
    [Serializable]
    public struct RoomMapCoordinate
    {
        public int zoneIndex;          // 1: Z1, 2: Z2, ..., 5: Core
        public Vector2Int gridPos;     // Grid coordinates (0,0 = top-left)
        public Vector2Int size;        // Room size in grid cells (1x1, 2x1, etc.)
    }

    [Serializable]
    public enum RoomDoorDirection { North, South, East, West }

    [Serializable]
    public struct RoomDoorInfo
    {
        public RoomDoorDirection direction;
        public Vector2Int localOffset;  // Position relative to room's gridPos
        public bool isLocked;
        public AbilityFlags requiredAbility;
        public string targetRoomId;
    }

    [Serializable]
    public struct RoomMapDefinition
    {
        public string roomId;              // Unique ID (e.g., "Z1_R01")
        public string displayName;         // Display name (e.g., "Foundry Entrance")
        public RoomMapCoordinate coordinate;
        public List<RoomDoorInfo> doors;
        public bool hasChronoStation;
        public bool hasMonolith;
        public bool hasBoss;
        public string bossId;              // e.g., "Sentinel01" (if hasBoss)
    }

    /// <summary>
    /// Defines all room metadata for the blueprint map system.
    /// Create one in Assets/Data/ and assign to MapSystem prefab.
    /// Will be populated by anti during level design (B1-B5).
    /// </summary>
    [CreateAssetMenu(menuName = "EchoOfTheVoid/Map/MapDefinitionData", fileName = "MapDefinitionData")]
    public class MapDefinitionData : ScriptableObject
    {
        [SerializeField] private List<RoomMapDefinition> rooms = new();

        public IReadOnlyList<RoomMapDefinition> Rooms => rooms;

        public RoomMapDefinition? GetRoomById(string roomId)
        {
            foreach (var room in rooms)
                if (room.roomId == roomId)
                    return room;
            return null;
        }

        public List<RoomMapDefinition> GetRoomsByZone(int zoneIndex)
        {
            var result = new List<RoomMapDefinition>();
            foreach (var room in rooms)
                if (room.coordinate.zoneIndex == zoneIndex)
                    result.Add(room);
            return result;
        }

        public RoomMapDefinition? GetRoomAt(int zoneIndex, Vector2Int gridPos)
        {
            foreach (var room in rooms)
                if (room.coordinate.zoneIndex == zoneIndex && room.coordinate.gridPos == gridPos)
                    return room;
            return null;
        }
    }
}
