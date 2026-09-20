using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using EchoOfTheVoid.Core;
using EchoOfTheVoid.UI;

namespace EchoOfTheVoid.Tests
{
    public class BossBarTests
    {
        private GameObject _go;
        private BossHealthBar _bar;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("BossBar");
            _bar = _go.AddComponent<BossHealthBar>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_go);
        }

        [UnityTest]
        public IEnumerator Engaged_ShowsTheBarWithTheBossName()
        {
            yield return null;
            Assert.IsFalse(_bar.IsVisible, "Hidden until a boss fight starts");

            BossEvents.RaiseEngaged("sentinel_01", "SENTINEL-01", 600);
            Assert.IsTrue(_bar.IsVisible);
            Assert.AreEqual("SENTINEL-01", _bar.DisplayedName);
            Assert.AreEqual(1f, _bar.FillAmount);
        }

        [UnityTest]
        public IEnumerator HealthChanged_MovesTheFill_AndIgnoresOtherBosses()
        {
            yield return null;
            BossEvents.RaiseEngaged("sentinel_01", "SENTINEL-01", 600);

            BossEvents.RaiseHealthChanged("sentinel_01", 150, 600);
            Assert.AreEqual(0.25f, _bar.FillAmount, 0.001f);

            BossEvents.RaiseHealthChanged("someone_else", 1, 600);
            Assert.AreEqual(0.25f, _bar.FillAmount, 0.001f, "Another boss's health does not move this bar");
        }

        [UnityTest]
        public IEnumerator Defeated_EmptiesThenHides()
        {
            yield return null;
            BossEvents.RaiseEngaged("sentinel_01", "SENTINEL-01", 600);
            BossEvents.RaiseDefeated("sentinel_01");
            Assert.AreEqual(0f, _bar.FillAmount, 0.001f);
            Assert.IsTrue(_bar.IsVisible, "The bar lingers a moment on the last hit");

            yield return new WaitForSecondsRealtime(1.8f);
            Assert.IsFalse(_bar.IsVisible);
        }

        [UnityTest]
        public IEnumerator Reset_HidesAtOnce_AndTheNextFightStartsFull()
        {
            yield return null;
            BossEvents.RaiseEngaged("sentinel_01", "SENTINEL-01", 600);
            BossEvents.RaiseHealthChanged("sentinel_01", 100, 600);

            BossEvents.RaiseReset("sentinel_01"); // Kael died mid-fight
            Assert.IsFalse(_bar.IsVisible);

            BossEvents.RaiseEngaged("sentinel_01", "SENTINEL-01", 600);
            Assert.IsTrue(_bar.IsVisible);
            Assert.AreEqual(1f, _bar.FillAmount, "A rematch starts with a full bar");
        }
    }
}
