using UnityEngine;

namespace EchoOfTheVoid.Core
{
    public class RealityManager : MonoBehaviour
    {
        public static RealityManager Instance { get; private set; }

        [SerializeField] private RealmType currentRealm = RealmType.Prime;
        [SerializeField] private float switchCooldown = 0.25f;

        private float _lastSwitchTime = -10f;

        public RealmType CurrentRealm => currentRealm;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // Broadcast initial realm state so all listeners sync
            RealityEventBus.TriggerRealmSwitch(currentRealm);
        }

        public bool CanSwitchRealm()
        {
            return Time.time >= _lastSwitchTime + switchCooldown;
        }

        public void SwitchRealm(RealmType target)
        {
            if (!CanSwitchRealm()) return;

            currentRealm = target;
            _lastSwitchTime = Time.time;
            RealityEventBus.TriggerRealmSwitch(currentRealm);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayRealityShift();
        }

        public void ToggleRealm()
        {
            if (!CanSwitchRealm()) return;

            RealmType target = (currentRealm == RealmType.Prime) ? RealmType.Echo : RealmType.Prime;
            SwitchRealm(target);
        }
    }
}
