using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using EchoOfTheVoid.Core;
using EchoOfTheVoid.Environment;
using EchoOfTheVoid.Player;

namespace EchoOfTheVoid.Tests
{
    public class AbilityTests
    {
        private TestWorld _world;

        [TearDown]
        public void TearDown() => _world.Dispose();

        /// <summary>A tall wall on Kael's right; he starts in mid-air beside it so he slides down it.</summary>
        private void BuildWallJumpScene()
        {
            _world = TestWorld.Create(new Vector2(0f, 6f));
            _world.CreateBox("TestWall", new Vector2(1.0f, 8f), new Vector2(1f, 20f)); // left face at x = 0.5
        }

        [Test]
        public void NewGame_StartsWithShiftAndDash_ButNotWallJumpOrResonance()
        {
            _world = TestWorld.Create(new Vector2(0f, 1.5f));
            Assert.IsTrue(_world.Abilities.Has(AbilityFlags.RealityShift));
            Assert.IsTrue(_world.Abilities.Has(AbilityFlags.PhaseDash));
            Assert.IsFalse(_world.Abilities.Has(AbilityFlags.WallJump), "Wall Jump is locked until the Piston Boots (spec D8)");
            Assert.IsFalse(_world.Abilities.Has(AbilityFlags.ResonanceStrike));
        }

        [Test]
        public void Unlock_ReportsOnlyNewAbilities_AndRaisesTheEventOnce()
        {
            _world = TestWorld.Create(new Vector2(0f, 1.5f));
            int raised = 0;
            _world.Abilities.OnAbilityUnlocked += _ => raised++;

            Assert.IsTrue(_world.Abilities.Unlock(AbilityFlags.WallJump));
            Assert.IsFalse(_world.Abilities.Unlock(AbilityFlags.WallJump), "Second unlock is a no-op");
            Assert.IsFalse(_world.Abilities.Unlock(AbilityFlags.None));

            Assert.AreEqual(1, raised);
            Assert.IsTrue(_world.Abilities.Has(AbilityFlags.WallJump));
        }

        [UnityTest]
        public IEnumerator WallJump_IsRefused_WhileLocked()
        {
            BuildWallJumpScene();
            yield return new WaitForSeconds(0.3f); // fall into contact and start sliding
            Assert.IsTrue(_world.Controller.IsTouchingWall);

            _world.Input.PressJump();
            yield return new WaitForSeconds(0.3f);

            Assert.Greater(_world.Player.transform.position.x, -0.5f, "Locked: Kael must not kick off the wall");
        }

        [UnityTest]
        public IEnumerator WallJump_Works_OnceUnlocked()
        {
            BuildWallJumpScene();
            _world.Abilities.Unlock(AbilityFlags.WallJump);
            yield return new WaitForSeconds(0.3f);
            Assert.IsTrue(_world.Controller.IsTouchingWall);

            _world.Input.PressJump();
            yield return new WaitForSeconds(0.3f);

            Assert.Less(_world.Player.transform.position.x, -1.2f, "Unlocked: Kael jumps away from the wall");
        }

        [UnityTest]
        public IEnumerator Dash_IsRefused_WithoutPhaseDash()
        {
            _world = TestWorld.Create(new Vector2(0f, 1.5f));
            _world.Abilities.SetFlags(AbilityFlags.RealityShift);
            yield return new WaitForSeconds(0.6f);
            float startX = _world.Player.transform.position.x;

            _world.Input.PressDash();
            yield return new WaitForSeconds(0.3f);

            Assert.That(_world.Player.transform.position.x, Is.EqualTo(startX).Within(0.1f));
        }

        [UnityTest]
        public IEnumerator Pickup_UnlocksTheAbility_AndDisappears()
        {
            _world = TestWorld.Create(new Vector2(0f, 1.5f));
            var pickup = new GameObject("Pickup");
            pickup.transform.position = new Vector2(0f, 0.8f); // on Kael
            var trigger = pickup.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = 0.7f;
            pickup.AddComponent<AbilityPickup>().Configure(AbilityFlags.WallJump, "PISTON BOOTS");

            yield return new WaitForSeconds(0.5f);

            Assert.IsTrue(_world.Abilities.Has(AbilityFlags.WallJump));
            Assert.IsTrue(pickup == null, "Pickup is consumed");
        }
    }
}
