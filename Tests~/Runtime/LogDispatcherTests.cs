using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Dto;
using EldritchGames.EldritchLogger.Exporting;
using Moq;
using NUnit.Framework;
using System.Threading.Tasks;

namespace EldritchGames.EldritchLogger.Tests
{
    [TestFixture]
    public class LogDispatcherTests
    {
        [Test]
        public void Dispatch_ShouldCallSyncSinkImmediately()
        {
            var syncSink = new Mock<ILogSink>();
            var dispatcher = new LogDispatcher();
            var dto = new LogEntryDto { Message = "Hello" };

            dispatcher.Dispatch(dto, new ILogSink[] { syncSink.Object });

            syncSink.Verify(s => s.OnLogReceived(It.Is<LogEntryDto>(d => d.Message == "Hello")), Times.Once);
        }

        [Test]
        public void Dispatch_ShouldFireAndForgetAsyncSink()
        {
            var asyncSink = new Mock<IAsyncLogExporter>();
            asyncSink.Setup(s => s.Export(It.IsAny<LogEntryDto>(), It.IsAny<string>()))
                     .Returns(Task.CompletedTask);

            var dispatcher = new LogDispatcher();
            var dto = new LogEntryDto { Message = "Hello" };

            dispatcher.Dispatch(dto, new ILogSink[] { asyncSink.Object });

            // Task.CompletedTask is already done so ExportAsync runs synchronously in test context
            asyncSink.Verify(s => s.Export(It.Is<LogEntryDto>(d => d.Message == "Hello"), It.IsAny<string>()), Times.Once);
        }

        [Test]
        public void Dispatch_ShouldCallBothSyncAndAsyncSinks()
        {
            var syncSink  = new Mock<ILogSink>();
            var asyncSink = new Mock<IAsyncLogExporter>();
            asyncSink.Setup(s => s.Export(It.IsAny<LogEntryDto>(), It.IsAny<string>()))
                     .Returns(Task.CompletedTask);

            var dispatcher = new LogDispatcher();
            var dto = new LogEntryDto { Message = "Hello" };

            dispatcher.Dispatch(dto, new ILogSink[] { syncSink.Object, asyncSink.Object });

            syncSink.Verify(s => s.OnLogReceived(It.Is<LogEntryDto>(d => d.Message == "Hello")), Times.Once);
            asyncSink.Verify(s => s.Export(It.Is<LogEntryDto>(d => d.Message == "Hello"), It.IsAny<string>()), Times.Once);
        }
    }
}
