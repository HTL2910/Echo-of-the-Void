using System;
using System.Collections;
using UnityEngine;

namespace EchoOfTheVoid.Environment.Mechanics
{
    /// <summary>
    /// K3: Door that opens when a paired PressurePlate is held, closes after closeDelay (spec §4, Z4 uses 1.5s).
    /// Exposes Opened/Closed events for additional triggers (e.g., audio).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Door : MonoBehaviour
    {
        [SerializeField] private PressurePlate linkedPlate;
        [SerializeField] private float closeDelay = 1.5f;
        [SerializeField] private float openSpeed = 8f;

        public event Action Opened;
        public event Action Closed;

        public bool IsOpen { get; private set; }

        private Collider2D _col;
        private SpriteRenderer _sprite;
        private Vector3 _closedPosition;
        private Vector3 _openPosition;
        private Coroutine _closeRoutine;

        private void Awake()
        {
            _col = GetComponent<Collider2D>();
            _sprite = GetComponentInChildren<SpriteRenderer>();
            _closedPosition = transform.position;
            // Open by sliding upward 2 units (adjust per prefab needs)
            _openPosition = _closedPosition + Vector3.up * 2f;
        }

        private void Start()
        {
            if (linkedPlate != null)
            {
                linkedPlate.Pressed  += OnPlatePressed;
                linkedPlate.Released += OnPlateReleased;
            }
        }

        private void OnDestroy()
        {
            if (linkedPlate != null)
            {
                linkedPlate.Pressed  -= OnPlatePressed;
                linkedPlate.Released -= OnPlateReleased;
            }
        }

        private void OnPlatePressed()
        {
            if (_closeRoutine != null)
            {
                StopCoroutine(_closeRoutine);
                _closeRoutine = null;
            }
            StartCoroutine(SlideRoutine(_openPosition));
            if (!IsOpen)
            {
                IsOpen = true;
                Opened?.Invoke();
            }
        }

        private void OnPlateReleased()
        {
            _closeRoutine = StartCoroutine(CloseAfterDelay());
        }

        private IEnumerator CloseAfterDelay()
        {
            yield return new WaitForSeconds(closeDelay);
            yield return SlideRoutine(_closedPosition);
            if (IsOpen)
            {
                IsOpen = false;
                Closed?.Invoke();
            }
        }

        private IEnumerator SlideRoutine(Vector3 target)
        {
            while (Vector3.Distance(transform.position, target) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(transform.position, target, openSpeed * Time.deltaTime);
                yield return null;
            }
            transform.position = target;

            // Collider active when door is closed
            if (_col != null) _col.enabled = (transform.position == _closedPosition);
        }

        /// <summary>External link plate if not set in Inspector.</summary>
        public void LinkPlate(PressurePlate plate)
        {
            if (linkedPlate == plate) return;
            if (linkedPlate != null)
            {
                linkedPlate.Pressed  -= OnPlatePressed;
                linkedPlate.Released -= OnPlateReleased;
            }
            linkedPlate = plate;
            if (linkedPlate != null)
            {
                linkedPlate.Pressed  += OnPlatePressed;
                linkedPlate.Released += OnPlateReleased;
            }
        }
    }
}
