using UnityEngine;
using EchoOfTheVoid.Core;

namespace EchoOfTheVoid.Combat
{
    public struct DamageInfo
    {
        public int Amount;
        public Vector2 HitPoint;
        public Vector2 Knockback;
        public RealmType AttackRealm;
        public bool IsResonance;
        public GameObject Attacker;

        public DamageInfo(int amount, Vector2 hitPoint, Vector2 knockback, RealmType attackRealm, bool isResonance = false, GameObject attacker = null)
        {
            Amount = amount;
            HitPoint = hitPoint;
            Knockback = knockback;
            AttackRealm = attackRealm;
            IsResonance = isResonance;
            Attacker = attacker;
        }
    }

    public struct HitFeedback
    {
        public bool IsDeflected;
        public int DealtDamage;
        public bool IsDead;

        public HitFeedback(bool isDeflected, int dealtDamage, bool isDead)
        {
            IsDeflected = isDeflected;
            DealtDamage = dealtDamage;
            IsDead = isDead;
        }
    }

    public interface IDamageable
    {
        RealmType EntityRealm { get; }
        HitFeedback TakeDamage(DamageInfo info);
    }
}
