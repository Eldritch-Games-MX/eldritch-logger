using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Dto;
using EldritchGames.EldritchLogger.Pipeline;
using EldritchGames.EldritchLogger.Sinks;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace EldritchGames.EldritchLogger.Tests
{
    public class LogDispatcherTests
    {
        private sealed class ProbeSink : ILogSink
        {
            public bool SawDispatching;
            public string Name => "Probe";
            public LogLevel MinimumLevel => LogLevel.Debug;
            public void Emit(LogEntryDto entry) => SawDispatching = LogDispatcher.IsDispatching;
        }

        [Test]
        public void IsDispatching_IsTrueOnlyWhileSinksRun()
        {
            var probe = new ProbeSink();

            new LogDispatcher().Dispatch(new LogEntryDto(), new ILogSink[] { probe });

            Assert.That(probe.SawDispatching, Is.True);
            Assert.That(LogDispatcher.IsDispatching, Is.False);
        }

        [Test]
        public void ThrowingSink_IsReported_AndStateIsReset()
        {
            using var capture = new SelfLogCapture();
            var after = new RecordingSink();

            new LogDispatcher().Dispatch(new LogEntryDto(), new ILogSink[] { new ThrowingSink(), after });

            Assert.That(after.Entries, Has.Count.EqualTo(1));
            Assert.That(capture.Messages, Has.Count.EqualTo(1));
            Assert.That(LogDispatcher.IsDispatching, Is.False);
        }

        [Test]
        public void SinksBelowTheirMinimumLevel_AreSkipped()
        {
            var errors = new RecordingSink(LogLevel.Error);

            new LogDispatcher().Dispatch(new LogEntryDto { Level = LogLevel.Warning }, new ILogSink[] { errors });

            Assert.That(errors.Entries, Is.Empty);
        }
    }
}
