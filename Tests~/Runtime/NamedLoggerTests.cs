using EldritchGames.EldritchLogger.Core;
using NUnit.Framework;
using System;

namespace EldritchGames.EldritchLogger.Tests
{
    public class NamedLoggerTests
    {
        [Test]
        public void NamedLogger_ForwardsIsEnabled_AndIgnoresNullEntries()
        {
            var sink = new RecordingSink(LogLevel.Warning);
            using var root = new EldritchLoggerBuilder().AddSink(sink).Build();
            var named = new NamedLogger(root, "Named");

            Assert.That(named.IsEnabled(LogLevel.Info, LogCategory.General), Is.False);
            Assert.That(named.IsEnabled(LogLevel.Error, LogCategory.General), Is.True);
            Assert.DoesNotThrow(() => named.Log(null));
            Assert.Throws<ArgumentNullException>(() => new NamedLogger(null, "x"));
        }
    }
}
