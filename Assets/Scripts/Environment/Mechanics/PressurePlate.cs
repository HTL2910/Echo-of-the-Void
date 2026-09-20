using System;
using System.Collections;
using UnityEngine;

namespace EchoOfTheVoid.Environment.Mechanics
{
    /// <summary>
    /// K3: Pressure Plate (spec §4 Z4).
    /// Activates when Kael (tag "Player") or Echo Anchor (layer "Anchor") presses it.
    /// Fires Pressed / Released events for Door to listen.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class PressurePlate : MonoBehaviour
    {
        public event Action Pressed;
        public event Action Released;

        public bool IsPressed { get; private set; }

        private int _occupantCount;

        private void Awake()
        {
            var col = GetComponent<Collider2D>();
            col.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsValidActivator(other)) return;
            _occupantCount++;
            if (!IsPressed)
            {
                IsPressed = true;
                Pressed?.Invoke();
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!IsValidActivator(other)) return;
            _occupantCount = Mathf.Max(0, _occupantCount - 1);
            if (_occupantCount == 0 && IsPressed)
            {
                IsPressed = false;
                Released?.Invoke();
            }
        }

        private static bool IsValidActivator(Collider2D col)
        {
            // Player tag or Anchor layer
            if (col.CompareTag("Player")) return true;
            int anchorLayer = LayerMask.NameToLayer("Anchor");
            if (anchorLayer != -1 && col.gameObject.layer == anchorLayer) return true;
            return false;
        }
    }
}
