using UnityEngine;
using EchoOfTheVoid.Environment;

namespace EchoOfTheVoid.Core
{
    public enum ShiftResult { Switched, OnCooldown, Blocked }

    public class RealityManager : MonoBehaviour
    {
        /// <summary>Occupant bounds are shrunk by this much so merely standing on/against a platform never blocks a shift.</summary>
        private const float OverlapSkin = 0.06f;

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

        /// <summary>
        /// Toggle the realm for an occupant (the player) that must not end up inside solid geometry (spec 2.2).
        /// A blocked shift costs no cooldown and raises <see cref="RealityEventBus.OnShiftDenied"/>.
        /// </summary>
        public ShiftResult TryToggleRealm(Bounds occupant)
        {
            if (!CanSwitchRealm()) return ShiftResult.OnCooldown;

            RealmType target = (currentRealm == RealmType.Prime) ? RealmType.Echo : RealmType.Prime;

            Bounds inner = occupant;
            inner.Expand(-OverlapSkin * 2f);
            if (RealityObstacles.AnySolidOverlap(target, inner))
            {
                RealityEventBus.TriggerShiftDenied();
                return ShiftResult.Blocked;
            }

            SwitchRealm(target);
            return ShiftResult.Switched;
        }

        public void ToggleRealm()
        {
            if (!CanSwitchRealm()) return;

            RealmType target = (currentRealm == RealmType.Prime) ? RealmType.Echo : RealmType.Prime;
            SwitchRealm(target);
        }
    }
}
