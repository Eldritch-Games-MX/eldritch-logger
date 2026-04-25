using EldritchGames.EldritchLogger.Builder;
using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Settings;
using Moq;
using NUnit.Framework;
using System.Linq;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Tests
{
    [TestFixture]
    public class EldritchLoggerTests
    {
        private LogSettings CreateSettings(params ExportFormat[] formats)
        {
            var settings = ScriptableObject.CreateInstance<LogSettings>();
            settings.name = "TestLogger";
            settings.logLevel = LogLevel.Debug;
            settings.exportFormats = formats.ToList();
            settings.exportDirectory = "unused";
            settings.exportFileName = "session";
            return settings;
        }

        [Test]
        public void Constructor_ShouldInitializeSinks()
        {
            var settings = CreateSettings(ExportFormat.Json, ExportFormat.Xml);
            using var logger = new Core.EldritchLogger(settings);

            // Json + Xml + UnityConsole
            Assert.That(logger.Sinks.Count(), Is.EqualTo(3));
        }

        [Test]
        public void AtDebug_ShouldReturnLogBuilder()
        {
            var settings = CreateSettings(ExportFormat.Json);
            using var logger = new Core.EldritchLogger(settings);

            var builder = logger.AtDebug(LogCategory.General);

            Assert.That(builder, Is.Not.Null);
            Assert.That(builder.GetType().Name, Is.EqualTo("LogBuilder"));
        }

        [Test]
        public void AtInfo_ShouldReturnLogBuilder()
        {
            var settings = CreateSettings(ExportFormat.Json);
            using var logger = new Core.EldritchLogger(settings);

            Assert.That(logger.AtInfo(LogCategory.General), Is.Not.Null);
        }

        [Test]
        public void AtWarning_ShouldReturnLogBuilder()
        {
            var settings = CreateSettings(ExportFormat.Json);
            using var logger = new Core.EldritchLogger(settings);

            Assert.That(logger.AtWarning(LogCategory.General), Is.Not.Null);
        }

        [Test]
        public void AtError_ShouldReturnLogBuilder()
        {
            var settings = CreateSettings(ExportFormat.Json);
            using var logger = new Core.EldritchLogger(settings);

            Assert.That(logger.AtError(LogCategory.General), Is.Not.Null);
        }

        [Test]
        public void AtCritical_ShouldReturnLogBuilder()
        {
            var settings = CreateSettings(ExportFormat.Json);
            using var logger = new Core.EldritchLogger(settings);

            Assert.That(logger.AtCritical(LogCategory.General), Is.Not.Null);
        }

        [Test]
        public void LogBuilder_ShouldCallUnderlyingLogger()
        {
            var mockLogger = new Mock<IEldritchLogger>();

            var builder = new LogBuilder(mockLogger.Object, LogLevel.Info, LogCategory.General);
            builder.Log("Test message");

            mockLogger.Verify(l => l.Log(
                LogLevel.Info,
                LogCategory.General,
                "Test message",
                It.IsAny<System.Collections.Generic.Dictionary<string, object>>(),
                null), Times.Once);
        }
    }
}
