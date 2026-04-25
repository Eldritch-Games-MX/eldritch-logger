using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Dto;
using EldritchGames.EldritchLogger.Exporting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class LogDispatcher : ILogDispatcher
{
    public void Dispatch(LogEntryDto dto, IEnumerable<ILogSink> sinks)
    {
        foreach (var sink in sinks)
        {
            switch (sink)
            {
                case IAsyncLogExporter asyncSink:
                    _ = ExportAsync(asyncSink, dto);
                    break;
                default:
                    sink.OnLogReceived(dto);
                    break;
            }
        }
    }

    private static async Task ExportAsync(IAsyncLogExporter sink, LogEntryDto dto)
    {
        try
        {
            await sink.Export(dto, sink.TargetPath);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[EldritchLogger] Export to {sink.GetType().Name} failed: {ex.Message}");
        }
    }
}
