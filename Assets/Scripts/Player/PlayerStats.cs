using System;
using System.Collections;
using UnityEngine;
using EchoOfTheVoid.Core;
using EchoOfTheVoid.Combat;
using EchoOfTheVoid.Feedback;
using EchoOfTheVoid.UI;

namespace EchoOfTheVoid.Player
{
    public class PlayerStats : MonoBehaviour, IDamageable
    {
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
            _renderer = GetComponentInChildren<SpriteRenderer>();
            _currentHealth = maxHealth;
            _currentEnergy = maxEnergy;
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
            if (_isInvulnerable || _currentHealth <= 0)
            {
                return new HitFeedback(isDeflected: true, dealtDamage: 0, isDead: false);
            }

            int finalDamage = info.Amount;
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
                OnPlayerDeath?.Invoke();
                return new HitFeedback(isDeflected: false, dealtDamage: finalDamage, isDead: true);
            }

            StartCoroutine(IFrameRoutine());
            return new HitFeedback(isDeflected: false, dealtDamage: finalDamage, isDead: false);
        }

        private IEnumerator IFrameRoutine()
        {
            _isInvulnerable = true;
            float elapsed = 0f;
            float blinkInterval = 0.08f;

            while (elapsed < iFrameDuration)
            {
                if (_renderer != null)
                {
                    _renderer.color = new Color(1f, 0.4f, 0.4f, 0.4f);
                }
                yield return new WaitForSeconds(blinkInterval);
                if (_renderer != null)
                {
                    _renderer.color = Color.white;
                }
                yield return new WaitForSeconds(blinkInterval);
                elapsed += blinkInterval * 2f;
            }

            if (_renderer != null) _renderer.color = Color.white;
            _isInvulnerable = false;
        }
    }
}
