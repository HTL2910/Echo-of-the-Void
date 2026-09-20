using System;
using UnityEngine;
using EchoOfTheVoid.Player;

namespace EchoOfTheVoid.Environment.Mechanics
{
    /// <summary>
    /// K3: Lever (spec §4). Toggled by pressing E within range.
    /// Exposes Toggled event; consumers link freely (lights, platforms, doors...).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Lever : MonoBehaviour
    {
        public event Action<bool> Toggled; // true = ON, false = OFF

        public bool IsOn { get; private set; }

        private PlayerController _playerInRange;
        private SpriteRenderer _sprite;

        private static readonly Color OnColor  = new Color(0.3f, 1f, 0.5f, 1f);
        private static readonly Color OffColor = new Color(0.7f, 0.3f, 0.2f, 1f);

        private void Awake()
        {
            var col = GetComponent<Collider2D>();
            col.isTrigger = true;
            _sprite = GetComponentInChildren<SpriteRenderer>();
            UpdateVisual();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var ctrl = other.GetComponentInParent<PlayerController>();
            if (ctrl != null) _playerInRange = ctrl;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            var ctrl = other.GetComponentInParent<PlayerController>();
            if (ctrl != null && ctrl == _playerInRange) _playerInRange = null;
        }

        private void Update()
        {
            if (_playerInRange != null && _playerInRange.InteractPressed)
                Toggle();
        }

        public void Toggle()
        {
            IsOn = !IsOn;
            UpdateVisual();
            Toggled?.Invoke(IsOn);
        }

        private void UpdateVisual()
        {
            if (_sprite != null)
                _sprite.color = IsOn ? OnColor : OffColor;
        }
    }
}
