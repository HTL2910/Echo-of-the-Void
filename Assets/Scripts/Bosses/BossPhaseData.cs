using UnityEngine;
using EchoOfTheVoid.Core;

namespace EchoOfTheVoid.Bosses
{
    /// <summary>
    /// K4: Reusable ScriptableObject defining a single boss phase's patterns and thresholds.
    /// One SO per phase; BossBase reads an ordered list of these.
    /// </summary>
    [CreateAssetMenu(fileName = "NewBossPhase", menuName = "Echo of the Void/Boss Phase Data")]
    public class BossPhaseData : ScriptableObject
    {
        [Header("Phase Identity")]
        public string PhaseName = "Phase 1";

        [Header("Health Threshold")]
        [Tooltip("This phase activates when boss HP falls below this fraction (0-1). Phase 0 starts at 1.")]
        [Range(0f, 1f)]
        public float HpThreshold = 1f;

        [Header("Attack Timings")]
        [Tooltip("Warning time before MissileRain circles appear until missiles fire.")]
        public float MissileWarningDuration = 1.2f;
        [Tooltip("Interval between SweepKick repeats.")]
        public float SweepKickInterval = 3.5f;
        [Tooltip("Interval between LaserSweep attacks.")]
        public float LaserSweepInterval = 5.0f;

        [Header("Damage Values")]
        public int SweepKickDamage  = 18;
        public int MissileImpactDamage = 22;
        public int LaserDamagePerSecond = 12;

        [Header("Missile Rain")]
        public int MissileCount = 4;
    }
}
