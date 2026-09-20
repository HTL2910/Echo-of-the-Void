using System;
using UnityEngine;
using EchoOfTheVoid.Core;

namespace EchoOfTheVoid.Player
{
    /// <summary>Which abilities Kael currently owns. Systems ask <see cref="Has"/> before performing a gated action.</summary>
    public class AbilitySet : MonoBehaviour
    {
        [SerializeField] private AbilityFlags unlocked = AbilityFlags.RealityShift | AbilityFlags.PhaseDash;

        public AbilityFlags Flags => unlocked;

        /// <summary>Raised once per newly acquired ability.</summary>
        public event Action<AbilityFlags> OnAbilityUnlocked;

        public bool Has(AbilityFlags ability) => (unlocked & ability) == ability;

        /// <returns>true if the ability was newly unlocked.</returns>
        public bool Unlock(AbilityFlags ability)
        {
            if (ability == AbilityFlags.None || Has(ability)) return false;

            unlocked |= ability;
            OnAbilityUnlocked?.Invoke(ability);
            return true;
        }

        /// <summary>Replace the whole set (loading a save). Does not raise unlock events.</summary>
        public void SetFlags(AbilityFlags flags)
        {
            unlocked = flags;
        }
    }
}
