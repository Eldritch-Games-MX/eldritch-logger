using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Dto;
using EldritchGames.EldritchLogger.Exporting;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EldritchGames.EldritchLogger.Tests
{
    [TestFixture]
    public class LogDispatcherTests
    {
        private class SyncSinkStub : ILogSink
        {
            public List<LogEntryDto> Calls = new();
            public SinkCategory Category => SinkCategory.Persistent;
            public void OnLogReceived(LogEntryDto dto) => Calls.Add(dto);
        }

        private class AsyncSinkStub : IAsyncLogExporter
        {
            public List<LogEntryDto> Calls = new();
            public SinkCategory Category => SinkCategory.Persistent;
            public string TargetPath => "unused";
            public Task Export(LogEntryDto dto, string path)
            {
                Calls.Add(dto);
                return Task.CompletedTask;
            }
            public void OnLogReceived(LogEntryDto dto) => Calls.Add(dto);
        }

        [Test]
        public async Task Dispatch_ShouldCallSyncAndAsyncSinks()
        {
            var sync = new SyncSinkStub();
            var async = new AsyncSinkStub();
            var dispatcher = new LogDispatcher();

            var dto = new LogEntryDto { Message = "Hello" };
            await dispatcher.Dispatch(dto, new ILogSink[] { sync, async });

            Assert.That(sync.Calls.Count, Is.EqualTo(1));
            Assert.That(async.Calls.Count, Is.EqualTo(1));
        }
    }
}