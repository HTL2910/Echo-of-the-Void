using UnityEngine;
using UnityEngine.UI;
using EchoOfTheVoid.Core;
using EchoOfTheVoid.Player;

namespace EchoOfTheVoid.UI
{
    public class PlayerHUD : MonoBehaviour
    {
        public static PlayerHUD Instance { get; private set; }

        [Header("UI Element References")]
        [SerializeField] private Image healthBarFill;
        [SerializeField] private Image energyBarFill;
        [SerializeField] private Image dashCooldownFill;
        [SerializeField] private Text realmText;
        [SerializeField] private Image realmBadge;
        [SerializeField] private Text announcementText;
        [SerializeField] private Text hpValueText;
        [SerializeField] private Text ceValueText;
        [SerializeField] private Image iconDash;
        [SerializeField] private Image iconWallJump;
        [SerializeField] private Image iconResonance;
        [SerializeField] private Image iconEchoAnchor;

        private PlayerStats _playerStats;
        private PlayerController _playerController;
        private AbilitySet _abilitySet;
        private Coroutine _announcementRoutine;

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
            FindPlayerAndBind();
            RealityEventBus.OnRealmSwitched += UpdateRealmIndicator;
            RealityEventBus.OnShiftDenied += ShowShiftDenied;

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
            if (_abilitySet != null)
            {
                _abilitySet.OnAbilityUnlocked -= HandleAbilityUnlocked;
            }
            RealityEventBus.OnRealmSwitched -= UpdateRealmIndicator;
            RealityEventBus.OnShiftDenied -= ShowShiftDenied;
        }

        private void ShowShiftDenied()
        {
            ShowAnnouncement("SHIFT BLOCKED", 0.8f);
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
                _abilitySet = player.GetComponent<AbilitySet>();

                if (_playerStats != null)
                {
                    _playerStats.OnHealthChanged += UpdateHealthBar;
                    _playerStats.OnEnergyChanged += UpdateEnergyBar;

                    UpdateHealthBar(_playerStats.CurrentHealth, _playerStats.MaxHealth);
                    UpdateEnergyBar(_playerStats.CurrentEnergy, _playerStats.MaxEnergy);
                }

                if (_abilitySet != null)
                {
                    _abilitySet.OnAbilityUnlocked += HandleAbilityUnlocked;
                    UpdateAbilityIcons();
                }
            }
        }

        private void HandleAbilityUnlocked(AbilityFlags ability)
        {
            UpdateAbilityIcons();
        }

        private void UpdateAbilityIcons()
        {
            if (_abilitySet == null) return;

            SetIconState(iconWallJump, _abilitySet.Has(AbilityFlags.WallJump));
            SetIconState(iconResonance, _abilitySet.Has(AbilityFlags.ResonanceStrike));
            SetIconState(iconEchoAnchor, _abilitySet.Has(AbilityFlags.EchoAnchor));
            SetIconState(iconDash, _abilitySet.Has(AbilityFlags.PhaseDash));
        }

        private void SetIconState(Image img, bool unlocked)
        {
            if (img == null) return;
            img.color = unlocked ? Color.white : new Color(0.4f, 0.4f, 0.5f, 0.35f);
        }

        private void UpdateHealthBar(int current, int max)
        {
            if (healthBarFill != null && max > 0)
            {
                healthBarFill.fillAmount = (float)current / max;
            }
            if (hpValueText != null)
            {
                hpValueText.text = $"{current} / {max}";
            }
        }

        private void UpdateEnergyBar(float current, int max)
        {
            if (energyBarFill != null && max > 0)
            {
                energyBarFill.fillAmount = current / max;
            }
            if (ceValueText != null)
            {
                ceValueText.text = $"{(int)current} / {max}";
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

        public void ShowAnnouncement(string message, float duration = 2f)
        {
            if (announcementText == null) return;
            if (_announcementRoutine != null) StopCoroutine(_announcementRoutine);
            _announcementRoutine = StartCoroutine(AnnouncementCoroutine(message, duration));
        }

        private System.Collections.IEnumerator AnnouncementCoroutine(string message, float duration)
        {
            announcementText.text = message;
            announcementText.gameObject.SetActive(true);
            yield return new WaitForSeconds(duration);
            announcementText.gameObject.SetActive(false);
            _announcementRoutine = null;
        }

        public void BindElements(Image hpFill, Image ceFill, Image dashFill, Text text, Image badge, Text announcement = null,
            Text hpVal = null, Text ceVal = null, Image dashIcon = null, Image wallJump = null, Image resonance = null, Image anchor = null)
        {
            healthBarFill = hpFill;
            energyBarFill = ceFill;
            dashCooldownFill = dashFill;
            realmText = text;
            realmBadge = badge;
            announcementText = announcement;
            hpValueText = hpVal;
            ceValueText = ceVal;
            iconDash = dashIcon;
            iconWallJump = wallJump;
            iconResonance = resonance;
            iconEchoAnchor = anchor;
            if (announcementText != null) announcementText.gameObject.SetActive(false);
            UpdateAbilityIcons();
        }
    }
}
