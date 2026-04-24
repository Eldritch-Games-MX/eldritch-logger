using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Domain;
using NUnit.Framework;
using System;

namespace EldritchGames.EldritchLogger.Tests
{
    [TestFixture]
    public class LogEntryFactoryTests
    {
        [Test]
        public void Create_ShouldBuildLogEntry()
        {
            var factory = new LogEntryFactory();
            var entry = factory.Create(LogLevel.Info, LogCategory.General, "Test", null, null);

            Assert.That(entry.Message, Is.EqualTo("Test"));
            Assert.That(entry.Level, Is.EqualTo(LogLevel.Info));
            Assert.That(entry.Category, Is.EqualTo(LogCategory.General));
            Assert.That(entry.Timestamp, Is.Not.EqualTo(default(DateTime)));
        }
    }
}
