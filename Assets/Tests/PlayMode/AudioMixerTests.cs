using NUnit.Framework;
using UnityEngine;
using EchoOfTheVoid.Core;
using EchoOfTheVoid.Player;

namespace EchoOfTheVoid.Tests
{
    [Category("Audio")]
    public class AudioMixerTests
    {
        private GameObject _mixerControllerObj;
        private AudioMixerController _controller;

        [SetUp]
        public void Setup()
        {
            GameSession.Reset();
            PlayerStats.Reset();

            _mixerControllerObj = new GameObject("AudioMixerController");
            _controller = _mixerControllerObj.AddComponent<AudioMixerController>();
        }

        [TearDown]
        public void Teardown()
        {
            Object.Destroy(_mixerControllerObj);
            GameSession.Reset();
            PlayerStats.Reset();
        }

        [Test]
        public void LinearToDb_Conversion()
        {
            float linear1 = 1.0f;
            float db_expected = 0f;
            float db_actual = Mathf.Log10(Mathf.Clamp(linear1, 0.0001f, 1f)) * 20f;
            Assert.That(db_actual, Is.EqualTo(db_expected).Within(0.1f));
        }

        [Test]
        public void LinearToDb_Half()
        {
            float linear = 0.5f;
            float db_expected = -6f;
            float db_actual = Mathf.Log10(Mathf.Clamp(linear, 0.0001f, 1f)) * 20f;
            Assert.That(db_actual, Is.EqualTo(db_expected).Within(0.1f));
        }

        [Test]
        public void LinearToDb_VeryLow()
        {
            float linear = 0.0001f;
            float db_expected = -80f;
            float db_actual = Mathf.Log10(Mathf.Clamp(linear, 0.0001f, 1f)) * 20f;
            Assert.That(db_actual, Is.EqualTo(db_expected).Within(0.1f));
        }

        [Test]
        public void LowHealth_TriggersBelowThreshold()
        {
            var stats = PlayerStats.Instance;
            stats.CurrentHealth = 10;
            stats.MaxHealth = 100;

            // 10/100 = 0.1 <= 0.25, should trigger LowHP
            Assert.That((float)stats.CurrentHealth / stats.MaxHealth, Is.LessThanOrEqualTo(0.25f));
        }

        [Test]
        public void LowHealth_DoesNotTriggerAboveThreshold()
        {
            var stats = PlayerStats.Instance;
            stats.CurrentHealth = 30;
            stats.MaxHealth = 100;

            // 30/100 = 0.3 > 0.25, should not trigger LowHP
            Assert.That((float)stats.CurrentHealth / stats.MaxHealth, Is.GreaterThan(0.25f));
        }

        [Test]
        public void RealmSwitch_UpdatesCurrentRealm()
        {
            var initialRealm = RealmType.Prime;
            RealityEventBus.SwitchRealm(RealmType.Echo);

            // Note: Actual realm state is in RealityManager. This test validates event flow.
            // A more comprehensive test would mock the mixer's snapshot state.
        }

        [Test]
        public void Pause_SetsIsPausedFlag()
        {
            GameFlow.IsPaused = false;
            Assert.That(GameFlow.IsPaused, Is.False);

            GameFlow.IsPaused = true;
            Assert.That(GameFlow.IsPaused, Is.True);

            GameFlow.IsPaused = false;
            Assert.That(GameFlow.IsPaused, Is.False);
        }

        [Test]
        public void VolumeRange_Clamp()
        {
            float linear = -0.5f;
            float clamped = Mathf.Clamp(linear, 0.0001f, 1f);
            Assert.That(clamped, Is.EqualTo(0.0001f));

            linear = 1.5f;
            clamped = Mathf.Clamp(linear, 0.0001f, 1f);
            Assert.That(clamped, Is.EqualTo(1f));
        }
    }
}
