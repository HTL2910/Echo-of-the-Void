using UnityEngine;
using UnityEngine.UI;
using EchoOfTheVoid.Core;
using EchoOfTheVoid.Environment;
using EchoOfTheVoid.Player;
using EchoOfTheVoid.Save;

namespace EchoOfTheVoid.UI.Map
{
    /// <summary>
    /// Small 5x5 or 7x7 minimap displayed in HUD corner.
    /// Shows nearby rooms, doors, and Kael's position.
    /// Updated only on room change (zero allocation per frame).
    /// </summary>
    public class MinimapHUD : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image mapImage;
        [SerializeField] private RectTransform mapContainer;
        [SerializeField] private int windowSize = 5; // 5x5 or 7x7
        [SerializeField] private float cellSize = 16f;
        [SerializeField] private MapDefinitionData mapDefinition;

        private RenderTexture _minimapTexture;
        private int _lastRoomZone = -1;
        private Vector2Int _lastRoomPos;

        private void OnEnable()
        {
            MapManager.OnRoomDiscovered += OnRoomDiscovered;
            GameFlow.OnGameLoaded += OnGameLoaded;
        }

        private void OnDisable()
        {
            MapManager.OnRoomDiscovered -= OnRoomDiscovered;
            GameFlow.OnGameLoaded -= OnGameLoaded;
            if (_minimapTexture != null)
                Destroy(_minimapTexture);
        }

        private void Start()
        {
            if (mapImage != null)
                _minimapTexture = new RenderTexture((int)(windowSize * cellSize), (int)(windowSize * cellSize), 0);
        }

        private void OnGameLoaded(SaveData data)
        {
            Refresh();
        }

        private void OnRoomDiscovered(string roomId)
        {
            Refresh();
        }

        private void Refresh()
        {
            var player = FindObjectOfType<PlayerController>();
            if (player == null || mapDefinition == null) return;

            // Find current room
            var currentRoom = RoomManager.Instance != null ? RoomManager.Instance.CurrentRoom : null;
            if (currentRoom == null) return;

            // Only redraw if room changed
            if (currentRoom.RoomId == GetCurrentRoomId()) return;

            RedrawMinimap();
        }

        private void RedrawMinimap()
        {
            // To be implemented: render 5x5/7x7 grid with room states
            // For now, this is a placeholder
            if (mapImage != null)
                mapImage.enabled = true;
        }

        private string GetCurrentRoomId()
        {
            var player = FindObjectOfType<PlayerController>();
            if (player == null) return "";
            var room = RoomManager.Instance != null ? RoomManager.Instance.CurrentRoom : null;
            return room != null ? room.RoomId : "";
        }
    }
}
