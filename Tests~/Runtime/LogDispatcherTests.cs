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
        public async Task Dispatch_ShouldCallSyncAndAsyncSinks()
        {
            // Arrange
            var syncSink = new Mock<ILogSink>();
            var asyncSink = new Mock<IAsyncLogExporter>();

            var dispatcher = new LogDispatcher();
            var dto = new LogEntryDto { Message = "Hello" };

            // Act
            await dispatcher.Dispatch(dto, new ILogSink[] { syncSink.Object, asyncSink.Object });

            // Assert
            // Sync sinks should get OnLogReceived
            syncSink.Verify(s => s.OnLogReceived(It.Is<LogEntryDto>(d => d.Message == "Hello")), Times.Once);

            // Async sinks should get Export, not OnLogReceived
            asyncSink.Verify(s => s.Export(It.Is<LogEntryDto>(d => d.Message == "Hello"), It.IsAny<string>()), Times.Once);
        }
    }
}
