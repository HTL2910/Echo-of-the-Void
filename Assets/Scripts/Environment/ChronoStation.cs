using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using EchoOfTheVoid.Core;
using EchoOfTheVoid.Player;
using EchoOfTheVoid.Save;
using EchoOfTheVoid.UI;

namespace EchoOfTheVoid.Environment
{
    /// <summary>
    /// Save point (spec 3.5b): interact to set the respawn point, heal, refill energy and write the save file.
    /// The station's root sits at floor level; Kael respawns <see cref="respawnHeight"/> above it.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class ChronoStation : MonoBehaviour
    {
        [SerializeField] private string stationId = "station";
        [SerializeField] private int healAmount = 25;
        [SerializeField] private float respawnHeight = 0.9f;

        private PlayerController _playerInRange;

        /// <summary>Raised (with the station id) each time Kael rests. Regular enemies use it to respawn.</summary>
        public static event Action<string> AnyStationUsed;

        public string StationId => stationId;

        public void Configure(string id, int heal = 25)
        {
            stationId = id;
            healAmount = heal;
        }

        private void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var controller = other.GetComponentInParent<PlayerController>();
            if (controller == null) return;

            _playerInRange = controller;
            if (PlayerHUD.Instance != null) PlayerHUD.Instance.ShowAnnouncement("[E] CHRONO STATION", 1.5f);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            var controller = other.GetComponentInParent<PlayerController>();
            if (controller != null && controller == _playerInRange) _playerInRange = null;
        }

        private void Update()
        {
            if (_playerInRange != null && _playerInRange.InteractPressed) Activate(_playerInRange);
        }

        public void Activate(PlayerController player)
        {
            var respawn = player.GetComponent<PlayerRespawn>();
            var stats = player.GetComponent<PlayerStats>();
            if (respawn == null || stats == null || stats.IsDead) return;

            RealmType realm = RealityManager.Instance != null ? RealityManager.Instance.CurrentRealm : RealmType.Prime;
            Vector3 spawn = transform.position + Vector3.up * respawnHeight;

            respawn.SetCheckpoint(spawn, realm);
            stats.Heal(healAmount);
            stats.RestoreEnergy(stats.MaxEnergy);

            // Update the running session (keeps collected items and defeated bosses) instead of starting a blank save
            var data = GameSession.Current;
            data.sceneName = SceneManager.GetActiveScene().name;
            data.playtimeSeconds = Time.time;
            data.checkpointId = stationId;
            data.checkpointX = spawn.x;
            data.checkpointY = spawn.y;
            data.checkpointRealm = (int)realm;
            data.maxHealth = stats.MaxHealth;
            data.currentHealth = stats.CurrentHealth;
            data.abilityFlags = (int)(player.GetComponent<AbilitySet>()?.Flags ?? AbilityFlags.None);

            bool saved = SaveService.Save(data, GameSession.ActiveSlot);
            AudioManager.Play(SfxGroup.StationActivate);
            AnyStationUsed?.Invoke(stationId);

            if (PlayerHUD.Instance != null)
                PlayerHUD.Instance.ShowAnnouncement(saved ? "CHECKPOINT SAVED" : "SAVE FAILED", 2f);
        }
    }
}
