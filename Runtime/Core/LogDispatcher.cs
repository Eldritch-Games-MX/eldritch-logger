using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Dto;
using EldritchGames.EldritchLogger.Exporting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class LogDispatcher : ILogDispatcher
{
    [ThreadStatic] private static bool dispatching;

    /// <summary>
    /// True while the current thread is delivering an entry to synchronous sinks.
    /// Lets listeners of <see cref="Application.logMessageReceived"/> recognise the
    /// <see cref="Debug.Log(object)"/> echo produced by <see cref="UnityConsoleExporter"/>.
    /// </summary>
    public static bool IsDispatching => dispatching;

    public void Dispatch(LogEntryDto dto, IEnumerable<ILogSink> sinks)
    {
        bool wasDispatching = dispatching;
        dispatching = true;
        try
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
        finally
        {
            dispatching = wasDispatching;
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
