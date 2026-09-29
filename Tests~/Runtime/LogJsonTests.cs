using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Dto;
using NUnit.Framework;
using System;

namespace EldritchGames.EldritchLogger.Tests
{
    public class LogJsonTests
    {
        [Test]
        public void LogJson_RoundTripsEntries()
        {
            var original = new LogEntryDto { Timestamp = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc), Level = LogLevel.Error, Category = "Net", Message = "m" };

            var json = LogJson.Serialize(original);
            var back = LogJson.Deserialize(json);

            Assert.That(json, Does.Contain("\"Level\":\"Error\"").And.Not.Contain("Exception"), "enum as text, nulls omitted");
            Assert.That(back.Level, Is.EqualTo(LogLevel.Error));
            Assert.That(back.Timestamp, Is.EqualTo(original.Timestamp));
            Assert.That(back.Timestamp.Kind, Is.EqualTo(DateTimeKind.Utc));
        }
    }
}
