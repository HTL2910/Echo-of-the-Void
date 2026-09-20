using UnityEngine;
using EchoOfTheVoid.Core;
using EchoOfTheVoid.Combat;

namespace EchoOfTheVoid.Enemies
{
    public class TrainingDummy : EnemyBase
    {
        [Header("Dummy Settings")]
        [SerializeField] private bool canDie = false;

        protected override void Awake()
        {
            base.Awake();
            currentHealth = 999;
        }

        public override HitFeedback TakeDamage(DamageInfo info)
        {
            var feedback = base.TakeDamage(info);
            if (!canDie && currentHealth <= 0)
            {
                currentHealth = 999;
                isDead = false;
                if (col != null) col.enabled = true;
            }
            return feedback;
        }
    }
}
