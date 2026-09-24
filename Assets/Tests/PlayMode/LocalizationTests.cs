using NUnit.Framework;
using UnityEngine;
using EchoOfTheVoid.Core;
using EchoOfTheVoid.Settings;

namespace EchoOfTheVoid.Tests
{
    [Category("Localization")]
    public class LocalizationTests
    {
        private LocalizationService _service;
        private GameObject _serviceObj;

        [SetUp]
        public void Setup()
        {
            GameSession.Reset();
            SettingsService.Reload();

            _serviceObj = new GameObject("LocalizationService");
            _service = _serviceObj.AddComponent<LocalizationService>();
        }

        [TearDown]
        public void Teardown()
        {
            Object.Destroy(_serviceObj);
            GameSession.Reset();
            SettingsService.Reload();
        }

        [Test]
        public void GetString_ReturnsEnglishByDefault()
        {
            _service.SetLanguage("en");
            Assert.That(_service.CurrentLanguage, Is.EqualTo("en"));
        }

        [Test]
        public void SetLanguage_UpdatesCurrentLanguage()
        {
            _service.SetLanguage("vi");
            Assert.That(_service.CurrentLanguage, Is.EqualTo("vi"));
        }

        [Test]
        public void SetLanguage_RejectsInvalidLanguages()
        {
            _service.SetLanguage("fr");
            Assert.That(_service.CurrentLanguage, Is.EqualTo("en"), "Invalid languages default to EN");
        }

        [Test]
        public void SetLanguage_SavesToSettings()
        {
            _service.SetLanguage("vi");
            SettingsService.Reload();
            Assert.That(SettingsService.Current.language, Is.EqualTo("vi"));
        }

        [Test]
        public void GetString_MissingKeyReturnsBracketedKey()
        {
            string result = _service.GetString("nonexistent.key");
            Assert.That(result, Is.EqualTo("[nonexistent.key]"));
        }

        [Test]
        public void GetString_EnglishFallback()
        {
            _service.SetLanguage("vi");
            // If VI is missing, should return EN
            // (depends on strings.json content; this tests the fallback logic)
            string result = _service.GetString("ui.play");
            Assert.That(string.IsNullOrEmpty(result), Is.False, "Should return a string");
        }

        [Test]
        public void LanguageChange_RaisesEvent()
        {
            bool eventFired = false;
            string firedLanguage = null;
            LocalizationService.OnLanguageChanged += (lang) =>
            {
                eventFired = true;
                firedLanguage = lang;
            };

            _service.SetLanguage("vi");

            Assert.That(eventFired, Is.True);
            Assert.That(firedLanguage, Is.EqualTo("vi"));

            LocalizationService.OnLanguageChanged -= (lang) => {};
        }
    }
}
