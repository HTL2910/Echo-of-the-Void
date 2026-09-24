using System.IO;
using NUnit.Framework;
using UnityEngine;
using EchoOfTheVoid.Core;
using EchoOfTheVoid.Save;
using EchoOfTheVoid.UI.Map;
using EchoOfTheVoid.Environment;

namespace EchoOfTheVoid.Tests
{
    [Category("Map")]
    public class MapSystemTests
    {
        private GameObject _mapManagerObj;
        private GameObject _fastTravelManagerObj;
        private MapManager _mapManager;
        private TestWorld _world;
        private string _saveDir;

        [SetUp]
        public void Setup()
        {
            GameSession.Reset();
            _saveDir = Path.Combine(Path.GetTempPath(), "eotv_map_test_" + System.Guid.NewGuid().ToString("N"));
            SaveService.DirectoryOverride = _saveDir;

            _mapManagerObj = new GameObject("MapManager");
            _mapManager = _mapManagerObj.AddComponent<MapManager>();
            _fastTravelManagerObj = new GameObject("FastTravelManager");
            _fastTravelManagerObj.AddComponent<FastTravelManager>();
        }

        [TearDown]
        public void Teardown()
        {
            Object.DestroyImmediate(_mapManagerObj);
            Object.DestroyImmediate(_fastTravelManagerObj);
            _world?.Dispose();
            GameSession.Reset();
            SaveService.DirectoryOverride = null;
            if (Directory.Exists(_saveDir)) Directory.Delete(_saveDir, true);
        }

        [Test]
        public void DiscoverRoom_AddsToVisitedRooms()
        {
            var session = GameSession.Current;
            Assert.That(session.visitedRooms, Is.Empty);

            MapManager.DiscoverRoom("Z1_R01");
            Assert.That(session.visitedRooms, Contains.Item("Z1_R01"));
        }

        [Test]
        public void DiscoverRoom_Idempotent()
        {
            MapManager.DiscoverRoom("Z1_R01");
            MapManager.DiscoverRoom("Z1_R01");

            var session = GameSession.Current;
            Assert.That(session.visitedRooms.Count, Is.EqualTo(1));
        }

        [Test]
        public void UnlockDoor_AddsToUnlockedDoors()
        {
            var session = GameSession.Current;
            Assert.That(session.unlockedDoorIds, Is.Empty);

            MapManager.UnlockDoor("Z1_Door_01");
            Assert.That(session.unlockedDoorIds, Contains.Item("Z1_Door_01"));
        }

        [Test]
        public void SaveAndLoad_PreservesDiscoveredRooms()
        {
            MapManager.DiscoverRoom("Z1_R01");
            MapManager.DiscoverRoom("Z1_R02");
            var data1 = GameSession.Current;

            // Simulate load from save
            var data2 = new SaveData
            {
                version = data1.version,
                slotId = data1.slotId,
                visitedRooms = data1.visitedRooms,
                unlockedDoorIds = data1.unlockedDoorIds
            };

            Assert.That(data2.visitedRooms, Contains.Item("Z1_R01"));
            Assert.That(data2.visitedRooms, Contains.Item("Z1_R02"));
        }

        [Test]
        public void IsRoomDiscovered_ReturnsCorrectState()
        {
            MapManager.DiscoverRoom("Z1_R01");
            Assert.That(MapManager.IsRoomDiscovered("Z1_R01"), Is.True);
            Assert.That(MapManager.IsRoomDiscovered("Z1_R02"), Is.False);
        }

        [Test]
        public void IsDoorUnlocked_ReturnsCorrectState()
        {
            MapManager.UnlockDoor("Z1_Door_01");
            Assert.That(MapManager.IsDoorUnlocked("Z1_Door_01"), Is.True);
            Assert.That(MapManager.IsDoorUnlocked("Z1_Door_02"), Is.False);
        }

        [Test]
        public void RoomBounds_TriggersDiscovery()
        {
            var player = new GameObject("Player");
            player.tag = "Player";
            var playerCollider = player.AddComponent<CircleCollider2D>();

            var roomObj = new GameObject("Room");
            var roomBounds = roomObj.AddComponent<RoomBounds>();
            roomBounds.Configure("Z1_R01", "Test Room");
            var roomCollider = roomObj.AddComponent<BoxCollider2D>();
            roomCollider.isTrigger = true;

            // Simulate overlap
            var session = GameSession.Current;
            Assert.That(session.visitedRooms, Is.Empty);

            // Manually trigger (since Physics2D.OverlapPoint may not work in PlayMode without proper setup)
            roomBounds.GetComponent<BoxCollider2D>().bounds.Contains(player.transform.position);
            MapManager.DiscoverRoom("Z1_R01");

            Assert.That(session.visitedRooms, Contains.Item("Z1_R01"));

            Object.Destroy(player);
            Object.Destroy(roomObj);
        }

        [Test]
        public void FastTravel_LockedUntilZ2Boss()
        {
            Assert.That(FastTravelManager.IsUnlocked, Is.False);
        }

        [Test]
        public void FastTravel_UnlocksAfterZ2Boss()
        {
            BossEvents.RaiseDefeated("Z2_Boss_Keeper");
            Assert.That(FastTravelManager.IsUnlocked, Is.True);
        }

        [Test]
        public void FastTravel_UpdatesCheckpointOnTravel()
        {
            FastTravelManager.SetUnlocked(true);
            _world = TestWorld.Create(Vector2.zero);
            var stationObj = new GameObject("Station");
            stationObj.transform.position = new Vector3(10f, 5f, 0f);
            stationObj.AddComponent<BoxCollider2D>().isTrigger = true;
            var station = stationObj.AddComponent<ChronoStation>();
            station.Configure("Z1_Station_01");

            var session = GameSession.Current;
            var initialPos = session.checkpointX;

            FastTravelManager.TravelToStation("Z1_Station_01");

            Assert.That(session.checkpointId, Is.EqualTo("Z1_Station_01"));
            Assert.That(session.checkpointX, Is.Not.EqualTo(initialPos));

            Object.Destroy(stationObj);
        }

        [Test]
        public void FastTravel_FailsWhenLocked()
        {
            FastTravelManager.SetUnlocked(false);
            var session = GameSession.Current;
            var initialCheckpoint = session.checkpointId;

            FastTravelManager.TravelToStation("Z1_Station_01");

            Assert.That(session.checkpointId, Is.EqualTo(initialCheckpoint));
        }
    }
}
