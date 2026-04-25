using EldritchGames.EldritchLogger.Dto;
using System.Collections.Generic;

namespace EldritchGames.EldritchLogger.Core
{
    /// <summary>
    /// Routes log entries to registered sinks.
    /// Synchronous sinks (e.g. Unity Console) are called immediately.
    /// Async sinks (e.g. file exporters) are dispatched in the background.
    /// </summary>
    public interface ILogDispatcher
    {
        /// <summary>
        /// Dispatches <paramref name="dto"/> to every sink. Returns immediately.
        /// </summary>
        void Dispatch(LogEntryDto dto, IEnumerable<ILogSink> sinks);
    }
}