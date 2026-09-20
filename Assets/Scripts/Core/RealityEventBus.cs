using System;

namespace EchoOfTheVoid.Core
{
    public static class RealityEventBus
    {
        public static event Action<RealmType> OnRealmSwitched;

        public static void TriggerRealmSwitch(RealmType targetRealm)
        {
            OnRealmSwitched?.Invoke(targetRealm);
        }
    }
}
