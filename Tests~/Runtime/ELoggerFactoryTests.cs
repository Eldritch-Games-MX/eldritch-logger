using EldritchGames.EldritchLogger.Core;
using Moq;
using NUnit.Framework;

namespace EldritchGames.EldritchLogger.Tests
{
    [TestFixture]
    public class ELoggerFactoryTests
    {
        [TearDown]
        public void TearDown()
        {
            ELoggerFactory.ClearFactory();
        }

        [Test]
        public void GetLogger_WhenFactoryNotSet_ReturnsNullLogger()
        {
            var logger = ELoggerFactory.GetLogger("Test");

            Assert.That(logger, Is.SameAs(NullLogger.Instance));
        }

        [Test]
        public void GetLoggerGeneric_WhenFactoryNotSet_ReturnsNullLogger()
        {
            var logger = ELoggerFactory.GetLogger<ELoggerFactoryTests>();

            Assert.That(logger, Is.SameAs(NullLogger.Instance));
        }

        [Test]
        public void GetLogger_WhenFactorySet_DelegatesToFactory()
        {
            var mockFactory = new Mock<ILoggerFactory>();
            var mockLogger  = new Mock<IEldritchLogger>();
            mockFactory.Setup(f => f.GetLogger("MyClass")).Returns(mockLogger.Object);

            ELoggerFactory.SetFactory(mockFactory.Object);
            var logger = ELoggerFactory.GetLogger("MyClass");

            Assert.That(logger, Is.SameAs(mockLogger.Object));
            mockFactory.Verify(f => f.GetLogger("MyClass"), Times.Once);
        }

        [Test]
        public void GetLoggerGeneric_WhenFactorySet_UsesTypeName()
        {
            var mockFactory = new Mock<ILoggerFactory>();
            var mockLogger  = new Mock<IEldritchLogger>();
            mockFactory.Setup(f => f.GetLogger(nameof(ELoggerFactoryTests))).Returns(mockLogger.Object);

            ELoggerFactory.SetFactory(mockFactory.Object);
            var logger = ELoggerFactory.GetLogger<ELoggerFactoryTests>();

            Assert.That(logger, Is.SameAs(mockLogger.Object));
        }

        [Test]
        public void SetFactory_Null_ThrowsArgumentNullException()
        {
            Assert.Throws<System.ArgumentNullException>(() => ELoggerFactory.SetFactory(null));
        }

        [Test]
        public void ClearFactory_AfterSet_ResetsToNullLogger()
        {
            ELoggerFactory.SetFactory(new Mock<ILoggerFactory>().Object);
            ELoggerFactory.ClearFactory();

            var logger = ELoggerFactory.GetLogger("Any");

            Assert.That(logger, Is.SameAs(NullLogger.Instance));
        }
    }
}
