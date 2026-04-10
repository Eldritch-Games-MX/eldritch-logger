using EldritchGames.EldritchLogger.Builder;
using EldritchGames.EldritchLogger.Core;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
        public async Task Category_SetsCategory()
        {
            await fakeLogger.AtInfo()
                            .Category(LogCategory.UI)
                            .Log("Category test");

            Assert.AreEqual(LogCategory.UI, fakeLogger.LastCategory);
        }

        [Test]
        public async Task AddKeyValue_AddsMetadata()
        {
            await fakeLogger.AtInfo()
                            .AddKeyValue("CustomKey", "CustomValue")
                            .Log("Info with metadata");

            Assert.AreEqual("CustomValue", fakeLogger.LastMetadata["CustomKey"]);
        }

        [Test]
        public async Task WithException_AttachesException()
        {
            var ex = new InvalidOperationException("Boom!");
            await fakeLogger.AtError()
                            .WithException(ex)
                            .Log("Error with exception");

            Assert.AreEqual(ex, fakeLogger.LastException);
        }

        [Test]
        public async Task WithComponent_AddsComponentContextAndGameObject()
        {
            var go = new GameObject("TestObject");
            var component = go.AddComponent<BoxCollider>();

            await fakeLogger.AtInfo()
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

            public Task Log(LogLevel level, LogCategory category, string message,
                            Dictionary<string, object> metadata = null, Exception exception = null)
            {
                LastLevel = level;
                LastCategory = category;
                LastMessage = message;
                LastMetadata = metadata ?? new Dictionary<string, object>();
                LastException = exception;
                return Task.CompletedTask;
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
