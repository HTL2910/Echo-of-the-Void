using UnityEngine;
using UnityEngine.UI;
using EchoOfTheVoid.Core;

namespace EchoOfTheVoid.UI.Map
{
    /// <summary>
    /// Fullscreen blueprint map view (toggle with M key).
    /// Shows all discovered rooms, locked doors with ability icons, Chrono Stations.
    /// Supports pan (WASD/drag) and zoom (scroll/triggers).
    /// Pauses game while open.
    /// </summary>
    public class FullscreenMapView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform mapContent;
        [SerializeField] private MapDefinitionData mapDefinition;
        [SerializeField] private float zoomMin = 0.5f;
        [SerializeField] private float zoomMax = 3f;
        [SerializeField] private KeyCode toggleKey = KeyCode.M;

        private bool _isOpen;
        private float _zoom = 1f;
        private Vector2 _panOffset;

        private void OnEnable()
        {
            GameFlow.OnGameLoaded += OnGameLoaded;
        }

        private void OnDisable()
        {
            GameFlow.OnGameLoaded -= OnGameLoaded;
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
                Toggle();

            if (_isOpen)
            {
                HandlePan();
                HandleZoom();
            }
        }

        public void Toggle()
        {
            _isOpen = !_isOpen;
            canvasGroup.alpha = _isOpen ? 1f : 0f;
            canvasGroup.blocksRaycasts = _isOpen;

            GameFlow.IsPaused = _isOpen;

            if (_isOpen)
                Refresh();
        }

        private void HandlePan()
        {
            var pan = Vector2.zero;
            if (Input.GetKey(KeyCode.W)) pan.y += 10f;
            if (Input.GetKey(KeyCode.S)) pan.y -= 10f;
            if (Input.GetKey(KeyCode.A)) pan.x -= 10f;
            if (Input.GetKey(KeyCode.D)) pan.x += 10f;

            _panOffset += pan;
            if (mapContent != null)
                mapContent.anchoredPosition = _panOffset;
        }

        private void HandleZoom()
        {
            var scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                _zoom = Mathf.Clamp(_zoom + scroll, zoomMin, zoomMax);
                if (mapContent != null)
                    mapContent.localScale = Vector3.one * _zoom;
            }
        }

        private void Refresh()
        {
            // To be implemented: render all rooms with their states
            // - Discovered rooms: white/visible
            // - Undiscovered doors: ? (blinking)
            // - Locked doors: icon for required ability
            // - Chrono Stations: marker
            // - Monoliths: marker
            // - Bosses: marker
            if (mapContent != null)
                mapContent.gameObject.SetActive(true);
        }

        private void OnGameLoaded(SaveData data)
        {
            _isOpen = false;
            canvasGroup.alpha = 0f;
            _zoom = 1f;
            _panOffset = Vector2.zero;
        }
    }
}
