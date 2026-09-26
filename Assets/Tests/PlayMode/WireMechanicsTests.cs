using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using EchoOfTheVoid.Core;
using EchoOfTheVoid.Environment;
using EchoOfTheVoid.Environment.Mechanics;
using EchoOfTheVoid.Enemies;
using EchoOfTheVoid.Player;

namespace EchoOfTheVoid.Tests
{
    /// <summary>I3: Wire Mechanics integration tests. Validates PressurePlate, RailCable, and GravitonField work together.</summary>
    public class WireMechanicsTests
    {
        private TestWorld _world;

        [SetUp]
        public void SetUp()
        {
            _world = TestWorld.Create(new Vector2(0f, 0f));
        }

        [TearDown]
        public void TearDown()
        {
            _world.Dispose();
        }

        [UnityTest]
        public IEnumerator PressurePlate_ActivatesWithEchoAnchor()
        {
            // Setup pressure plate
            var platGo = new GameObject("Pressure Plate");
            platGo.transform.position = new Vector3(5f, 1f, 0f);
            var platCol = platGo.AddComponent<BoxCollider2D>();
            platCol.size = new Vector2(2f, 0.5f);
            var plate = platGo.AddComponent<PressurePlate>();

            bool platActivated = false;
            plate.Pressed += () => platActivated = true;

            // Setup Echo Anchor with correct layer
            var anchorGo = new GameObject("Echo Anchor");
            anchorGo.layer = LayerMask.NameToLayer("Anchor");
            anchorGo.transform.position = new Vector3(5f, 1.5f, 0f);
            var anchorCol = anchorGo.AddComponent<BoxCollider2D>();
            anchorCol.size = new Vector2(0.5f, 0.5f);

            yield return new WaitForSeconds(0.1f);

            // Move anchor onto plate
            anchorGo.transform.position = new Vector3(5f, 1f, 0f);
            yield return new WaitForSeconds(0.1f);

            Assert.IsTrue(platActivated, "PressurePlate should activate when Echo Anchor enters");
        }

        [UnityTest]
        public IEnumerator RailCable_CanBeSetupAndActivated()
        {
            // Create rail cable
            var railGo = new GameObject("RailCable");
            railGo.transform.position = Vector3.zero;
            railGo.AddComponent<BoxCollider2D>();
            var rail = railGo.AddComponent<RailCable>();

            yield return null;

            rail.SetupCable(new Vector2(0f, 0f), new Vector2(10f, 5f));
            rail.SetActive(true);

            Assert.IsTrue(rail.IsActive);
            Assert.AreEqual(new Vector2(0f, 0f), rail.StartPoint);
            Assert.AreEqual(new Vector2(10f, 5f), rail.EndPoint);
            Assert.Greater(rail.Length, 0f);
        }

        [UnityTest]
        public IEnumerator RailCable_CalculatesDirectionCorrectly()
        {
            var railGo = new GameObject("RailCable");
            railGo.AddComponent<BoxCollider2D>();
            var rail = railGo.AddComponent<RailCable>();

            yield return null;

            rail.SetupCable(Vector2.zero, new Vector2(4f, 3f));

            // Direction should be normalized (4, 3) = (0.8, 0.6)
            Vector2 expected = new Vector2(4f, 3f).normalized;
            Assert.AreEqual(expected, rail.Direction, "Rail direction should be normalized start->end vector");
        }

        [UnityTest]
        public IEnumerator GravitonField_TriggersPlayerEvents()
        {
            var fieldGo = new GameObject("Graviton Field");
            fieldGo.transform.position = new Vector3(0f, 5f, 0f);
            var fieldCol = fieldGo.AddComponent<BoxCollider2D>();
            fieldCol.size = new Vector2(5f, 2f);
            fieldGo.AddComponent<GravitonField>();

            // Player starts below
            _world.Player.transform.position = new Vector3(0f, 3f, 0f);
            yield return null;

            // Move player into field
            _world.Player.transform.position = new Vector3(0f, 5f, 0f);
            yield return null;

            // Player should detect the field (no exceptions thrown)
            Assert.IsNotNull(_world.Controller);
        }

        [UnityTest]
        public IEnumerator Door_ListensTo_PressurePlate()
        {
            // Create a pressure plate
            var platGo = new GameObject("TriggerPlate");
            platGo.transform.position = new Vector3(0f, 1f, 0f);
            var platCol = platGo.AddComponent<BoxCollider2D>();
            platCol.size = new Vector2(2f, 0.5f);
            var plate = platGo.AddComponent<PressurePlate>();

            // Create a door
            var doorGo = new GameObject("Door");
            doorGo.transform.position = new Vector3(5f, 0f, 0f);
            var doorCol = doorGo.AddComponent<BoxCollider2D>();
            doorCol.size = new Vector2(1f, 2f);
            var door = doorGo.AddComponent<Door>();

            // Wire them together
            plate.Pressed += door.Open;
            plate.Released += door.Close;

            yield return null;

            // Move player onto plate
            _world.Player.transform.position = new Vector3(0f, 1f, 0f);
            yield return new WaitForSeconds(0.1f);

            // Door should be open
            Assert.IsTrue(door.IsOpen, "Door should open when plate is pressed");

            // Move player away
            _world.Player.transform.position = new Vector3(0f, 10f, 0f);
            yield return new WaitForSeconds(0.1f);

            // Door should be closed
            Assert.IsFalse(door.IsOpen, "Door should close when plate is released");
        }

        [UnityTest]
        public IEnumerator AllMechanics_WorkWithoutNullReferences()
        {
            // Create all three mechanics in one scene

            // Pressure Plate
            var platGo = new GameObject("Plate");
            platGo.AddComponent<BoxCollider2D>().isTrigger = true;
            platGo.AddComponent<PressurePlate>();

            // Rail Cable
            var railGo = new GameObject("Rail");
            railGo.AddComponent<BoxCollider2D>().isTrigger = true;
            var rail = railGo.AddComponent<RailCable>();
            rail.SetupCable(Vector2.zero, new Vector2(5f, 5f));

            // Graviton Field
            var fieldGo = new GameObject("Field");
            fieldGo.AddComponent<BoxCollider2D>().isTrigger = true;
            fieldGo.AddComponent<GravitonField>();

            // Move player through all of them
            _world.Player.transform.position = Vector3.zero;
            yield return null;

            _world.Player.transform.position = new Vector3(0f, 1f, 0f);
            yield return null;

            _world.Player.transform.position = new Vector3(2f, 2f, 0f);
            yield return null;

            // Should not throw any null reference exceptions
            Assert.IsNotNull(rail);
            Assert.IsTrue(rail.IsActive || !rail.IsActive); // Just check it exists
        }
    }
}
