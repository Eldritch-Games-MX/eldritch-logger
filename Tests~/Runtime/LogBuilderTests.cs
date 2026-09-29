using EldritchGames.EldritchLogger.Builder;
using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Domain;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace EldritchGames.EldritchLogger.Tests
{
    public class LogBuilderTests
    {
        private sealed class CapturingLogger : IEldritchLogger
        {
            public readonly List<LogEntry> Entries = new();
            public bool IsEnabled(LogLevel level, LogCategory category) => true;
            public void Log(LogEntry entry) => Entries.Add(entry);
        }

        private CapturingLogger logger;
        private GameObject gameObject;

        [SetUp]
        public void SetUp() => logger = new CapturingLogger();

        [TearDown]
        public void TearDown()
        {
            if (gameObject != null) UnityEngine.Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void Chain_ProducesOneEntryWithEverything()
        {
            var ex = new Exception("x");

            logger.AtWarning(LogCategory.AI)
                  .Category("Loot")
                  .AddKeyValue("Id", 42)
                  .WithException(ex)
                  .Log("message");

            var entry = logger.Entries[0];
            Assert.That(logger.Entries, Has.Count.EqualTo(1));
            Assert.That(entry.Level, Is.EqualTo(LogLevel.Warning));
            Assert.That(entry.Category, Is.EqualTo(new LogCategory("Loot")));
            Assert.That(entry.Properties["Id"], Is.EqualTo(42));
            Assert.That(entry.Exception, Is.SameAs(ex));
            Assert.That(entry.Message, Is.EqualTo("message"));
        }

        [Test]
        public void WithComponent_RecordsComponentAndGameObject_AndSetsContext()
        {
            gameObject = new GameObject("Player");
            var component = gameObject.AddComponent<BoxCollider>();

            logger.AtInfo().WithComponent(component).Log("m");

            var entry = logger.Entries[0];
            Assert.That(entry.Properties[LogPropertyKeys.Component], Is.EqualTo(nameof(BoxCollider)));
            Assert.That(entry.Properties[LogPropertyKeys.GameObject], Is.EqualTo("Player"));
            Assert.That(entry.Context, Is.SameAs(component));
        }

        [Test]
        public void WithEvent_DistinguishesDelegatesAndUnityEvents()
        {
            Action handler = () => { };

            logger.AtInfo().WithEvent(handler, "OnDeath").Log("a");
            logger.AtInfo().WithEvent(new UnityEvent(), "OnClick").Log("b");

            Assert.That(logger.Entries[0].Properties[LogPropertyKeys.CSharpEvent], Is.EqualTo("OnDeath"));
            Assert.That(logger.Entries[1].Properties[LogPropertyKeys.UnityEvent], Is.EqualTo("OnClick"));
        }

        [Test]
        public void WithContext_SetsContextObject()
        {
            gameObject = new GameObject("Ctx");

            logger.AtInfo().WithContext(gameObject).Log("m");

            Assert.That(logger.Entries[0].Context, Is.SameAs(gameObject));
        }

        [Test]
        public void NullArguments_Throw()
        {
            var builder = new LogBuilder(logger, LogLevel.Info);
            Assert.Throws<ArgumentNullException>(() => builder.AddKeyValue(null, 1));
            Assert.Throws<ArgumentNullException>(() => builder.WithEvent(null, "x"));
            Assert.Throws<ArgumentNullException>(() => builder.WithComponent(null));
            Assert.Throws<ArgumentNullException>(() => new LogBuilder(null, LogLevel.Info));
        }
    }
}
