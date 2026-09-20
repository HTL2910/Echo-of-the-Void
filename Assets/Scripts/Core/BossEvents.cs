using System;

namespace EchoOfTheVoid.Core
{
    /// <summary>
    /// Boss fight notifications. Boss code raises them, the HUD (boss health bar), music and camera listen.
    /// Ids are stable strings such as "sentinel_01", "keeper_myra", "doppelganger", "rift_knight_prime", "chronos".
    /// </summary>
    public static class BossEvents
    {
        public static event Action<string, string, int> Engaged;        // id, display name, max HP
        public static event Action<string, int, int> HealthChanged;     // id, current, max
        public static event Action<string> Defeated;                    // id
        public static event Action<string> Reset;                       // id (player died mid-fight, boss returns to start)

        public static void RaiseEngaged(string id, string displayName, int maxHealth) => Engaged?.Invoke(id, displayName, maxHealth);
        public static void RaiseHealthChanged(string id, int current, int max) => HealthChanged?.Invoke(id, current, max);
        public static void RaiseDefeated(string id) => Defeated?.Invoke(id);
        public static void RaiseReset(string id) => Reset?.Invoke(id);
    }
}
