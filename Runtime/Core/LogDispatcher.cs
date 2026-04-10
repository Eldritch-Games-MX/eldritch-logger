using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Dto;
using EldritchGames.EldritchLogger.Exporting;
using System.Collections.Generic;
using System.Threading.Tasks;

public class LogDispatcher : ILogDispatcher
{
    public async Task Dispatch(LogEntryDto dto, IEnumerable<ILogSink> sinks)
    {
        foreach (var sink in sinks)
        {
            switch (sink)
            {
                case IAsyncLogExporter asyncSink:
                    await asyncSink.Export(dto, asyncSink.TargetPath);
                    break;
                default:
                    sink.OnLogReceived(dto);
                    break;
            }
        }
    }
}
