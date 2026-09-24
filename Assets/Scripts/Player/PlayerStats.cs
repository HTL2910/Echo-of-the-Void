using System;
using System.Collections;
using UnityEngine;
using EchoOfTheVoid.Settings;
using EchoOfTheVoid.Core;
using EchoOfTheVoid.Combat;
using EchoOfTheVoid.Feedback;
using EchoOfTheVoid.UI;

namespace EchoOfTheVoid.Player
{
    public class PlayerStats : MonoBehaviour, IDamageable
    {
        public static PlayerStats Instance { get; private set; }

        [Header("Survival (GDD)")]
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private int maxEnergy = 100;
        [SerializeField] private float energyRegenPerSecond = 15f;
        [SerializeField] private float iFrameDuration = 0.8f;

        private int _currentHealth;
        private float _currentEnergy;
        private bool _isInvulnerable;
        private SpriteRenderer _renderer;
        private Vector3 _spawnPosition;

        public int CurrentHealth => _currentHealth;
        public int MaxHealth => maxHealth;
        public float CurrentEnergy => _currentEnergy;
        public int MaxEnergy => maxEnergy;
        public bool IsInvulnerable => _isInvulnerable;

        public RealmType EntityRealm => (RealityManager.Instance != null) 
            ? RealityManager.Instance.CurrentRealm 
            : RealmType.Prime;

        public event Action<int, int> OnHealthChanged;
        public event Action<float, int> OnEnergyChanged;
        public event Action OnPlayerDeath;

        private void Awake()
        {
            Instance = this;
            _renderer = GetComponentInChildren<SpriteRenderer>();
            _currentHealth = maxHealth;
            _currentEnergy = maxEnergy;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            _spawnPosition = transform.position;
            OnHealthChanged?.Invoke(_currentHealth, maxHealth);
            OnEnergyChanged?.Invoke(_currentEnergy, maxEnergy);
        }

        private void Update()
        {
            // Passive CE regeneration when grounded
            var controller = GetComponent<PlayerController>();
            if (controller != null && controller.IsGrounded && _currentEnergy < maxEnergy)
            {
                ModifyEnergy(energyRegenPerSecond * Time.deltaTime);
            }
        }

        public bool ConsumeEnergy(int amount)
        {
            if (_currentEnergy >= amount)
            {
                ModifyEnergy(-amount);
                return true;
            }
            return false;
        }

        public void RestoreEnergy(float amount)
        {
            ModifyEnergy(amount);
        }

        private void ModifyEnergy(float delta)
        {
            _currentEnergy = Mathf.Clamp(_currentEnergy + delta, 0f, maxEnergy);
            OnEnergyChanged?.Invoke(_currentEnergy, maxEnergy);
        }

        public void SetInvulnerable(bool invulnerable)
        {
            _isInvulnerable = invulnerable;
        }

        public bool IsDead => _currentHealth <= 0;

        /// <summary>Loading a save: set max/current health without i-frames or feedback.</summary>
        public void RestoreProgress(int current, int max)
        {
            maxHealth = Mathf.Max(1, max);
            _currentHealth = Mathf.Clamp(current, 1, maxHealth);
            OnHealthChanged?.Invoke(_currentHealth, maxHealth);
        }

        public void Heal(int amount)
        {
            if (IsDead) return;
            _currentHealth = Mathf.Min(maxHealth, _currentHealth + amount);
            OnHealthChanged?.Invoke(_currentHealth, maxHealth);
        }

        public void Revive(int health)
        {
            StopAllCoroutines();
            _currentHealth = Mathf.Clamp(health, 1, maxHealth);
            _currentEnergy = maxEnergy;
            OnHealthChanged?.Invoke(_currentHealth, maxHealth);
            OnEnergyChanged?.Invoke(_currentEnergy, maxEnergy);
            if (_renderer != null) _renderer.color = Color.white;
            _isInvulnerable = false;
            StartCoroutine(IFrameRoutine());
        }

        public HitFeedback TakeDamage(DamageInfo info)
        {
            if (_isInvulnerable || _currentHealth <= 0 || info.Amount <= 0)
            {
                return new HitFeedback(isDeflected: true, dealtDamage: 0, isDead: false);
            }

            // Assist mode: reduced damage, never below 1 so hits still register
            int finalDamage = Mathf.Max(1, Mathf.RoundToInt(info.Amount * SettingsService.Current.damageTakenMultiplier));
            _currentHealth = Mathf.Max(0, _currentHealth - finalDamage);
            OnHealthChanged?.Invoke(_currentHealth, maxHealth);

            // Screen shake heavy
            if (CameraShakeManager.Instance != null)
            {
                CameraShakeManager.Instance.ShakeHeavy();
            }

            // Hit stop (GDD: Kael receives damage = 0.10s, 6 frames)
            if (HitStopManager.Instance != null)
            {
                HitStopManager.Instance.TriggerHitStop(0.10f, 0.05f);
            }

            if (_currentHealth <= 0)
            {
                AudioManager.Play(SfxGroup.Death);
                OnPlayerDeath?.Invoke();
                return new HitFeedback(isDeflected: false, dealtDamage: finalDamage, isDead: true);
            }

            AudioManager.Play(SfxGroup.Hurt);
            StartCoroutine(IFrameRoutine());
            return new HitFeedback(isDeflected: false, dealtDamage: finalDamage, isDead: false);
        }

        private IEnumerator IFrameRoutine()
        {
            _isInvulnerable = true;
            float blinkInterval = 0.08f;
            float duration = SettingsService.Current.extendedIFrames ? iFrameDuration * 1.5f : iFrameDuration;
            float endTime = Time.time + duration;
            bool dimmed = true;

            while (Time.time < endTime)
            {
                if (_renderer != null)
                    _renderer.color = dimmed ? new Color(1f, 0.4f, 0.4f, 0.4f) : Color.white;

                float remaining = endTime - Time.time;
                yield return new WaitForSeconds(Mathf.Min(blinkInterval, remaining));
                dimmed = !dimmed;
            }

            if (_renderer != null) _renderer.color = Color.white;
            _isInvulnerable = false;
        }
    }
}
