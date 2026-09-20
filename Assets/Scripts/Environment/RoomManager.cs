using System;
using UnityEngine;
using EchoOfTheVoid.Core;
using EchoOfTheVoid.Feedback;
using EchoOfTheVoid.Player;

namespace EchoOfTheVoid.Environment
{
    /// <summary>
    /// Tracks which room Kael is in. Entering a new room confines the camera to it, flashes the screen (0.15 s),
    /// freezes Kael's input for a moment and records the room as visited (for the map and the save).
    /// </summary>
    public class RoomManager : MonoBehaviour
    {
        public static RoomManager Instance { get; private set; }

        [SerializeField] private float transitionFlashSeconds = 0.15f;
        [SerializeField] private float inputLockSeconds = 0.1f;

        private Transform _player;
        private CameraFollow2D _camera;

        public RoomBounds CurrentRoom { get; private set; }

        /// <summary>previous room id (null on the first room), new room id.</summary>
        public static event Action<string, string> RoomEntered;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (_player == null)
            {
                var go = GameObject.FindGameObjectWithTag("Player");
                if (go == null) return;
                _player = go.transform;
            }

            Vector2 position = _player.position;
            if (CurrentRoom != null && CurrentRoom.isActiveAndEnabled && CurrentRoom.Contains(position)) return;

            foreach (var room in RoomBounds.All)
            {
                if (room != CurrentRoom && room.Contains(position))
                {
                    Enter(room);
                    return;
                }
            }
        }

        private void Enter(RoomBounds room)
        {
            string previousId = CurrentRoom != null ? CurrentRoom.RoomId : null;
            bool firstRoom = CurrentRoom == null;
            CurrentRoom = room;

            if (_camera == null) _camera = FindFirstObjectByType<CameraFollow2D>();
            if (_camera != null)
            {
                Bounds b = room.WorldBounds;
                _camera.SetBounds(b.min, b.max);
                _camera.SnapToTarget(); // cut to the new room, never glide across the wall
            }

            if (!GameSession.Current.visitedRooms.Contains(room.RoomId)) GameSession.Current.visitedRooms.Add(room.RoomId);

            if (!firstRoom)
            {
                ScreenFader.Flash(transitionFlashSeconds);
                if (_player != null)
                {
                    var controller = _player.GetComponent<PlayerController>();
                    if (controller != null) controller.LockInput(inputLockSeconds);
                }
            }

            RoomEntered?.Invoke(previousId, room.RoomId);
        }
    }
}
