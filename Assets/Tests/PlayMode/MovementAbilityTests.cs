using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using EchoOfTheVoid.Combat;
using EchoOfTheVoid.Core;
using EchoOfTheVoid.Enemies;
using EchoOfTheVoid.Environment;
using EchoOfTheVoid.Environment.Mechanics;
using EchoOfTheVoid.Player;

namespace EchoOfTheVoid.Tests
{
    public class EchoAnchorTests
    {
        private TestWorld _world;
        private GameObject _extra;

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            _world?.Dispose();
            _world = null;
            if (_extra != null) Object.Destroy(_extra);
        }

        private IEnumerator Start(bool unlocked = true)
        {
            _world = TestWorld.Create(new Vector2(0f, 1.5f));
            if (unlocked) _world.Abilities.Unlock(AbilityFlags.EchoAnchor);
            yield return new WaitForSeconds(0.6f);
        }

        [UnityTest]
        public IEnumerator WithoutTheAbility_NothingHappens()
        {
            yield return Start(unlocked: false);
            Assert.IsFalse(_world.Anchor.Activate());
            Assert.IsFalse(_world.Anchor.HasAnchor);
            Assert.AreEqual(100f, _world.Stats.CurrentEnergy, 1f, "and no energy was spent");
        }

        [UnityTest]
        public IEnumerator Placing_CostsEnergy_AndLeavesOneShadowOnTheAnchorLayer()
        {
            yield return Start();
            float before = _world.Stats.CurrentEnergy;

            Assert.IsTrue(_world.Anchor.Activate());

            Assert.IsTrue(_world.Anchor.HasAnchor);
            Assert.AreEqual(before - EchoAnchor.EnergyCost, _world.Stats.CurrentEnergy, 2f);
            var shadow = GameObject.Find("EchoAnchor_Shadow");
            Assert.IsNotNull(shadow);
            Assert.AreEqual(LayerMask.NameToLayer("Anchor"), shadow.layer, "Pressure plates look for the Anchor layer");
            int shadows = 0;
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None)) if (go.name == "EchoAnchor_Shadow") shadows++;
            Assert.AreEqual(1, shadows, "Only one shadow at a time");

        }

        [UnityTest]
        public IEnumerator NotEnoughEnergy_RefusesToPlace()
        {
            yield return Start();
            _world.Stats.ConsumeEnergy(90);
            Assert.IsFalse(_world.Anchor.Activate());
            Assert.IsFalse(_world.Anchor.HasAnchor);
        }

        [UnityTest]
        public IEnumerator PressingAgain_SwapsKaelBackToTheShadow_ForFree()
        {
            yield return Start();
            float startX = _world.Player.transform.position.x;
            _world.Anchor.Activate();

            _world.Input.Move = 1f;
            yield return new WaitForSeconds(0.6f);
            _world.Input.Move = 0f;
            Assert.Greater(_world.Player.transform.position.x, startX + 3f, "Kael has moved away");
            float energy = _world.Stats.CurrentEnergy;

            bool swapped = false;
            _world.Anchor.Swapped += () => swapped = true;
            Assert.IsTrue(_world.Anchor.Activate());

            Assert.IsTrue(swapped);
            Assert.That(_world.Player.transform.position.x, Is.EqualTo(startX).Within(0.1f), "Kael is back at the shadow");
            Assert.IsFalse(_world.Anchor.HasAnchor, "The shadow is used up");
            Assert.GreaterOrEqual(_world.Stats.CurrentEnergy, energy, "Swapping is free");
        }

        [UnityTest]
        public IEnumerator TheShadow_ExpiresAfterEightSeconds()
        {
            yield return Start();
            bool expired = false;
            _world.Anchor.Expired += () => expired = true;
            _world.Anchor.Activate();

            Time.timeScale = 4f;                        // 8 game seconds in 2 real seconds
            yield return new WaitForSecondsRealtime(2.4f);
            Time.timeScale = 1f;

            Assert.IsTrue(expired);
            Assert.IsFalse(_world.Anchor.HasAnchor);
        }

        [UnityTest]
        public IEnumerator TheShadow_HoldsAPressurePlateDown_WhileKaelIsElsewhere()
        {
            yield return Start();
            _extra = new GameObject("Plate");
            _extra.transform.position = new Vector3(6f, 0.1f, 0f);
            var box = _extra.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(2f, 0.6f);
            var plate = _extra.AddComponent<PressurePlate>();

            _world.Player.transform.position = new Vector3(6f, 1f, 0f); // step on it
            yield return new WaitForSeconds(0.4f);
            Assert.IsTrue(plate.IsPressed, "Kael presses it");

            _world.Anchor.Activate();                                    // leave a shadow on the plate
            _world.Player.transform.position = new Vector3(-6f, 1f, 0f); // and walk away
            yield return new WaitForSeconds(0.5f);
            Assert.IsTrue(plate.IsPressed, "The shadow keeps the plate pressed (spec 4: Echo Anchor)");

            _world.Anchor.Clear();
            yield return new WaitForSeconds(0.3f);
            Assert.IsFalse(plate.IsPressed, "Once the shadow is gone the plate releases");
        }

        [UnityTest]
        public IEnumerator Swap_IsRefused_WhenTheShadowIsNowInsideSolidGround()
        {
            yield return Start();
            _world.Anchor.Activate();
            _world.Input.Move = 1f;
            yield return new WaitForSeconds(0.5f);
            _world.Input.Move = 0f;

            _world.CreateBox("Block", new Vector2(0f, 1f), new Vector2(3f, 3f)); // grows around the shadow
            yield return new WaitForFixedUpdate();
            float x = _world.Player.transform.position.x;
            bool blocked = false;
            _world.Anchor.Blocked += () => blocked = true;

            Assert.IsFalse(_world.Anchor.Activate());

            Assert.IsTrue(blocked);
            Assert.That(_world.Player.transform.position.x, Is.EqualTo(x).Within(0.05f), "Kael did not move");
            Assert.IsTrue(_world.Anchor.HasAnchor, "The shadow stays so the player can try again");
        }

        [UnityTest]
        public IEnumerator Dying_RemovesTheShadow()
        {
            yield return Start();
            _world.Anchor.Activate();
            _world.Stats.TakeDamage(new DamageInfo(999, Vector2.zero, Vector2.zero, RealmType.Prime));
            yield return null;
            Assert.IsFalse(_world.Anchor.HasAnchor);
        }
    }

    public class GravityInversionTests
    {
        private TestWorld _world;
        private GameObject _field;

        [TearDown]
        public void TearDown()
        {
            _world?.Dispose();
            _world = null;
            if (_field != null) Object.Destroy(_field);
        }

        /// <summary>Floor at y = 0, ceiling whose underside is at y = 6, and a graviton field around x = 0.</summary>
        private IEnumerator BuildRoom(bool withAbility)
        {
            _world = TestWorld.Create(new Vector2(0f, 1.5f));
            _world.CreateBox("Ceiling", new Vector2(0f, 6.5f), new Vector2(40f, 1f));
            _field = new GameObject("GravitonField");
            _field.transform.position = new Vector3(0f, 3f, 0f);
            _field.AddComponent<BoxCollider2D>().size = new Vector2(6f, 8f);
            _field.AddComponent<GravitonField>();
            _field.GetComponent<BoxCollider2D>().isTrigger = true;
            if (withAbility) _world.Abilities.Unlock(AbilityFlags.GravityInversion);
            yield return new WaitForSeconds(0.6f);
        }

        [UnityTest]
        public IEnumerator OutsideAField_OrWithoutTheAbility_GravityStaysNormal()
        {
            yield return BuildRoom(withAbility: false);
            Assert.IsTrue(_world.Controller.InGravitonField);
            Assert.IsFalse(_world.Controller.TryToggleGravity(), "Needs the Graviton Core");

            _world.Abilities.Unlock(AbilityFlags.GravityInversion);
            _world.Player.transform.position = new Vector3(20f, 1.5f, 0f);
            yield return new WaitForSeconds(0.4f);
            Assert.IsFalse(_world.Controller.InGravitonField);
            Assert.IsFalse(_world.Controller.TryToggleGravity(), "and only inside a field");
        }

        [UnityTest]
        public IEnumerator InsideAField_KaelFallsUpToTheCeiling_AndJumpsDownward()
        {
            yield return BuildRoom(withAbility: true);

            _world.Input.PressGravity();
            yield return new WaitForSeconds(1.0f);

            Assert.IsTrue(_world.Controller.IsGravityInverted);
            Assert.IsTrue(_world.Controller.IsGrounded, "The ceiling is the ground now");
            Assert.That(_world.Player.transform.position.y, Is.EqualTo(6f - 0.8125f).Within(0.15f), "Standing under the ceiling");

            _world.Input.PressJump();
            float minY = _world.Player.transform.position.y;
            for (int i = 0; i < 12; i++) { yield return new WaitForFixedUpdate(); minY = Mathf.Min(minY, _world.Player.transform.position.y); }
            Assert.Less(minY, 5f, "Jumping goes away from the ceiling, i.e. downward");
        }

        [UnityTest]
        public IEnumerator LeavingTheField_RestoresGravityAfterAWarning()
        {
            yield return BuildRoom(withAbility: true);
            _world.Input.PressGravity();
            yield return new WaitForSeconds(0.9f);
            Assert.IsTrue(_world.Controller.IsGravityInverted);

            _world.Input.Move = 1f;
            yield return new WaitForSeconds(0.6f);      // runs out of the 6-wide field
            _world.Input.Move = 0f;
            Assert.IsFalse(_world.Controller.InGravitonField);
            Assert.IsTrue(_world.Controller.IsGravityInverted, "Not yet: there is a 1 s grace period");

            yield return new WaitForSeconds(1.5f);
            Assert.IsFalse(_world.Controller.IsGravityInverted, "Gravity came back by itself");
            yield return new WaitForSeconds(0.8f);
            Assert.That(_world.Player.transform.position.y, Is.EqualTo(0.8125f).Within(0.2f), "Kael dropped back to the floor");
        }

        [UnityTest]
        public IEnumerator Dying_WhileInverted_RespawnsTheRightWayUp()
        {
            yield return BuildRoom(withAbility: true);
            _world.Input.PressGravity();
            yield return new WaitForSeconds(0.9f);
            Assert.IsTrue(_world.Controller.IsGravityInverted);

            _world.Stats.TakeDamage(new DamageInfo(999, Vector2.zero, Vector2.zero, RealmType.Prime));
            yield return new WaitForSecondsRealtime(1.7f);

            Assert.IsFalse(_world.Controller.IsGravityInverted);
        }
    }

    public class RailGrindTests
    {
        private TestWorld _world;
        private GameObject _cable;

        [TearDown]
        public void TearDown()
        {
            _world?.Dispose();
            _world = null;
            if (_cable != null) Object.Destroy(_cable);
        }

        /// <summary>A downhill cable from (0,4) to (12,2) with Kael dropped onto it near the top.</summary>
        private RailCable BuildCable(bool active = true)
        {
            _world = TestWorld.Create(new Vector2(2f, 7f));
            _cable = new GameObject("Cable");
            _cable.AddComponent<BoxCollider2D>();
            var rail = _cable.AddComponent<RailCable>();
            rail.SetupCable(new Vector2(0f, 4f), new Vector2(12f, 2f));
            rail.SetActive(active);
            return rail;
        }

        [UnityTest]
        public IEnumerator FallingOntoAnActiveCable_StartsAGrind_AlongTheCable()
        {
            BuildCable();
            bool grinding = false;
            float speed = 0f;
            for (int i = 0; i < 60 && !grinding; i++)
            {
                yield return new WaitForFixedUpdate();
                grinding = _world.Controller.CurrentStateName == "PlayerRailGrindState";
            }
            Assert.IsTrue(grinding, "Kael latches onto the cable");

            float x0 = _world.Player.transform.position.x;
            yield return new WaitForSeconds(0.3f);
            speed = _world.Rb.linearVelocity.magnitude;
            Assert.Greater(_world.Player.transform.position.x, x0 + 2f, "and rides it downhill");
            Assert.GreaterOrEqual(speed, 9.5f, "at least run speed");
        }

        [UnityTest]
        public IEnumerator ReachingTheEnd_DropsKael()
        {
            BuildCable();
            yield return new WaitForSeconds(2.2f);

            Assert.AreNotEqual("PlayerRailGrindState", _world.Controller.CurrentStateName, "The ride is over");
            Assert.Greater(_world.Player.transform.position.x, 11f, "He came off the far end");
        }

        [UnityTest]
        public IEnumerator Jumping_KicksOffTheCable()
        {
            BuildCable();
            for (int i = 0; i < 60 && _world.Controller.CurrentStateName != "PlayerRailGrindState"; i++)
                yield return new WaitForFixedUpdate();
            Assert.AreEqual("PlayerRailGrindState", _world.Controller.CurrentStateName);
            yield return new WaitForSeconds(0.15f);

            _world.Input.PressJump();
            yield return new WaitForSeconds(0.1f);

            Assert.AreNotEqual("PlayerRailGrindState", _world.Controller.CurrentStateName);
            Assert.Greater(_world.Rb.linearVelocity.y, 3f, "Kael is thrown upward");
            yield return new WaitForSeconds(0.15f);
            Assert.AreNotEqual("PlayerRailGrindState", _world.Controller.CurrentStateName, "and does not stick straight back on");
        }

        [UnityTest]
        public IEnumerator AnInactiveCable_IsIgnored()
        {
            BuildCable(active: false);
            yield return new WaitForSeconds(1.2f);
            Assert.AreNotEqual("PlayerRailGrindState", _world.Controller.CurrentStateName);
            Assert.IsTrue(_world.Controller.IsGrounded, "Kael fell straight through to the floor");
        }

        [UnityTest]
        public IEnumerator TheCableTurningBackIntoALaser_DropsKaelAtOnce()
        {
            var rail = BuildCable();
            for (int i = 0; i < 60 && _world.Controller.CurrentStateName != "PlayerRailGrindState"; i++)
                yield return new WaitForFixedUpdate();
            Assert.AreEqual("PlayerRailGrindState", _world.Controller.CurrentStateName);

            rail.SetActive(false); // the world shifted back to Prime
            yield return new WaitForSeconds(0.2f);

            Assert.AreNotEqual("PlayerRailGrindState", _world.Controller.CurrentStateName);
        }
    }
}
