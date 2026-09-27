using EldritchGames.EldritchLogger.Console.Settings;
using EldritchGames.EldritchLogger.Console.Commands;
using EldritchGames.EldritchLogger.Console.Core;
using EldritchGames.EldritchLogger.Console.Loader;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Console.Tests.EditMode.Command
{
    [TestFixture]
    public class ThemeCommandTests
    {
        private ThemeCommand themeCommand;
        private Mock<IConsoleThemeApplier> mockApplier;
        private Mock<IThemeLoader> mockLoader;
        private List<string> logMessages;
        private List<string> warnings;

        [SetUp]
        public void SetUp()
        {
            mockApplier = new Mock<IConsoleThemeApplier>();
            mockLoader = new Mock<IThemeLoader>();
            themeCommand = new ThemeCommand(mockApplier.Object, mockLoader.Object);

            logMessages = new List<string>();
            warnings = new List<string>();
            Application.logMessageReceived += CaptureLog;
        }

        [TearDown]
        public void TearDown()
        {
            Application.logMessageReceived -= CaptureLog;
        }

        private void CaptureLog(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Log)
                logMessages.Add(condition);
            else if (type == LogType.Warning)
                warnings.Add(condition);
        }

        [Test]
        public void Name_ReturnsTheme()
        {
            Assert.AreEqual("theme", themeCommand.Name);
        }

        [Test]
        public void Description_ReturnsExpectedText()
        {
            Assert.AreEqual("Changes the console theme. Usage: theme <name>", themeCommand.Description);
        }

        [Test]
        public void ExpectedArgs_DefinesSingleOptionalString()
        {
            var args = themeCommand.ExpectedArgs;
            Assert.AreEqual(1, args.Length);
            Assert.AreEqual("name", args[0].Name);
            Assert.AreEqual("string", args[0].Type);
            Assert.IsFalse(args[0].Required);
        }

        [Test]
        public void Execute_NoArgs_NoThemesFound_LogsMessage()
        {
            mockLoader.Setup(l => l.LoadAllThemes()).Returns(new ConsoleTheme[0]);

            themeCommand.Execute(new string[0]);

            Assert.IsTrue(logMessages.Exists(m => m.Contains("No themes found")));
        }

        [Test]
        public void Execute_NoArgs_ListsAvailableThemes()
        {
            var themeA = ScriptableObject.CreateInstance<ConsoleTheme>();
            themeA.name = "DarkTheme";
            var themeB = ScriptableObject.CreateInstance<ConsoleTheme>();
            themeB.name = "LightTheme";

            mockLoader.Setup(l => l.LoadAllThemes()).Returns(new[] { themeA, themeB });

            themeCommand.Execute(new string[0]);

            Assert.IsTrue(logMessages.Exists(m => m.Contains("Available themes")));
            Assert.IsTrue(logMessages.Exists(m => m.Contains("DarkTheme")));
            Assert.IsTrue(logMessages.Exists(m => m.Contains("LightTheme")));
        }

        [Test]
        public void Execute_InvalidTheme_ShowsWarning()
        {
            mockLoader.Setup(l => l.LoadTheme("NonExistentTheme")).Returns((ConsoleTheme)null);

            themeCommand.Execute(new[] { "NonExistentTheme" });

            Assert.IsTrue(warnings.Exists(w => w.Contains("not found")));
            mockApplier.Verify(a => a.ApplyTheme(It.IsAny<ConsoleTheme>()), Times.Never);
        }

        [Test]
        public void Execute_ValidTheme_CallsApplyTheme()
        {
            var theme = ScriptableObject.CreateInstance<ConsoleTheme>();
            theme.name = "TestTheme";

            mockLoader.Setup(l => l.LoadTheme("TestTheme")).Returns(theme);

            themeCommand.Execute(new[] { "TestTheme" });

            mockApplier.Verify(a => a.ApplyTheme(theme), Times.Once);
            Assert.IsTrue(logMessages.Exists(m => m.Contains("Theme 'TestTheme' applied.")));
        }
    }
}
