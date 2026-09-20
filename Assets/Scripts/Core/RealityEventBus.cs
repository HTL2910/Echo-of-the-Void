using System;

namespace EchoOfTheVoid.Core
{
    public static class RealityEventBus
    {
        public static event Action<RealmType> OnRealmSwitched;

        /// <summary>Raised when a Reality Shift is refused because it would trap the player inside solid geometry.</summary>
        public static event Action OnShiftDenied;

        public static void TriggerRealmSwitch(RealmType targetRealm)
        {
            OnRealmSwitched?.Invoke(targetRealm);
        }

        public static void TriggerShiftDenied()
        {
            OnShiftDenied?.Invoke();
        }
    }
}
