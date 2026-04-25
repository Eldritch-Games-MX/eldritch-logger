using EldritchGames.EldritchLogger.Core;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;

namespace EldritchGames.EldritchLogger.Tests
{
    [TestFixture]
    public class NamedLoggerTests
    {
        [Test]
        public void Log_InjectsLoggerNameIntoMetadata()
        {
            var mockInner = new Mock<IEldritchLogger>();
            Dictionary<string, object> captured = null;
            mockInner
                .Setup(l => l.Log(
                    It.IsAny<LogLevel>(), It.IsAny<LogCategory>(), It.IsAny<string>(),
                    It.IsAny<Dictionary<string, object>>(), null))
                .Callback<LogLevel, LogCategory, string, Dictionary<string, object>, System.Exception>(
                    (_, __, ___, meta, ____) => captured = meta);

            var factory = new EldritchLoggerFactory(mockInner.Object);
            var logger  = factory.GetLogger("PlayerController");

            logger.Log(LogLevel.Info, LogCategory.General, "hello");

            Assert.That(captured, Is.Not.Null);
            Assert.That(captured["Logger"], Is.EqualTo("PlayerController"));
        }

        [Test]
        public void Log_PreservesExistingMetadata()
        {
            var mockInner = new Mock<IEldritchLogger>();
            Dictionary<string, object> captured = null;
            mockInner
                .Setup(l => l.Log(
                    It.IsAny<LogLevel>(), It.IsAny<LogCategory>(), It.IsAny<string>(),
                    It.IsAny<Dictionary<string, object>>(), null))
                .Callback<LogLevel, LogCategory, string, Dictionary<string, object>, System.Exception>(
                    (_, __, ___, meta, ____) => captured = meta);

            var factory  = new EldritchLoggerFactory(mockInner.Object);
            var logger   = factory.GetLogger("MySystem");
            var existing = new Dictionary<string, object> { { "UserId", 42 } };

            logger.Log(LogLevel.Info, LogCategory.General, "hello", existing);

            Assert.That(captured["UserId"], Is.EqualTo(42));
            Assert.That(captured["Logger"], Is.EqualTo("MySystem"));
        }

        [Test]
        public void Log_OverwritesExistingLoggerKey()
        {
            var mockInner = new Mock<IEldritchLogger>();
            Dictionary<string, object> captured = null;
            mockInner
                .Setup(l => l.Log(
                    It.IsAny<LogLevel>(), It.IsAny<LogCategory>(), It.IsAny<string>(),
                    It.IsAny<Dictionary<string, object>>(), null))
                .Callback<LogLevel, LogCategory, string, Dictionary<string, object>, System.Exception>(
                    (_, __, ___, meta, ____) => captured = meta);

            var factory  = new EldritchLoggerFactory(mockInner.Object);
            var logger   = factory.GetLogger("TrueOwner");
            var existing = new Dictionary<string, object> { { "Logger", "Spoofed" } };

            logger.Log(LogLevel.Info, LogCategory.General, "hello", existing);

            Assert.That(captured["Logger"], Is.EqualTo("TrueOwner"));
        }

        [Test]
        public void GetLogger_ReturnsDistinctInstancePerName()
        {
            var mockInner = new Mock<IEldritchLogger>();
            var factory   = new EldritchLoggerFactory(mockInner.Object);

            var a = factory.GetLogger("A");
            var b = factory.GetLogger("B");

            Assert.That(a, Is.Not.SameAs(b));
        }

        [Test]
        public void GetLogger_EmptyName_ThrowsArgumentException()
        {
            var mockInner = new Mock<IEldritchLogger>();
            var factory   = new EldritchLoggerFactory(mockInner.Object);

            Assert.Throws<System.ArgumentException>(() => factory.GetLogger(""));
            Assert.Throws<System.ArgumentException>(() => factory.GetLogger("   "));
        }

        [Test]
        public void Dispose_DisposesRootLogger()
        {
            var mockInner = new Mock<IEldritchLogger>();
            mockInner.As<System.IDisposable>();

            var factory = new EldritchLoggerFactory(mockInner.Object);
            factory.Dispose();

            mockInner.As<System.IDisposable>().Verify(d => d.Dispose(), Times.Once);
        }
    }
}
