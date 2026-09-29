using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Mapper;
using EldritchGames.EldritchLogger.Pipeline;
using NUnit.Framework;
using System;

namespace EldritchGames.EldritchLogger.Tests
{
    public class ELoggerFactoryTests
    {
        private RecordingSink sink;
        private Core.EldritchLogger root;

        [SetUp]
        public void SetUp()
        {
            sink = new RecordingSink();
            root = new Core.EldritchLogger(new DelegateFilter((_, _) => true), null, new LogEntryMapper(),
                                           new LogDispatcher(), new FakeClock(), new[] { sink });
        }

        [TearDown]
        public void TearDown()
        {
            ELoggerFactory.ClearFactory();
            root.Dispose();
        }

        [Test]
        public void WithoutFactory_ReturnsNullLogger_AndNoSinkRegistry()
        {
            Assert.That(ELoggerFactory.GetLogger("x"), Is.SameAs(NullLogger.Instance));
            Assert.That(ELoggerFactory.GetLogger<ELoggerFactoryTests>(), Is.SameAs(NullLogger.Instance));
            Assert.That(ELoggerFactory.Sinks, Is.Null);
        }

        [Test]
        public void NamedLogger_StampsLoggerName()
        {
            ELoggerFactory.SetFactory(new EldritchLoggerFactory(root));

            ELoggerFactory.GetLogger<ELoggerFactoryTests>().AtInfo().Log("hi");

            Assert.That(sink.Entries[0].GetMetadata(LogPropertyKeys.Logger), Is.EqualTo(nameof(ELoggerFactoryTests)));
        }

        [Test]
        public void Sinks_DelegatesToRootLogger()
        {
            ELoggerFactory.SetFactory(new EldritchLoggerFactory(root));
            var extra = new RecordingSink();

            ELoggerFactory.Sinks.AddSink(extra);
            ELoggerFactory.GetLogger("x").AtInfo().Log("hi");

            Assert.That(extra.Entries, Has.Count.EqualTo(1));
        }

        [Test]
        public void Factory_RejectsBlankNamesAndNullRoot()
        {
            Assert.Throws<ArgumentNullException>(() => new EldritchLoggerFactory(null));
            Assert.Throws<ArgumentException>(() => new EldritchLoggerFactory(root).GetLogger(" "));
            Assert.Throws<ArgumentNullException>(() => ELoggerFactory.SetFactory(null));
        }

        [Test]
        public void Factory_WithRootWithoutRegistry_ThrowsNotSupported()
        {
            var factory = new EldritchLoggerFactory(NullLogger.Instance);
            Assert.Throws<NotSupportedException>(() => factory.AddSink(new RecordingSink()));
        }

        [Test]
        public void Factory_Dispose_DisposesTheRootLoggerAndItsSinks()
        {
            var sink = new RecordingSink();
            var root = new EldritchLoggerBuilder().AddSink(sink).Build();

            new EldritchLoggerFactory(root).Dispose();

            Assert.That(sink.Disposed, Is.True);
            Assert.That(root.IsEnabled(LogLevel.Critical, LogCategory.General), Is.False);
        }
    }
}
