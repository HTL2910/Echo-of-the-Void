using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using EchoOfTheVoid.Core;
using EchoOfTheVoid.Environment.Mechanics;

namespace EchoOfTheVoid.Tests
{
    /// <summary>K3: Tests for environmental mechanics — Spikes, BouncePad, PressurePlate, Door, Lever.</summary>
    public class MechanicK3Tests
    {
        [TearDown]
        public void Teardown()
        {
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
                Object.Destroy(go);
        }

        // ─────────────────────────────────────────────
        // PressurePlate + Door
        // ─────────────────────────────────────────────

        [UnityTest]
        public IEnumerator PressurePlate_PlayerTagActivates_FiresPressedEvent()
        {
            var plateGo = new GameObject("Plate");
            var plateTrigger = plateGo.AddComponent<BoxCollider2D>();
            plateTrigger.isTrigger = true;
            var plate = plateGo.AddComponent<PressurePlate>();

            bool pressed = false;
            plate.Pressed += () => pressed = true;

            // Simulate player entering trigger
            var playerGo = new GameObject("Player");
            playerGo.tag = "Player";
            var prb = playerGo.AddComponent<Rigidbody2D>();
            var pcol = playerGo.AddComponent<BoxCollider2D>();

            yield return null;

            // Manually fire trigger (Physics simulation works in play mode)
            // We'll test via plate's public IsPressed after direct test
            // Use SendMessage approach to fire OnTriggerEnter2D
            plate.SendMessage("OnTriggerEnter2D", pcol, SendMessageOptions.DontRequireReceiver);

            Assert.IsTrue(plate.IsPressed, "PressurePlate.IsPressed should be true after player enters");
            Assert.IsTrue(pressed, "PressurePlate.Pressed event should have fired");
        }

        [UnityTest]
        public IEnumerator PressurePlate_Released_FiresReleasedEvent()
        {
            var plateGo = new GameObject("Plate");
            plateGo.AddComponent<BoxCollider2D>().isTrigger = true;
            var plate = plateGo.AddComponent<PressurePlate>();

            bool released = false;
            plate.Released += () => released = true;

            var playerGo = new GameObject("Player");
            playerGo.tag = "Player";
            var pcol = playerGo.AddComponent<BoxCollider2D>();

            yield return null;

            // Press then release
            plate.SendMessage("OnTriggerEnter2D", pcol, SendMessageOptions.DontRequireReceiver);
            plate.SendMessage("OnTriggerExit2D", pcol, SendMessageOptions.DontRequireReceiver);

            Assert.IsFalse(plate.IsPressed);
            Assert.IsTrue(released, "PressurePlate.Released event should fire when player exits");
        }

        [UnityTest]
        public IEnumerator Door_WhenPlatePressed_OpensImmediately()
        {
            var plateGo = new GameObject("Plate");
            plateGo.AddComponent<BoxCollider2D>().isTrigger = true;
            var plate = plateGo.AddComponent<PressurePlate>();

            var doorGo = new GameObject("Door");
            doorGo.AddComponent<BoxCollider2D>();
            var door = doorGo.AddComponent<Door>();
            door.LinkPlate(plate);

            yield return null;

            // Press the plate
            var playerGo = new GameObject("Player");
            playerGo.tag = "Player";
            var pcol = playerGo.AddComponent<BoxCollider2D>();
            plate.SendMessage("OnTriggerEnter2D", pcol, SendMessageOptions.DontRequireReceiver);

            // Wait a tiny bit for coroutine to start
            yield return new WaitForSeconds(0.1f);

            Assert.IsTrue(door.IsOpen, "Door should open when PressurePlate is pressed");
        }

        [UnityTest]
        public IEnumerator Door_WhenPlateReleased_ClosesAfterDelay()
        {
            var plateGo = new GameObject("Plate");
            plateGo.AddComponent<BoxCollider2D>().isTrigger = true;
            var plate = plateGo.AddComponent<PressurePlate>();

            var doorGo = new GameObject("Door");
            doorGo.AddComponent<BoxCollider2D>();
            var door = doorGo.AddComponent<Door>();
            door.LinkPlate(plate);

            yield return null;

            // Press then release
            var playerGo = new GameObject("Player");
            playerGo.tag = "Player";
            var pcol = playerGo.AddComponent<BoxCollider2D>();

            plate.SendMessage("OnTriggerEnter2D", pcol, SendMessageOptions.DontRequireReceiver);
            yield return new WaitForSeconds(0.1f); // let it open

            plate.SendMessage("OnTriggerExit2D", pcol, SendMessageOptions.DontRequireReceiver);

            // Door should be open right after release
            Assert.IsTrue(door.IsOpen, "Door should still be open right after plate released");

            // Wait for default close delay (1.5s) + slide time
            yield return new WaitForSeconds(2.0f);

            Assert.IsFalse(door.IsOpen, "Door should close after closeDelay has passed");
        }

        // ─────────────────────────────────────────────
        // Lever
        // ─────────────────────────────────────────────

        [UnityTest]
        public IEnumerator Lever_StartsOff()
        {
            var leverGo = new GameObject("Lever");
            leverGo.AddComponent<BoxCollider2D>().isTrigger = true;
            var lever = leverGo.AddComponent<Lever>();
            yield return null;

            Assert.IsFalse(lever.IsOn, "Lever should start in OFF state");
        }

        [UnityTest]
        public IEnumerator Lever_Toggle_ChangesState()
        {
            var leverGo = new GameObject("Lever");
            leverGo.AddComponent<BoxCollider2D>().isTrigger = true;
            var lever = leverGo.AddComponent<Lever>();
            yield return null;

            bool toggledValue = false;
            lever.Toggled += (v) => toggledValue = v;

            lever.Toggle();
            Assert.IsTrue(lever.IsOn, "Lever should be ON after first toggle");
            Assert.IsTrue(toggledValue, "Toggled event should fire with true");

            lever.Toggle();
            Assert.IsFalse(lever.IsOn, "Lever should be OFF after second toggle");
            Assert.IsFalse(toggledValue, "Toggled event should fire with false");
        }

        // ─────────────────────────────────────────────
        // BouncePad
        // ─────────────────────────────────────────────

        [UnityTest]
        public IEnumerator BouncePad_DoesNotThrow_OnCreate()
        {
            Assert.DoesNotThrow(() =>
            {
                var go = new GameObject("BouncePad");
                go.AddComponent<BoxCollider2D>();
                go.AddComponent<BouncePad>();
            });
            yield return null;
        }
    }
}
