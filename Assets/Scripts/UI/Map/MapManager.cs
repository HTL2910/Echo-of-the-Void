using System;
using System.Collections.Generic;
using UnityEngine;
using EchoOfTheVoid.Core;
using EchoOfTheVoid.Save;

namespace EchoOfTheVoid.UI.Map
{
    /// <summary>
    /// Tracks room discovery and door unlock state. Syncs with SaveData.
    /// Raises events when rooms are discovered or doors unlocked.
    /// </summary>
    public class MapManager : MonoBehaviour
    {
        private static MapManager _instance;
        private HashSet<string> _discoveredRooms = new();
        private HashSet<string> _unlockedDoors = new();

        public static event Action<string> OnRoomDiscovered;
        public static event Action<string> OnDoorUnlocked;

        public static bool IsRoomDiscovered(string roomId) => _instance != null && _instance._discoveredRooms.Contains(roomId);
        public static bool IsDoorUnlocked(string doorId) => _instance != null && _instance._unlockedDoors.Contains(doorId);

        private void Awake()
        {
            if (_instance != null)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            GameFlow.OnGameLoaded += OnGameLoaded;
        }

        private void OnDisable()
        {
            GameFlow.OnGameLoaded -= OnGameLoaded;
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private void OnGameLoaded(SaveData data)
        {
            _discoveredRooms.Clear();
            _unlockedDoors.Clear();

            if (data?.visitedRooms != null)
                foreach (var roomId in data.visitedRooms)
                    _discoveredRooms.Add(roomId);

            if (data?.unlockedDoorIds != null)
                foreach (var doorId in data.unlockedDoorIds)
                    _unlockedDoors.Add(doorId);
        }

        public static void DiscoverRoom(string roomId)
        {
            if (_instance == null) return;
            if (_instance._discoveredRooms.Add(roomId))
            {
                OnRoomDiscovered?.Invoke(roomId);
                var current = GameSession.Current;
                if (current != null)
                {
                    current.visitedRooms.Add(roomId);
                    SaveService.Save(current, GameSession.ActiveSlot);
                }
            }
        }

        public static void UnlockDoor(string doorId)
        {
            if (_instance == null) return;
            if (_instance._unlockedDoors.Add(doorId))
            {
                OnDoorUnlocked?.Invoke(doorId);
                var current = GameSession.Current;
                if (current != null)
                {
                    current.unlockedDoorIds.Add(doorId);
                    SaveService.Save(current, GameSession.ActiveSlot);
                }
            }
        }
    }
}
