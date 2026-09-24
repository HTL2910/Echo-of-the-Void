using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using EchoOfTheVoid.Core;
using EchoOfTheVoid.Environment;
using EchoOfTheVoid.Player;
using EchoOfTheVoid.Save;

namespace EchoOfTheVoid.UI.Map
{
    /// <summary>
    /// Handles fast travel between discovered Chrono Stations.
    /// Only unlocked after defeating Z2 boss (per spec M5).
    /// </summary>
    public class FastTravelManager : MonoBehaviour
    {
        private static FastTravelManager _instance;
        private bool _isUnlocked;

        public static event Action<string> OnFastTravelRequested;

        public static bool IsUnlocked => _instance != null && _instance._isUnlocked;
        public static void SetUnlocked(bool unlocked) { if (_instance != null) _instance._isUnlocked = unlocked; }

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
            BossEvents.Defeated += OnBossDefeated;
            GameFlow.OnGameLoaded += OnGameLoaded;
        }

        private void OnDisable()
        {
            BossEvents.Defeated -= OnBossDefeated;
            GameFlow.OnGameLoaded -= OnGameLoaded;
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private void OnBossDefeated(string bossId)
        {
            // Unlock fast travel after Z2 boss (Sentinel-01 is Z1; first Z2 boss TBD by Codex)
            if (bossId.Contains("Z2"))
                SetUnlocked(true);
        }

        private void OnGameLoaded(SaveData data)
        {
            _isUnlocked = data != null && data.bossDefeated.Count > 1;
        }

        public static void TravelToStation(string stationId)
        {
            if (!IsUnlocked)
            {
                Debug.LogWarning("Fast travel not unlocked yet");
                return;
            }

            OnFastTravelRequested?.Invoke(stationId);

            // Find and move player to station
            var station = FindStationById(stationId);
            if (station == null)
            {
                Debug.LogError($"Station {stationId} not found");
                return;
            }

            var player = FindObjectOfType<PlayerController>();
            if (player == null) return;

            var targetPos = station.transform.position;
            player.GetComponent<Transform>().position = targetPos + Vector3.up * 0.5f;

            // Update checkpoint
            var session = GameSession.Current;
            if (session != null)
            {
                session.checkpointId = stationId;
                session.checkpointX = targetPos.x;
                session.checkpointY = targetPos.y;
                SaveService.Save(session, GameSession.ActiveSlot);
            }

            // Unpause if map was open
            GameFlow.IsPaused = false;
        }

        private static ChronoStation FindStationById(string stationId)
        {
            var stations = FindObjectsOfType<ChronoStation>();
            foreach (var station in stations)
                if (station.StationId == stationId)
                    return station;
            return null;
        }
    }
}
