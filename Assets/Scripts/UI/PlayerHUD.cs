using UnityEngine;
using UnityEngine.UI;
using EchoOfTheVoid.Core;
using EchoOfTheVoid.Player;

namespace EchoOfTheVoid.UI
{
    public class PlayerHUD : MonoBehaviour
    {
        [Header("UI Element References")]
        [SerializeField] private Image healthBarFill;
        [SerializeField] private Image energyBarFill;
        [SerializeField] private Image dashCooldownFill;
        [SerializeField] private Text realmText;
        [SerializeField] private Image realmBadge;

        private PlayerStats _playerStats;
        private PlayerController _playerController;

        private void Start()
        {
            FindPlayerAndBind();
            RealityEventBus.OnRealmSwitched += UpdateRealmIndicator;

            if (RealityManager.Instance != null)
            {
                UpdateRealmIndicator(RealityManager.Instance.CurrentRealm);
            }
        }

        private void OnDestroy()
        {
            if (_playerStats != null)
            {
                _playerStats.OnHealthChanged -= UpdateHealthBar;
                _playerStats.OnEnergyChanged -= UpdateEnergyBar;
            }
            RealityEventBus.OnRealmSwitched -= UpdateRealmIndicator;
        }

        private void Update()
        {
            if (_playerController == null)
            {
                FindPlayerAndBind();
            }

            if (_playerController != null && dashCooldownFill != null)
            {
                float timer = _playerController.DashCooldownTimer;
                dashCooldownFill.fillAmount = Mathf.Clamp01(timer / 0.8f);
            }
        }

        private void FindPlayerAndBind()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                _playerStats = player.GetComponent<PlayerStats>();
                _playerController = player.GetComponent<PlayerController>();

                if (_playerStats != null)
                {
                    _playerStats.OnHealthChanged += UpdateHealthBar;
                    _playerStats.OnEnergyChanged += UpdateEnergyBar;

                    UpdateHealthBar(_playerStats.CurrentHealth, _playerStats.MaxHealth);
                    UpdateEnergyBar(_playerStats.CurrentEnergy, _playerStats.MaxEnergy);
                }
            }
        }

        private void UpdateHealthBar(int current, int max)
        {
            if (healthBarFill != null && max > 0)
            {
                healthBarFill.fillAmount = (float)current / max;
            }
        }

        private void UpdateEnergyBar(float current, int max)
        {
            if (energyBarFill != null && max > 0)
            {
                energyBarFill.fillAmount = current / max;
            }
        }

        private void UpdateRealmIndicator(RealmType newRealm)
        {
            Color primeColor = new Color(0.0f, 0.85f, 1.0f, 1f);  // Cyan
            Color echoColor = new Color(0.85f, 0.25f, 1.0f, 1f); // Magenta / Purple

            if (realmText != null)
            {
                realmText.text = $"REALM: {newRealm.ToString().ToUpper()}";
                realmText.color = (newRealm == RealmType.Prime) ? primeColor : echoColor;
            }

            if (realmBadge != null)
            {
                realmBadge.color = (newRealm == RealmType.Prime) ? primeColor : echoColor;
            }
        }

        public void BindElements(Image hpFill, Image ceFill, Image dashFill, Text text, Image badge)
        {
            healthBarFill = hpFill;
            energyBarFill = ceFill;
            dashCooldownFill = dashFill;
            realmText = text;
            realmBadge = badge;
        }
    }
}
