using EldritchGames.EldritchLogger.Console.Loader;
using EldritchGames.EldritchLogger.Console.Logging;
using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Dto;
using EldritchGames.EldritchLogger.Settings;
using Moq;
using NUnit.Framework;
using System;
using System.Threading.Tasks;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Console.Tests.EditMode.Logging
{
    [TestFixture]
    public class ConsoleLogSinkTests
    {
        private LogSettings settings;
        private Mock<IConsoleView> viewMock;
        private ConsoleLogSink sink;

        [SetUp]
        public void SetUp()
        {
            settings = ScriptableObject.CreateInstance<LogSettings>();
            settings.useCategoryColors = false;
            viewMock = new Mock<IConsoleView>();
            sink = new ConsoleLogSink(settings);
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(settings);
        }

        private static LogEntryDto Entry(string message) => new()
        {
            Timestamp = DateTime.Now,
            Level = "Info",
            Category = "General",
            Message = message
        };

        [Test]
        public void Constructor_NullSettings_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new ConsoleLogSink(null));
        }

        [Test]
        public void Category_IsRuntime()
        {
            Assert.That(sink.Category, Is.EqualTo(SinkCategory.Runtime));
        }

        [Test]
        public void OnLogReceived_DoesNotWriteUntilFlush()
        {
            sink.OnLogReceived(Entry("hello"));

            viewMock.Verify(v => v.AppendLog(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void Flush_AppendsEachFormattedEntryOnce()
        {
            sink.OnLogReceived(Entry("first"));
            sink.OnLogReceived(Entry("second"));

            sink.Flush(viewMock.Object);
            sink.Flush(viewMock.Object);

            viewMock.Verify(v => v.AppendLog(It.Is<string>(s => s.Contains("first") && s.Contains("[Info]"))), Times.Once);
            viewMock.Verify(v => v.AppendLog(It.Is<string>(s => s.Contains("second"))), Times.Once);
        }

        [Test]
        public void Flush_IncludesEntriesQueuedFromBackgroundThread()
        {
            Task.Run(() => sink.OnLogReceived(Entry("threaded"))).Wait();

            sink.Flush(viewMock.Object);

            viewMock.Verify(v => v.AppendLog(It.Is<string>(s => s.Contains("threaded"))), Times.Once);
        }
    }
}
