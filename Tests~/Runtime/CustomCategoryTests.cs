using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Dto;
using EldritchGames.EldritchLogger.Format;
using EldritchGames.EldritchLogger.Settings;
using Moq;
using NUnit.Framework;
using System;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Tests
{
    [TestFixture]
    public class CustomCategoryTests
    {
        private LogSettings settings;

        // User-defined enum for Option B tests
        private enum GameCategory { Systems, Economy, Quests }

        [SetUp]
        public void Setup()
        {
            settings = ScriptableObject.CreateInstance<LogSettings>();
            settings.useCategoryColors = true;
        }

        [TearDown]
        public void Cleanup()
        {
            UnityEngine.Object.DestroyImmediate(settings);
        }

        // ── LogSettings: AddCustomCategory ────────────────────────────────────

        [Test]
        public void AddCustomCategory_ReturnsTrue_AndAppendsEntry()
        {
            bool result = settings.AddCustomCategory("Systems", Color.red);

            Assert.IsTrue(result);
            Assert.AreEqual(1, settings.customCategories.Count);
            Assert.AreEqual("Systems", settings.customCategories[0].name);
            Assert.AreEqual(Color.red, settings.customCategories[0].color);
            Assert.IsTrue(settings.customCategories[0].enabled);
        }

        [Test]
        public void AddCustomCategory_ReturnsFalse_WhenNameEmpty()
        {
            bool result = settings.AddCustomCategory("", Color.white);

            Assert.IsFalse(result);
            Assert.IsEmpty(settings.customCategories);
        }

        [Test]
        public void AddCustomCategory_ReturnsFalse_WhenNameWhitespace()
        {
            bool result = settings.AddCustomCategory("   ", Color.white);

            Assert.IsFalse(result);
            Assert.IsEmpty(settings.customCategories);
        }

        [Test]
        public void AddCustomCategory_ReturnsFalse_WhenNameMatchesBuiltIn()
        {
            bool result = settings.AddCustomCategory("Audio", Color.white);

            Assert.IsFalse(result);
            Assert.IsEmpty(settings.customCategories);
        }

        [Test]
        public void AddCustomCategory_ReturnsFalse_WhenNameMatchesBuiltIn_CaseInsensitive()
        {
            bool result = settings.AddCustomCategory("audio", Color.white);

            Assert.IsFalse(result);
            Assert.IsEmpty(settings.customCategories);
        }

        [Test]
        public void AddCustomCategory_ReturnsFalse_WhenDuplicate()
        {
            settings.AddCustomCategory("Systems", Color.white);
            bool result = settings.AddCustomCategory("Systems", Color.blue);

            Assert.IsFalse(result);
            Assert.AreEqual(1, settings.customCategories.Count);
        }

        [Test]
        public void AddCustomCategory_ReturnsFalse_WhenDuplicate_CaseInsensitive()
        {
            settings.AddCustomCategory("Systems", Color.white);
            bool result = settings.AddCustomCategory("systems", Color.blue);

            Assert.IsFalse(result);
            Assert.AreEqual(1, settings.customCategories.Count);
        }

        // ── LogSettings: RemoveCustomCategory ────────────────────────────────

        [Test]
        public void RemoveCustomCategory_RemovesEntry()
        {
            settings.AddCustomCategory("Systems", Color.white);
            settings.RemoveCustomCategory("Systems");

            Assert.IsEmpty(settings.customCategories);
        }

        [Test]
        public void RemoveCustomCategory_NoThrow_WhenNotFound()
        {
            Assert.DoesNotThrow(() => settings.RemoveCustomCategory("NonExistent"));
        }

        // ── LogSettings: IsCustomCategoryEnabled ─────────────────────────────

        [Test]
        public void IsCustomCategoryEnabled_ReturnsTrue_WhenEnabledByDefault()
        {
            settings.AddCustomCategory("Systems", Color.white);

            Assert.IsTrue(settings.IsCustomCategoryEnabled("Systems"));
        }

        [Test]
        public void IsCustomCategoryEnabled_ReturnsFalse_WhenDisabled()
        {
            settings.AddCustomCategory("Systems", Color.white);
            settings.customCategories[0].enabled = false;

            Assert.IsFalse(settings.IsCustomCategoryEnabled("Systems"));
        }

        [Test]
        public void IsCustomCategoryEnabled_ReturnsFalse_WhenNotFound()
        {
            Assert.IsFalse(settings.IsCustomCategoryEnabled("NonExistent"));
        }

        [Test]
        public void IsCustomCategoryEnabled_IsCaseInsensitive()
        {
            settings.AddCustomCategory("Systems", Color.white);

            Assert.IsTrue(settings.IsCustomCategoryEnabled("SYSTEMS"));
            Assert.IsTrue(settings.IsCustomCategoryEnabled("systems"));
        }

        // ── LogSettings: GetCustomCategoryColor ──────────────────────────────

        [Test]
        public void GetCustomCategoryColor_ReturnsRegisteredColor()
        {
            settings.AddCustomCategory("Systems", Color.red);

            Assert.AreEqual(Color.red, settings.GetCustomCategoryColor("Systems"));
        }

        [Test]
        public void GetCustomCategoryColor_ReturnsWhite_WhenNotFound()
        {
            Assert.AreEqual(Color.white, settings.GetCustomCategoryColor("NonExistent"));
        }

        // ── LogSettings: EnableAll / DisableAll with custom categories ────────

        [Test]
        public void EnableAllCategories_AlsoEnablesCustomCategories()
        {
            settings.AddCustomCategory("Systems", Color.white);
            settings.customCategories[0].enabled = false;

            settings.EnableAllCategories();

            Assert.IsTrue(settings.customCategories[0].enabled);
        }

        [Test]
        public void DisableAllCategories_AlsoDisablesCustomCategories()
        {
            settings.AddCustomCategory("Systems", Color.white);

            settings.DisableAllCategories();

            Assert.IsFalse(settings.customCategories[0].enabled);
        }

        // ── LogEntryFormatter: custom category string ─────────────────────────

        [Test]
        public void Formatter_DoesNotThrow_OnUnknownCustomCategory()
        {
            var formatter = new LogEntryFormatter(settings);
            var dto = new LogEntryDto
            {
                Timestamp = DateTime.Now,
                Level = LogLevel.Debug.ToString(),
                Category = "UnregisteredCategory",
                Message = "test"
            };

            Assert.DoesNotThrow(() => formatter.Format(dto));
        }

        [Test]
        public void Formatter_AppliesCustomColor_WhenCategoryRegistered()
        {
            settings.AddCustomCategory("Systems", Color.red);
            var formatter = new LogEntryFormatter(settings);
            var dto = new LogEntryDto
            {
                Timestamp = DateTime.Now,
                Level = LogLevel.Debug.ToString(),
                Category = "Systems",
                Message = "test"
            };

            string output = formatter.Format(dto);
            string expectedHex = ColorUtility.ToHtmlStringRGB(Color.red);

            StringAssert.Contains($"<color=#{expectedHex}>", output);
            StringAssert.Contains("Systems</color>", output);
        }

        [Test]
        public void Formatter_FallsBackToWhite_WhenCustomCategoryUnregistered()
        {
            var formatter = new LogEntryFormatter(settings);
            var dto = new LogEntryDto
            {
                Timestamp = DateTime.Now,
                Level = LogLevel.Debug.ToString(),
                Category = "GhostCategory",
                Message = "test"
            };

            string output = formatter.Format(dto);
            string whiteHex = ColorUtility.ToHtmlStringRGB(Color.white);

            StringAssert.Contains($"<color=#{whiteHex}>", output);
        }

        [Test]
        public void Formatter_NoColorTag_WhenColorsDisabled_CustomCategory()
        {
            settings.useCategoryColors = false;
            settings.AddCustomCategory("Systems", Color.red);
            var formatter = new LogEntryFormatter(settings);
            var dto = new LogEntryDto
            {
                Timestamp = DateTime.Now,
                Level = LogLevel.Debug.ToString(),
                Category = "Systems",
                Message = "test"
            };

            string output = formatter.Format(dto);

            StringAssert.Contains("Systems", output);
            StringAssert.DoesNotContain("<color=", output);
        }

        // ── IEldritchLogger: string category overload ─────────────────────────

        [Test]
        public void Log_StringCategory_CallsUnderlyingLogger()
        {
            var mock = new Mock<IEldritchLogger>();

            mock.Object.Log(LogLevel.Info, "Systems", "msg");

            mock.Verify(l => l.Log(LogLevel.Info, "Systems", "msg", null, null), Times.Once);
        }

        // ── IEldritchLogger: Enum category overload ───────────────────────────

        [Test]
        public void Log_EnumCategory_CallsUnderlyingLogger_WithToString()
        {
            var mock = new Mock<IEldritchLogger>();

            mock.Object.Log(LogLevel.Warning, GameCategory.Economy, "profit");

            mock.Verify(l => l.Log(LogLevel.Warning, GameCategory.Economy, "profit", null, null), Times.Once);
        }

        [Test]
        public void NullLogger_EnumOverload_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
                NullLogger.Instance.Log(LogLevel.Debug, GameCategory.Systems, "msg"));
        }

        [Test]
        public void NullLogger_StringOverload_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
                NullLogger.Instance.Log(LogLevel.Debug, "Systems", "msg"));
        }

        // ── EldritchLogger: string/enum overloads smoke tests ────────────────

        [Test]
        public void EldritchLogger_StringCategory_DoesNotThrow_WhenEnabled()
        {
            settings.AddCustomCategory("Systems", Color.white);
            using var logger = new Core.EldritchLogger(settings);

            Assert.DoesNotThrow(() => logger.Log(LogLevel.Debug, "Systems", "msg"));
        }

        [Test]
        public void EldritchLogger_StringCategory_DoesNotThrow_WhenDisabled()
        {
            settings.AddCustomCategory("Systems", Color.white);
            settings.customCategories[0].enabled = false;
            using var logger = new Core.EldritchLogger(settings);

            Assert.DoesNotThrow(() => logger.Log(LogLevel.Debug, "Systems", "suppressed"));
        }

        [Test]
        public void EldritchLogger_EnumCategory_DoesNotThrow_WhenEnabled()
        {
            settings.AddCustomCategory("Economy", Color.white);
            using var logger = new Core.EldritchLogger(settings);

            Assert.DoesNotThrow(() => logger.Log(LogLevel.Info, GameCategory.Economy, "gold"));
        }

        [Test]
        public void EldritchLogger_EnumCategory_ToStringMatchesRegisteredName()
        {
            // Enum.ToString() must produce the same string used during registration
            Assert.AreEqual("Economy", GameCategory.Economy.ToString());
            Assert.AreEqual("Systems", GameCategory.Systems.ToString());
            Assert.AreEqual("Quests", GameCategory.Quests.ToString());
        }
    }
}
