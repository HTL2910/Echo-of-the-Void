using System;
using System.Collections.Generic;
using UnityEngine;
using EchoOfTheVoid.Core;

namespace EchoOfTheVoid.Enemies
{
    /// <summary>
    /// Feeds enemy gameplay state into an Animator according to the contract in Docs/phan_cong_code_va_noi_dung.md.
    /// Missing parameters or missing Animator are silently and safely ignored.
    /// </summary>
    public class EnemyAnimationDriver : MonoBehaviour
    {
        public const string Speed = "Speed";
        public const string IsGrounded = "IsGrounded";
        public const string Realm = "Realm";
        public const string TriggerAlert = "Alert";
        public const string TriggerCharge = "Charge";
        public const string TriggerAttack = "Attack";
        public const string TriggerHurt = "Hurt";
        public const string TriggerDie = "Die";

        private EnemyBase _enemy;
        private Animator _animator;
        private readonly Dictionary<string, AnimatorControllerParameterType> _parameters = new();

        public Animator AnimatorComponent => _animator;

        private void Awake()
        {
            _enemy = GetComponent<EnemyBase>();
            RefreshAnimator();
        }

        private void OnEnable()
        {
            if (_enemy != null)
            {
                _enemy.OnDamaged += HandleDamaged;
                _enemy.OnDeath += HandleDeath;
            }
            RealityEventBus.OnRealmSwitched += HandleRealmSwitched;
        }

        private void OnDisable()
        {
            if (_enemy != null)
            {
                _enemy.OnDamaged -= HandleDamaged;
                _enemy.OnDeath -= HandleDeath;
            }
            RealityEventBus.OnRealmSwitched -= HandleRealmSwitched;
        }

        public void RefreshAnimator()
        {
            _parameters.Clear();
            _animator = GetComponentInChildren<Animator>();
            if (_animator == null || _animator.runtimeAnimatorController == null) return;

            foreach (var p in _animator.parameters)
            {
                _parameters[p.name] = p.type;
            }

            if (_enemy != null)
            {
                SetInt(Realm, (int)_enemy.EntityRealm);
            }
        }

        public void SetMovement(float speed, bool isGrounded)
        {
            SetFloat(Speed, Mathf.Abs(speed));
            SetBool(IsGrounded, isGrounded);
        }

        public void TriggerEnemyAlert() => Trigger(TriggerAlert);
        public void TriggerEnemyCharge() => Trigger(TriggerCharge);
        public void TriggerEnemyAttack() => Trigger(TriggerAttack);
        public void TriggerEnemyHurt() => Trigger(TriggerHurt);
        public void TriggerEnemyDie() => Trigger(TriggerDie);

        private void HandleDamaged(bool isDeflected, int damage)
        {
            if (!isDeflected)
            {
                TriggerEnemyHurt();
            }
        }

        private void HandleDeath()
        {
            TriggerEnemyDie();
        }

        private void HandleRealmSwitched(RealmType realm)
        {
            if (_enemy != null)
            {
                SetInt(Realm, (int)_enemy.EntityRealm);
            }
        }

        private bool Has(string name, AnimatorControllerParameterType type)
            => _parameters.TryGetValue(name, out var t) && t == type;

        private void SetFloat(string name, float value)
        {
            if (_animator != null && Has(name, AnimatorControllerParameterType.Float))
            {
                _animator.SetFloat(name, value);
            }
        }

        private void SetBool(string name, bool value)
        {
            if (_animator != null && Has(name, AnimatorControllerParameterType.Bool))
            {
                _animator.SetBool(name, value);
            }
        }

        private void SetInt(string name, int value)
        {
            if (_animator != null && Has(name, AnimatorControllerParameterType.Int))
            {
                _animator.SetInteger(name, value);
            }
        }

        private void Trigger(string name)
        {
            if (_animator != null && Has(name, AnimatorControllerParameterType.Trigger))
            {
                _animator.SetTrigger(name);
            }
        }
    }
}
