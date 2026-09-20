using UnityEngine;
using EchoOfTheVoid.Core;

namespace EchoOfTheVoid.Enemies
{
    [CreateAssetMenu(fileName = "NewEnemyData", menuName = "Echo of the Void/Enemy Data")]
    public class EnemyDataSO : ScriptableObject
    {
        [Header("General")]
        public string EnemyName = "Enemy";
        public RealmType Realm = RealmType.Prime;

        [Header("Stats")]
        public int MaxHealth = 50;
        public int ContactDamage = 10;
        public float MoveSpeed = 3.2f;
        public float AlertSpeed = 5.0f;
        public float DetectionRadius = 5.0f;
        public float PoiseMax = 50f;

        [Header("Patrol")]
        public float PatrolDistance = 6.0f;
    }
}
