using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Domain;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace EldritchGames.EldritchLogger.Tests
{
    public class LogEntryTests
    {
        [Test]
        public void WithMethods_ReturnCopies_AndLeaveTheOriginalUnchanged()
        {
            var original = new LogEntry(LogLevel.Warning, "Net", "m", new Dictionary<string, object> { ["A"] = 1 });

            var stamped = original.WithTimestamp(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            var extended = original.WithProperty("B", 2);

            Assert.That(original.TimestampUtc, Is.EqualTo(default(DateTime)));
            Assert.That(original.Properties.ContainsKey("B"), Is.False);
            Assert.That(stamped.TimestampUtc.Year, Is.EqualTo(2026));
            Assert.That(extended.Properties["A"], Is.EqualTo(1));
            Assert.That(extended.Properties["B"], Is.EqualTo(2));
            Assert.That(extended.Level, Is.EqualTo(LogLevel.Warning));
            Assert.That(extended.Category, Is.EqualTo(new LogCategory("Net")));
        }

        [Test]
        public void Defaults_AreNeverNull()
        {
            var entry = new LogEntry(LogLevel.Info, default, null);
            Assert.That(entry.Message, Is.EqualTo(string.Empty));
            Assert.That(entry.Properties, Is.Empty);
            Assert.That(entry.Category, Is.EqualTo(LogCategory.General));
        }
    }
}
