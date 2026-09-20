using System.Collections.Generic;
using UnityEngine;
using EchoOfTheVoid.Core;

namespace EchoOfTheVoid.Player
{
    /// <summary>
    /// Feeds gameplay state into an Animator so artists can build animation graphs without touching code.
    /// Parameter contract is documented in Docs/phan_cong_code_va_noi_dung.md (section 4).
    /// Missing parameters or a missing Animator are silently ignored.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerAnimationDriver : MonoBehaviour
    {
        // Parameter names (the contract with the animation side)
        public const string Speed = "Speed";
        public const string VelocityY = "VelocityY";
        public const string IsGrounded = "IsGrounded";
        public const string IsWallSliding = "IsWallSliding";
        public const string IsDashing = "IsDashing";
        public const string Realm = "Realm";
        public const string TriggerJump = "Jump";
        public const string TriggerDash = "Dash";
        public const string TriggerAttack1 = "Attack1";
        public const string TriggerAttack2 = "Attack2";
        public const string TriggerAttack3 = "Attack3";
        public const string TriggerAirAttack = "AirAttack";
        public const string TriggerResonance = "Resonance";
        public const string TriggerHurt = "Hurt";
        public const string TriggerDie = "Die";

        private PlayerController _controller;
        private PlayerCombat _combat;
        private PlayerStats _stats;
        private Animator _animator;
        private readonly Dictionary<string, AnimatorControllerParameterType> _parameters = new();
        private int _lastHealth = -1;

        private void Awake()
        {
            _controller = GetComponent<PlayerController>();
            _combat = GetComponent<PlayerCombat>();
            _stats = GetComponent<PlayerStats>();
            RefreshAnimator();
        }

        private void OnEnable()
        {
            _controller.Jumped += OnJumped;
            _controller.Dashed += OnDashed;
            if (_combat != null) _combat.OnAttackPerformed += OnAttack;
            if (_stats != null)
            {
                _stats.OnHealthChanged += OnHealthChanged;
                _stats.OnPlayerDeath += OnDeath;
            }
            RealityEventBus.OnRealmSwitched += OnRealmSwitched;
        }

        private void OnDisable()
        {
            _controller.Jumped -= OnJumped;
            _controller.Dashed -= OnDashed;
            if (_combat != null) _combat.OnAttackPerformed -= OnAttack;
            if (_stats != null)
            {
                _stats.OnHealthChanged -= OnHealthChanged;
                _stats.OnPlayerDeath -= OnDeath;
            }
            RealityEventBus.OnRealmSwitched -= OnRealmSwitched;
        }

        /// <summary>Call after swapping the Animator or its controller at runtime.</summary>
        public void RefreshAnimator()
        {
            _parameters.Clear();
            _animator = GetComponentInChildren<Animator>();
            if (_animator == null || _animator.runtimeAnimatorController == null) return;

            foreach (var p in _animator.parameters) _parameters[p.name] = p.type;
        }

        private void Update()
        {
            if (_animator == null) return;

            Vector2 velocity = _controller.LinearVelocity;
            SetFloat(Speed, Mathf.Abs(velocity.x));
            SetFloat(VelocityY, velocity.y);
            SetBool(IsGrounded, _controller.IsGrounded);
            SetBool(IsWallSliding, _controller.CurrentStateName == nameof(States.PlayerWallSlideState));
            SetBool(IsDashing, _controller.CurrentStateName == nameof(States.PlayerDashState));
        }

        private void OnJumped() => Trigger(TriggerJump);
        private void OnDashed() => Trigger(TriggerDash);

        private void OnAttack(int step)
        {
            switch (step)
            {
                case 1: Trigger(TriggerAttack1); break;
                case 2: Trigger(TriggerAttack2); break;
                case 3: Trigger(TriggerAttack3); break;
                case 4: Trigger(TriggerAirAttack); break;
                case 5: Trigger(TriggerResonance); break;
            }
        }

        private void OnHealthChanged(int current, int max)
        {
            if (_lastHealth >= 0 && current < _lastHealth && current > 0) Trigger(TriggerHurt);
            _lastHealth = current;
        }

        private void OnDeath() => Trigger(TriggerDie);

        private void OnRealmSwitched(RealmType realm) => SetInt(Realm, (int)realm);

        private bool Has(string name, AnimatorControllerParameterType type)
            => _parameters.TryGetValue(name, out var t) && t == type;

        private void SetFloat(string name, float value)
        {
            if (Has(name, AnimatorControllerParameterType.Float)) _animator.SetFloat(name, value);
        }

        private void SetBool(string name, bool value)
        {
            if (Has(name, AnimatorControllerParameterType.Bool)) _animator.SetBool(name, value);
        }

        private void SetInt(string name, int value)
        {
            if (_animator != null && Has(name, AnimatorControllerParameterType.Int)) _animator.SetInteger(name, value);
        }

        private void Trigger(string name)
        {
            if (_animator != null && Has(name, AnimatorControllerParameterType.Trigger)) _animator.SetTrigger(name);
        }
    }
}
