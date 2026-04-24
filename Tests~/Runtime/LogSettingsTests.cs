using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Settings;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Tests
{
    [TestFixture]
    public class LogSettingsTests
    {
        private LogSettings settings;

        [SetUp]
        public void Setup()
        {
            settings = ScriptableObject.CreateInstance<LogSettings>();
        }

        [Test]
        public void ApplyVerbosePreset_SetsDebugAndAllCategories()
        {
            settings.ApplyVerbosePreset();

            Assert.AreEqual(LogLevel.Debug, settings.logLevel);
            CollectionAssert.AreEquivalent(
                (LogCategory[])Enum.GetValues(typeof(LogCategory)),
                settings.enabledCategories);
        }

        [Test]
        public void ApplyNormalPreset_SetsInfoAndExpectedCategories()
        {
            settings.ApplyNormalPreset();

            Assert.AreEqual(LogLevel.Info, settings.logLevel);
            CollectionAssert.AreEquivalent(
                new[] { LogCategory.Gameplay, LogCategory.UI, LogCategory.Network },
                settings.enabledCategories);
        }

        [Test]
        public void ApplyProductionPreset_SetsWarningAndExpectedCategories()
        {
            settings.ApplyProductionPreset();

            Assert.AreEqual(LogLevel.Warning, settings.logLevel);
            CollectionAssert.AreEquivalent(
                new[] { LogCategory.Gameplay, LogCategory.Network },
                settings.enabledCategories);
        }

        [Test]
        public void EnableAllCategories_SetsAll()
        {
            settings.EnableAllCategories();

            CollectionAssert.AreEquivalent(
                (LogCategory[])Enum.GetValues(typeof(LogCategory)),
                settings.enabledCategories);
        }

        [Test]
        public void DisableAllCategories_ClearsAll()
        {
            settings.DisableAllCategories();

            Assert.IsEmpty(settings.enabledCategories);
        }
    }
}
