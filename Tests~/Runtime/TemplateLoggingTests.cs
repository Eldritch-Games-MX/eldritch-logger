using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Dto;
using EldritchGames.EldritchLogger.Formatting;
using NUnit.Framework;
using System;
using System.Linq;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Tests
{
    public class TemplateLoggingTests
    {
        private sealed class CountingToString
        {
            public int Calls;
            public override string ToString() { Calls++; return "counted"; }
        }

        private RecordingSink sink;
        private Core.EldritchLogger logger;

        [SetUp]
        public void SetUp()
        {
            sink = new RecordingSink(LogLevel.Info);
            logger = new EldritchLoggerBuilder().AddSink(sink).WithClock(new FakeClock()).Build();
        }

        [TearDown]
        public void TearDown() => logger.Dispose();

        [Test]
        public void Shorthands_RenderAndRecordTheTemplate()
        {
            logger.Info("Player {Name} joined", "Bob");
            logger.Error(new InvalidOperationException("x"), "Save {Slot} failed", 3);

            Assert.That(sink.Entries[0].Message, Is.EqualTo("Player Bob joined"));
            Assert.That(sink.Entries[0].GetMetadata("Name"), Is.EqualTo("Bob"));
            Assert.That(sink.Entries[0].GetMetadata(LogPropertyKeys.MessageTemplate), Is.EqualTo("Player {Name} joined"));
            Assert.That(sink.Entries[1].Level, Is.EqualTo(LogLevel.Error));
            Assert.That(sink.Entries[1].Exception, Does.StartWith("InvalidOperationException: x"));
        }

        [Test]
        public void Builder_Template_KeepsOtherProperties()
        {
            logger.AtWarning(LogCategory.Network).AddKeyValue("Peer", 7).Log("Lost {Packets} packets", 3);

            var entry = sink.Entries[0];
            Assert.That(entry.Message, Is.EqualTo("Lost 3 packets"));
            Assert.That(entry.Category, Is.EqualTo("Network"));
            Assert.That(entry.GetMetadata("Peer"), Is.EqualTo("7"));
            Assert.That(entry.GetMetadata("Packets"), Is.EqualTo("3"));
        }

        [Test]
        public void DisabledLevels_DoNotRenderArguments()
        {
            var arg = new CountingToString();

            logger.Debug("Value {V}", arg);              // below the sink's Info minimum
            logger.AtDebug().Log("Value {V}", arg);

            Assert.That(arg.Calls, Is.EqualTo(0));
            Assert.That(sink.Entries, Is.Empty);
        }

        [Test]
        public void TextFormatter_DoesNotRepeatTemplateHoles()
        {
            logger.AtInfo().AddKeyValue("Extra", 1).Log("Player {Name} joined", "Bob");

            var text = new Formatting.TextLogFormatter(null, richText: false).Format(sink.Entries[0]);

            Assert.That(text, Does.EndWith("Player Bob joined Extra=1"));
            Assert.That(text, Does.Not.Contain("MessageTemplate").And.Not.Contain("Name="));
        }

        [Test]
        public void Builder_PlainMessage_IsNotTreatedAsTemplate()
        {
            logger.AtInfo().Log("json {\"a\": 1}");
            Assert.That(sink.Entries[0].Message, Is.EqualTo("json {\"a\": 1}"));
            Assert.That(sink.Entries[0].GetMetadata(LogPropertyKeys.MessageTemplate), Is.Null);
        }

        private LogEntryDto Last => sink.Entries.Last();

        private static string Text(LogEntryDto entry) => new TextLogFormatter(null, richText: false).Format(entry);

        private sealed class Unprintable
        {
            public override string ToString() => throw new InvalidOperationException("no");
        }

        // 10 ---------------------------------------------------------------------------------------------

        [Test]
        public void LogTemplate_WithException_GoesThroughTheBuilder()
        {
            var sink = new RecordingSink();
            using var logger = new EldritchLoggerBuilder().AddSink(sink).Build();

            logger.LogTemplate(LogLevel.Warning, LogCategory.AI, new InvalidOperationException("x"), "Path {Id} failed", 3);

            var e = sink.Entries.Single();
            Assert.That(e.Message, Is.EqualTo("Path 3 failed"));
            Assert.That(e.Category, Is.EqualTo("AI"));
            Assert.That(e.Exception, Does.StartWith("InvalidOperationException: x"));
            Assert.That(e.GetMetadata(LogPropertyKeys.MessageTemplate), Is.EqualTo("Path {Id} failed"));
        }

        [Test]
        public void TrailingExceptions_WithoutAHole_BecomeTheEntrysException()
        {
            var sink = new RecordingSink();
            using var logger = new EldritchLoggerBuilder().AddSink(sink).Build();
            var ex = new InvalidOperationException("disk");

            logger.Error("Save failed", ex);
            logger.Error("Save {Slot} failed", 3, ex);
            logger.Info("Caught {Error}", ex); // the hole takes it: it is a value, not the entry's exception

            Assert.That(sink.Entries[0].Message, Is.EqualTo("Save failed"));
            Assert.That(sink.Entries[0].Exception, Does.StartWith("InvalidOperationException: disk"));
            Assert.That(sink.Entries[1].Message, Is.EqualTo("Save 3 failed"));
            Assert.That(sink.Entries[1].Exception, Does.StartWith("InvalidOperationException: disk"));
            Assert.That(sink.Entries[2].Exception, Is.Null);
            Assert.That(sink.Entries[2].Message, Does.Contain("disk"));
        }

        [Test]
        public void TrailingException_DoesNotReplaceAnExplicitOne()
        {
            var sink = new RecordingSink();
            using var logger = new EldritchLoggerBuilder().AddSink(sink).Build();

            logger.Error(new ArgumentException("explicit"), "Failed", new InvalidOperationException("trailing"));

            Assert.That(sink.Entries[0].Exception, Does.StartWith("ArgumentException: explicit"));
        }

        [Test]
        public void InvalidHoleFormat_IsIgnored_InsteadOfThrowing()
        {
            Assert.DoesNotThrow(() => logger.Info("HP {Hp:D}", 3.5f));
            Assert.That(Last.Message, Is.EqualTo("HP 3.5"));
        }

        [Test]
        public void ThrowingToString_IsShownAsTheTypeName()
        {
            Assert.DoesNotThrow(() => logger.Info("Value {Value}", new Unprintable()));
            Assert.That(Last.Message, Is.EqualTo("Value <Unprintable>"));
            Assert.That(Last.GetMetadata("Value"), Is.EqualTo("<Unprintable>"));

            Assert.DoesNotThrow(() => logger.AtInfo().AddKeyValue("Other", new Unprintable()).Log("plain"));
            Assert.That(Last.GetMetadata("Other"), Is.EqualTo("<Unprintable>"));
        }

        [Test]
        public void HoleWithoutArgument_KeepsAPropertyOfTheSameName()
        {
            logger.AtInfo().AddKeyValue("Player", "bob").Log("{Player} joined");

            Assert.That(Last.Message, Is.EqualTo("{Player} joined"));
            Assert.That(Text(Last), Does.Contain("Player=bob"));
        }

        [Test]
        public void FilledHoles_AreLeftOutOfTextOutput_UnfilledOnesAreNot()
        {
            using (logger.BeginScope("B", "scoped"))
                logger.Info("{A} and {B}", 1);

            Assert.That(Last.Message, Is.EqualTo("1 and {B}"));
            Assert.That(Last.Metadata.Single(m => m.Key == "A").InMessage, Is.True);
            Assert.That(Last.Metadata.Single(m => m.Key == "B").InMessage, Is.False);

            var text = Text(Last);
            Assert.That(text, Does.Not.Contain("A=1"));
            Assert.That(text, Does.Contain("B=scoped"));
        }

        [Test]
        public void InMessage_IsNotSerialized()
        {
            logger.Info("{A}", 1);
            Assert.That(LogJson.Serialize(Last), Does.Not.Contain("InMessage"));
        }

        [Test]
        public void PlainMessages_GetNoTemplateProperty()
        {
            logger.Info("Game started");
            Assert.That(Last.Message, Is.EqualTo("Game started"));
            Assert.That(Last.GetMetadata(LogPropertyKeys.MessageTemplate), Is.Null);

            logger.Info("Use {{braces}}");
            Assert.That(Last.Message, Is.EqualTo("Use {braces}"));
            Assert.That(Last.GetMetadata(LogPropertyKeys.MessageTemplate), Is.Null);

            logger.Error("Save failed", new InvalidOperationException("disk"));
            Assert.That(Last.Exception, Does.StartWith("InvalidOperationException: disk"));
        }
    }
}
