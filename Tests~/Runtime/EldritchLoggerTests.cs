using EldritchGames.EldritchLogger.Builder;
using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Settings;
using Moq;
using NUnit.Framework;
using System.Linq;
using System.Threading.Tasks;
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
            Assert.That(Core.EldritchLogger.Instance, Is.SameAs(logger));
        }

        [Test]
        public void Dispose_ShouldClearInstance()
        {
            var settings = CreateSettings(ExportFormat.Json);
            var logger = new Core.EldritchLogger(settings);

            logger.Dispose();

            Assert.That(Core.EldritchLogger.Instance, Is.Null);
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

            var builder = logger.AtInfo(LogCategory.General);

            Assert.That(builder, Is.Not.Null);
        }

        [Test]
        public void AtWarning_ShouldReturnLogBuilder()
        {
            var settings = CreateSettings(ExportFormat.Json);
            using var logger = new Core.EldritchLogger(settings);

            var builder = logger.AtWarning(LogCategory.General);

            Assert.That(builder, Is.Not.Null);
        }

        [Test]
        public void AtError_ShouldReturnLogBuilder()
        {
            var settings = CreateSettings(ExportFormat.Json);
            using var logger = new Core.EldritchLogger(settings);

            var builder = logger.AtError(LogCategory.General);

            Assert.That(builder, Is.Not.Null);
        }

        [Test]
        public void AtCritical_ShouldReturnLogBuilder()
        {
            var settings = CreateSettings(ExportFormat.Json);
            using var logger = new Core.EldritchLogger(settings);

            var builder = logger.AtCritical(LogCategory.General);

            Assert.That(builder, Is.Not.Null);
        }

        [Test]
        public async Task LogBuilder_ShouldCallUnderlyingLoggerAsync()
        {
            var settings = CreateSettings(ExportFormat.Json);
            var mockLogger = new Mock<IEldritchLogger>();

            var builder = new LogBuilder(mockLogger.Object, LogLevel.Info, LogCategory.General);
            await builder.Log("Test message");

            mockLogger.Verify(l => l.Log(
                LogLevel.Info,
                LogCategory.General,
                "Test message",
                It.IsAny<System.Collections.Generic.Dictionary<string, object>>(),
                null), Times.Once);
        }
    }
}
