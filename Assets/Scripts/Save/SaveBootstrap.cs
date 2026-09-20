using UnityEngine;
using EchoOfTheVoid.Core;
using EchoOfTheVoid.Player;

namespace EchoOfTheVoid.Save
{
    /// <summary>
    /// Lives in every gameplay scene. When the player chose Continue, puts Kael where the save says:
    /// checkpoint position and realm, health, abilities. Does nothing for a new game.
    /// </summary>
    public class SaveBootstrap : MonoBehaviour
    {
        private void Start()
        {
            var data = GameSession.PendingLoad;
            if (data == null) return;

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) Apply(data, player);

            GameSession.ConsumePendingLoad();
        }

        public static void Apply(SaveData data, GameObject player)
        {
            var abilities = player.GetComponent<AbilitySet>();
            if (abilities != null) abilities.SetFlags((AbilityFlags)data.abilityFlags);

            var stats = player.GetComponent<PlayerStats>();
            if (stats != null) stats.RestoreProgress(data.currentHealth, data.maxHealth);

            RealmType realm = (RealmType)data.checkpointRealm;
            var respawn = player.GetComponent<PlayerRespawn>();
            Vector3 position = new Vector3(data.checkpointX, data.checkpointY, 0f);
            if (respawn != null) respawn.SetCheckpoint(position, realm);

            var body = player.GetComponent<Rigidbody2D>();
            player.transform.position = position;
            if (body != null) body.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();

            if (RealityManager.Instance != null && RealityManager.Instance.CurrentRealm != realm)
            {
                RealityManager.Instance.SwitchRealm(realm);
            }
        }
    }
}
