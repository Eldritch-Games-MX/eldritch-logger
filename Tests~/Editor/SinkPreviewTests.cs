using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.EditorTools;
using EldritchGames.EldritchLogger.Settings;
using EldritchGames.EldritchLogger.Sinks.Config;
using EldritchGames.EldritchLogger.Sinks.Network;
using NUnit.Framework;
using System.Linq;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Tests.Editor
{
    public class SinkPreviewTests
    {
        private LogSettings settings;

        [SetUp]
        public void SetUp() => settings = ScriptableObject.CreateInstance<LogSettings>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(settings);

        [Test]
        public void Sample_IsARealTemplatedEntry_WithScopeAndException()
        {
            settings.minimumLevel = LogLevel.Warning;
            var sample = SinkPreview.CreateSample(settings);

            Assert.That(sample.Level, Is.EqualTo(LogLevel.Warning));
            Assert.That(sample.Message, Is.EqualTo("Player Bob took 12.5 damage"));
            Assert.That(sample.GetMetadata("MatchId"), Is.EqualTo("42"));
            Assert.That(sample.Metadata.Single(m => m.Key == "Name").InMessage, Is.True);
            Assert.That(sample.Exception, Does.StartWith("InvalidOperationException"));
        }

        [Test]
        public void EveryBuiltInSinkConfig_PreviewsItsFormat()
        {
            var sample = SinkPreview.CreateSample(settings);

            var text = new TextFileSinkConfig().Preview(sample, settings);
            Assert.That(text, Does.Contain("Player Bob took 12.5 damage").And.Contain("MatchId=42").And.Not.Contain("<color"));
            Assert.That(text, Does.Not.Contain("Name=Bob"), "filled holes are not repeated");

            Assert.That(new UnityConsoleSinkConfig().Preview(sample, settings), Does.Contain("<color"));
            Assert.That(new UnityConsoleSinkConfig().PreviewIsRichText, Is.True);
            Assert.That(new JsonLinesFileSinkConfig().Preview(sample, settings), Does.StartWith("{").And.Contain("\"MatchId\""));
            Assert.That(new XmlFileSinkConfig().Preview(sample, settings), Does.StartWith("<LogEntryDto").And.Not.Contain("InMessage"));

            var http = new HttpSinkConfig { format = HttpPayloadFormat.Clef };
            Assert.That(http.Preview(sample, settings), Does.Contain("\"@mt\":\"Player {Name} took {Damage:0.0} damage\""));
        }
    }
}
