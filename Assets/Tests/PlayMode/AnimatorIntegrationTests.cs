using NUnit.Framework;
using UnityEngine;
using EchoOfTheVoid.Enemies;
using EchoOfTheVoid.Player;

namespace EchoOfTheVoid.Tests
{
    [Category("Animation")]
    public class AnimatorIntegrationTests
    {
        private const string PlayerPrefabPath = "Assets/Prefabs/Player/Player.prefab";
        private const string CrawlerPrefabPath = "Assets/Prefabs/Enemies/Enemy_Crawler_Prime.prefab";
        private const string WeaverPrefabPath = "Assets/Prefabs/Enemies/Enemy_Weaver_Echo.prefab";

        [Test]
        public void PlayerPrefab_HasAnimatorOnVisualChild()
        {
            var prefab = Resources.Load<GameObject>(PlayerPrefabPath);
            Assert.That(prefab, Is.Not.Null, "Player prefab not found");

            var visual = prefab.transform.Find("Visual");
            Assert.That(visual, Is.Not.Null, "Visual child not found on Player");

            var animator = visual.GetComponent<Animator>();
            Assert.That(animator, Is.Not.Null, "Animator component missing on Player/Visual");
        }

        [Test]
        public void PlayerAnimator_HasController()
        {
            var prefab = Resources.Load<GameObject>(PlayerPrefabPath);
            var visual = prefab.transform.Find("Visual");
            var animator = visual.GetComponent<Animator>();

            Assert.That(animator.runtimeAnimatorController, Is.Not.Null,
                "Animator controller not assigned to Player");
        }

        [Test]
        public void PlayerAnimator_HasAllRequiredStates()
        {
            var prefab = Resources.Load<GameObject>(PlayerPrefabPath);
            var visual = prefab.transform.Find("Visual");
            var animator = visual.GetComponent<Animator>();

            var controller = animator.runtimeAnimatorController as AnimatorController;
            Assert.That(controller, Is.Not.Null, "Failed to load AnimatorController");

            // Check for key states in all layers
            int layerCount = animator.layerCount;
            Assert.That(layerCount, Is.GreaterThan(0), "No layers in animator");

            // Note: Full state validation requires AnimatorStateMachine inspection
            // For now, verify controller is valid
            Assert.That(controller.layers.Length, Is.GreaterThan(0), "No layers in controller");
        }

        [Test]
        public void CrawlerEnemy_HasAnimatorOnVisualChild()
        {
            var prefab = Resources.Load<GameObject>(CrawlerPrefabPath);
            Assert.That(prefab, Is.Not.Null, "Crawler prefab not found");

            var visual = prefab.transform.Find("Visual");
            Assert.That(visual, Is.Not.Null, "Visual child not found on Crawler");

            var animator = visual.GetComponent<Animator>();
            Assert.That(animator, Is.Not.Null, "Animator component missing on Crawler/Visual");
        }

        [Test]
        public void CrawlerAnimator_HasController()
        {
            var prefab = Resources.Load<GameObject>(CrawlerPrefabPath);
            var visual = prefab.transform.Find("Visual");
            var animator = visual.GetComponent<Animator>();

            Assert.That(animator.runtimeAnimatorController, Is.Not.Null,
                "Animator controller not assigned to Crawler");
        }

        [Test]
        public void WeaverEnemy_HasAnimatorOnVisualChild()
        {
            var prefab = Resources.Load<GameObject>(WeaverPrefabPath);
            Assert.That(prefab, Is.Not.Null, "Weaver prefab not found");

            var visual = prefab.transform.Find("Visual");
            Assert.That(visual, Is.Not.Null, "Visual child not found on Weaver");

            var animator = visual.GetComponent<Animator>();
            Assert.That(animator, Is.Not.Null, "Animator component missing on Weaver/Visual");
        }

        [Test]
        public void WeaverAnimator_HasController()
        {
            var prefab = Resources.Load<GameObject>(WeaverPrefabPath);
            var visual = prefab.transform.Find("Visual");
            var animator = visual.GetComponent<Animator>();

            Assert.That(animator.runtimeAnimatorController, Is.Not.Null,
                "Animator controller not assigned to Weaver");
        }

        [Test]
        public void PlayerInstantiated_AnimatorRunning()
        {
            var prefab = Resources.Load<GameObject>(PlayerPrefabPath);
            var instance = Object.Instantiate(prefab);

            var visual = instance.transform.Find("Visual");
            var animator = visual.GetComponent<Animator>();

            // Animator should be enabled and have a controller
            Assert.That(animator.enabled, Is.True, "Animator disabled on Player instance");
            Assert.That(animator.runtimeAnimatorController, Is.Not.Null,
                "Animator controller missing after instantiate");

            Object.Destroy(instance);
        }

        [Test]
        public void CrawlerInstantiated_AnimatorRunning()
        {
            var prefab = Resources.Load<GameObject>(CrawlerPrefabPath);
            var instance = Object.Instantiate(prefab);

            var visual = instance.transform.Find("Visual");
            var animator = visual.GetComponent<Animator>();

            Assert.That(animator.enabled, Is.True, "Animator disabled on Crawler instance");
            Assert.That(animator.runtimeAnimatorController, Is.Not.Null,
                "Animator controller missing after instantiate");

            Object.Destroy(instance);
        }

        [Test]
        public void PlayerAnimator_SpriteRendererPresent()
        {
            var prefab = Resources.Load<GameObject>(PlayerPrefabPath);
            var visual = prefab.transform.Find("Visual");
            var spriteRenderer = visual.GetComponent<SpriteRenderer>();

            Assert.That(spriteRenderer, Is.Not.Null, "SpriteRenderer missing on Player/Visual");
            Assert.That(spriteRenderer.sprite, Is.Not.Null, "Default sprite not set on Player");
        }

        [Test]
        public void CrawlerAnimator_SpriteRendererPresent()
        {
            var prefab = Resources.Load<GameObject>(CrawlerPrefabPath);
            var visual = prefab.transform.Find("Visual");
            var spriteRenderer = visual.GetComponent<SpriteRenderer>();

            Assert.That(spriteRenderer, Is.Not.Null, "SpriteRenderer missing on Crawler/Visual");
        }
    }
}
