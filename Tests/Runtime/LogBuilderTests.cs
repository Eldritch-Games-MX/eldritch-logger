using EldritchGames.EldritchLogger.Builder;
using EldritchGames.EldritchLogger.Core;
using Moq;
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
        private Mock<IEldritchLogger> mockLogger;
        private LogLevel capturedLevel;
        private LogCategory capturedCategory;
        private string capturedMessage;
        private Dictionary<string, object> capturedMetadata;
        private Exception capturedException;

        [SetUp]
        public void Setup()
        {
            mockLogger = new Mock<IEldritchLogger>();

            // Capture arguments whenever Log is called
            mockLogger.Setup(l => l.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<LogCategory>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<Exception>()))
            .Callback((LogLevel level, LogCategory category, string message,
                       Dictionary<string, object> metadata, Exception exception) =>
            {
                capturedLevel = level;
                capturedCategory = category;
                capturedMessage = message;
                capturedMetadata = metadata;
                capturedException = exception;
            })
            .Returns(Task.CompletedTask);
        }

        [Test]
        public async Task Category_SetsCategory()
        {
            await new LogBuilder(mockLogger.Object, LogLevel.Info, LogCategory.General)
                .Category(LogCategory.UI)
                .Log("Category test");

            Assert.AreEqual(LogCategory.UI, capturedCategory);
        }

        [Test]
        public async Task AddKeyValue_AddsMetadata()
        {
            await new LogBuilder(mockLogger.Object, LogLevel.Info, LogCategory.General)
                .AddKeyValue("CustomKey", "CustomValue")
                .Log("Info with metadata");

            Assert.AreEqual("CustomValue", capturedMetadata["CustomKey"]);
        }

        [Test]
        public async Task WithException_AttachesException()
        {
            var ex = new InvalidOperationException("Boom!");
            await new LogBuilder(mockLogger.Object, LogLevel.Error, LogCategory.General)
                .WithException(ex)
                .Log("Error with exception");

            Assert.AreEqual(ex, capturedException);
        }

        [Test]
        public async Task WithComponent_AddsComponentContextAndGameObject()
        {
            var go = new GameObject("TestObject");
            var component = go.AddComponent<BoxCollider>();

            await new LogBuilder(mockLogger.Object, LogLevel.Info, LogCategory.General)
                .WithComponent(component)
                .Log("Info with component");

            Assert.AreEqual("BoxCollider@TestObject", capturedMetadata["ComponentContext"]);
            Assert.AreEqual("TestObject", capturedMetadata["GameObject"]);
        }
    }
}
