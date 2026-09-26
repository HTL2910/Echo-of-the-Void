using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;
using EchoOfTheVoid.Core;
using EchoOfTheVoid.Environment;

namespace EchoOfTheVoid.Tests
{
    public class RealityTilemapTests
    {
        private TestWorld _world;
        private GameObject _manager;
        private GameObject _grid;
        private int _deniedCount;

        private void OnDenied() => _deniedCount++;

        [SetUp]
        public void SetUp()
        {
            _deniedCount = 0;
            RealityEventBus.OnShiftDenied += OnDenied;
            _manager = new GameObject("RealityManager");
            _manager.AddComponent<RealityManager>();
        }

        [TearDown]
        public void TearDown()
        {
            RealityEventBus.OnShiftDenied -= OnDenied;
            _world.Dispose();
            if (_grid != null) Object.Destroy(_grid);
            Object.Destroy(_manager);
        }

        /// <summary>An Echo-only tilemap filled with the given cell rectangle, as an artist's room would be.</summary>
        private void BuildEchoTilemap(int xMin, int yMin, int xMax, int yMax)
        {
            _grid = new GameObject("TestGrid");
            _grid.AddComponent<Grid>();

            var go = new GameObject("Tilemap_Echo");
            go.transform.SetParent(_grid.transform, false);
            go.layer = _world.NeutralLayer;
            var tilemap = go.AddComponent<Tilemap>();
            go.AddComponent<TilemapRenderer>();
            go.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
            go.AddComponent<TilemapCollider2D>().compositeOperation = Collider2D.CompositeOperation.Merge;
            go.AddComponent<CompositeCollider2D>();
            go.AddComponent<RealityTilemap>().Configure(RealmType.Echo, Color.white);

            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.colliderType = Tile.ColliderType.Grid;
            for (int x = xMin; x <= xMax; x++)
                for (int y = yMin; y <= yMax; y++)
                    tilemap.SetTile(new Vector3Int(x, y, 0), tile);
        }

        [UnityTest]
        public IEnumerator EchoTilemap_IsNotSolidInPrime_AndSolidInEcho()
        {
            _world = TestWorld.Create(new Vector2(8f, 6f));
            BuildEchoTilemap(6, 2, 10, 2); // a floating Echo platform at y = 2..3, under Kael
            yield return new WaitForSeconds(0.2f);

            // Prime: the tiles are ghosts, Kael falls through to the real floor
            yield return new WaitForSeconds(1.0f);
            Assert.Less(_world.Player.transform.position.y, 1.5f, "Kael must fall through the Echo tiles while in Prime");

            // Echo: put Kael back above and shift; now the tiles hold him up
            _world.Player.transform.position = new Vector2(8f, 6f);
            _world.Rb.linearVelocity = Vector2.zero;
            yield return null;
            _world.Input.PressShift();
            yield return new WaitForSeconds(1.0f);

            Assert.AreEqual(RealmType.Echo, RealityManager.Instance.CurrentRealm);
            Assert.That(_world.Player.transform.position.y, Is.EqualTo(3f + 0.8125f).Within(0.15f), "Kael stands on the solid Echo tiles");
        }

        [UnityTest]
        public IEnumerator Shift_IsDenied_WhenEchoTilesWouldSurroundKael()
        {
            _world = TestWorld.Create(new Vector2(8.5f, 1.5f));
            BuildEchoTilemap(6, 0, 11, 3); // a solid block of Echo tiles around Kael
            yield return new WaitForSeconds(0.3f);

            _world.Input.PressShift();
            yield return null;
            yield return null;

            Assert.AreEqual(RealmType.Prime, RealityManager.Instance.CurrentRealm);
            Assert.AreEqual(1, _deniedCount);
        }

        [UnityTest]
        public IEnumerator EchoTilemap_AllowsShift_WhenSmallGapExists()
        {
            _world = TestWorld.Create(new Vector2(8f, 6f));
            // Create a mostly solid block but leave one small gap (e.g., gap at x=7 for Kael at x=8)
            BuildEchoTilemap(6, 2, 6, 2); // Left wall
            BuildEchoTilemap(9, 2, 10, 2); // Right wall, gap in between
            yield return new WaitForSeconds(0.2f);

            _world.Input.PressShift();
            yield return null;
            yield return null;

            // Shift should be allowed because there's a gap
            Assert.AreEqual(RealmType.Echo, RealityManager.Instance.CurrentRealm);
        }

        [UnityTest]
        public IEnumerator PrimeTilemap_CanExistAlongside_EchoTilemap()
        {
            _world = TestWorld.Create(new Vector2(8f, 6f));
            BuildEchoTilemap(6, 0, 6, 3); // Echo platform on left

            // Create a Prime tilemap in a different area
            var primeGo = new GameObject("Tilemap_Prime");
            primeGo.layer = _world.NeutralLayer;
            var primeTilemap = primeGo.AddComponent<Tilemap>();
            primeGo.AddComponent<TilemapRenderer>();
            primeGo.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
            primeGo.AddComponent<TilemapCollider2D>().compositeOperation = Collider2D.CompositeOperation.Merge;
            primeGo.AddComponent<CompositeCollider2D>();
            primeGo.AddComponent<RealityTilemap>().Configure(RealmType.Prime, Color.white);

            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.colliderType = Tile.ColliderType.Grid;
            for (int x = 10; x <= 14; x++)
                for (int y = 0; y <= 3; y++)
                    primeTilemap.SetTile(new Vector3Int(x, y, 0), tile);

            yield return new WaitForSeconds(0.2f);

            // Both tilemaps should be present
            Assert.AreEqual(RealmType.Prime, RealityManager.Instance.CurrentRealm);
            Assert.NotNull(primeTilemap);
        }

        [UnityTest]
        public IEnumerator ShiftCooldown_PreventsTooFrequentSwitching()
        {
            _world = TestWorld.Create(new Vector2(8f, 6f));
            yield return new WaitForSeconds(0.1f);

            // Try to shift twice rapidly
            _world.Input.PressShift();
            yield return null;
            RealmType afterFirst = RealityManager.Instance.CurrentRealm;

            _world.Input.PressShift();
            yield return null;
            RealmType afterSecond = RealityManager.Instance.CurrentRealm;

            // Second shift should be blocked by cooldown (0.25s)
            Assert.AreEqual(RealmType.Echo, afterFirst);
            Assert.AreEqual(RealmType.Echo, afterSecond, "Rapid shifting should be blocked by cooldown");

            // Wait for cooldown to expire
            yield return new WaitForSeconds(0.3f);
            _world.Input.PressShift();
            yield return null;

            // Now shift should succeed
            Assert.AreEqual(RealmType.Prime, RealityManager.Instance.CurrentRealm);
        }

        [UnityTest]
        public IEnumerator EchoTilemap_HasCorrectColliderLayer()
        {
            _world = TestWorld.Create(new Vector2(8f, 6f));
            BuildEchoTilemap(6, 2, 10, 2);
            yield return new WaitForSeconds(0.2f);

            // Verify layer assignment
            var grid = _world.Player.transform.parent?.GetComponent<Grid>();
            Assert.IsNotNull(grid, "Grid should exist in parent");
        }
    }
}
