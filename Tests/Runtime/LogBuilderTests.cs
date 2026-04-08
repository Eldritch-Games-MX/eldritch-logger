using EldritchGames.EldritchLogger;
using EldritchGames.EldritchLogger.Builder;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Tests
{
    [TestFixture]
    public class LogBuilderTests
    {
        private FakeLogger fakeLogger;

        [SetUp]
        public void Setup()
        {
            fakeLogger = new FakeLogger();
        }

        [Test]
        public void Category_SetsCategory()
        {
            fakeLogger.AtInfo()
                      .Category(LogCategory.UI)
                      .Log("Category test");

            Assert.AreEqual(LogCategory.UI, fakeLogger.LastCategory);
        }

        [Test]
        public void AddKeyValue_AddsMetadata()
        {
            fakeLogger.AtInfo()
                      .AddKeyValue("CustomKey", "CustomValue")
                      .Log("Info with metadata");

            Assert.AreEqual("CustomValue", fakeLogger.LastMetadata["CustomKey"]);
        }

        [Test]
        public void WithException_AttachesException()
        {
            var ex = new InvalidOperationException("Boom!");
            fakeLogger.AtError()
                      .WithException(ex)
                      .Log("Error with exception");

            Assert.AreEqual(ex, fakeLogger.LastException);
        }

        [Test]
        public void WithComponent_AddsComponentContextAndGameObject()
        {
            var go = new GameObject("TestObject");
            var component = go.AddComponent<BoxCollider>();

            fakeLogger.AtInfo()
                      .WithComponent(component)
                      .Log("Info with component");

            Assert.AreEqual("BoxCollider@TestObject", fakeLogger.LastMetadata["ComponentContext"]);
            Assert.AreEqual("TestObject", fakeLogger.LastMetadata["GameObject"]);
        }

        /// <summary>
        /// Simple fake logger implementation to capture the last log entry for assertions.
        /// Implements <see cref="IEldritchLogger"/> so it works with <see cref="LogBuilder"/>.
        /// </summary>
        private class FakeLogger : IEldritchLogger
        {
            public LogLevel LastLevel { get; private set; }
            public LogCategory LastCategory { get; private set; }
            public string LastMessage { get; private set; }
            public Dictionary<string, object> LastMetadata { get; private set; }
            public Exception LastException { get; private set; }

            public void Log(LogLevel level, LogCategory category, string message,
                            Dictionary<string, object> metadata = null, Exception exception = null)
            {
                LastLevel = level;
                LastCategory = category;
                LastMessage = message;
                LastMetadata = metadata ?? new Dictionary<string, object>();
                LastException = exception;
            }

            public ILogBuilder AtDebug(LogCategory category = LogCategory.General) =>
                new LogBuilder(this, LogLevel.Debug, category);

            public ILogBuilder AtInfo(LogCategory category = LogCategory.General) =>
                new LogBuilder(this, LogLevel.Info, category);

            public ILogBuilder AtWarning(LogCategory category = LogCategory.General) =>
                new LogBuilder(this, LogLevel.Warning, category);

            public ILogBuilder AtError(LogCategory category = LogCategory.General) =>
                new LogBuilder(this, LogLevel.Error, category);

            public ILogBuilder AtCritical(LogCategory category = LogCategory.General) =>
                new LogBuilder(this, LogLevel.Critical, category);
        }
    }
}
