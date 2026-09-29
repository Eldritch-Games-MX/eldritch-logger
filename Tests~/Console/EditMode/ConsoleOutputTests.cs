using EldritchGames.EldritchLogger.Console.Output;
using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Domain;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EldritchGames.EldritchLogger.Console.Tests.EditMode
{
    public class ConsoleOutputTests
    {
        private sealed class CapturingLogger : IEldritchLogger
        {
            public readonly List<LogEntry> Entries = new();
            public bool IsEnabled(LogLevel level, LogCategory category) => true;
            public void Log(LogEntry entry) => Entries.Add(entry);
        }

        [Test]
        public void Write_ColorsWarningsAndErrors()
        {
            var view = new FakeView();
            var output = new ConsoleOutput(view);

            output.Info("i");
            output.Warn("w");
            output.Error("e");

            Assert.That(view.Lines[0], Is.EqualTo("i"));
            Assert.That(view.Lines[1], Does.StartWith("<color=").And.Contain(">w</color>"));
            Assert.That(view.Lines[2], Does.StartWith("<color=").And.Contain(">e</color>"));
        }

        [Test]
        public void Mirror_SendsMarkedEntriesToTheLogger()
        {
            var logger = new CapturingLogger();
            var output = new ConsoleOutput(new FakeView(), logger);

            output.Warn("careful");

            var entry = logger.Entries.Single();
            Assert.That(entry.Level, Is.EqualTo(LogLevel.Warning));
            Assert.That(entry.Category, Is.EqualTo(new LogCategory("Console")));
            Assert.That(entry.Properties.ContainsKey(ConsoleOutput.MirroredPropertyKey), Is.True);
        }

        [Test]
        public void ConsoleOutput_Clear_ClearsTheView_AndRequiresAView()
        {
            var view = new FakeView();
            new ConsoleOutput(view).Clear();
            Assert.That(view.Clears, Is.EqualTo(1));
            Assert.Throws<ArgumentNullException>(() => new ConsoleOutput(null));
        }
    }
}
