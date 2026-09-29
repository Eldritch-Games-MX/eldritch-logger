using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Domain;
using EldritchGames.EldritchLogger.Mapper;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;

namespace EldritchGames.EldritchLogger.Tests
{
    public class LogEntryMapperTests
    {
        private static Exception Thrown()
        {
            try { throw new InvalidOperationException("broken"); }
            catch (Exception ex) { return ex; }
        }

        [Test]
        public void ToDto_CopiesFieldsAndStringifiesProperties()
        {
            var entry = new LogEntry(LogLevel.Error, "Loot", "msg",
                new Dictionary<string, object> { ["Count"] = 3, ["Null"] = null },
                timestampUtc: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

            var dto = new LogEntryMapper().ToDto(entry);

            Assert.That(dto.Level, Is.EqualTo(LogLevel.Error));
            Assert.That(dto.Category, Is.EqualTo("Loot"));
            Assert.That(dto.GetMetadata("Count"), Is.EqualTo("3"));
            Assert.That(dto.GetMetadata("Null"), Is.Null);
            Assert.That(dto.Exception, Is.Null);
            Assert.That(dto.Timestamp, Is.EqualTo(entry.TimestampUtc));
        }

        [Test]
        public void ToDto_DescribesException_AndFiltersLoggerFrames()
        {
            // Test frames live in EldritchGames.EldritchLogger.Tests, so filtering removes them.
            var filtered = new LogEntryMapper(filterLoggerFrames: true)
                .ToDto(new LogEntry(LogLevel.Error, default, "m", exception: Thrown()));
            var unfiltered = new LogEntryMapper(filterLoggerFrames: false)
                .ToDto(new LogEntry(LogLevel.Error, default, "m", exception: Thrown()));

            Assert.That(filtered.Exception, Is.EqualTo("InvalidOperationException: broken"));
            Assert.That(unfiltered.Exception, Does.StartWith("InvalidOperationException: broken\n"));
            Assert.That(unfiltered.Exception, Does.Contain("LogEntryMapperTests"));
        }

        [Test]
        public void PropertyValues_UseTheInvariantCulture()
        {
            var previous = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");
            try
            {
                var entry = new LogEntry(LogLevel.Info, default, "m",
                    new Dictionary<string, object> { ["LatencyMs"] = 182.4f, ["Ratio"] = 0.5, ["Name"] = "Bob", ["Null"] = null });

                var dto = new LogEntryMapper().ToDto(entry);

                Assert.That(dto.GetMetadata("LatencyMs"), Is.EqualTo("182.4"));
                Assert.That(dto.GetMetadata("Ratio"), Is.EqualTo("0.5"));
                Assert.That(dto.GetMetadata("Name"), Is.EqualTo("Bob"));
                Assert.That(dto.GetMetadata("Null"), Is.Null);
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = previous;
            }
        }
    }
}
