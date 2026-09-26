using UnityEngine;

namespace EchoOfTheVoid.Enemies
{
    [CreateAssetMenu(fileName = "RiftKnightData", menuName = "Echo of the Void/Rift Knight Data")]
    public class RiftKnightDataSO : EnemyDataSO
    {
        [Header("Realm Switching")]
        [Min(0.1f)] public float RealmSwitchInterval = 4f;
        [Min(1)] public int HitsBeforeRealmSwitch = 3;

        [Header("Attacks")]
        [Min(0)] public int SlashDamage = 25;
        [Min(0)] public int StompDamage = 35;
        [Min(0f)] public float ShockwaveHeight = 1.2f;
        [Min(0f)] public float SlashRange = 1.6f;
        [Min(0f)] public float StompRange = 4f;
        [Min(0f)] public float AttackCooldown = 1.5f;
        [Min(0f)] public float StompCooldown = 4f;
    }
}
