using EldritchGames.EldritchLogger.Dto;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EldritchGames.EldritchLogger.Core
{
    /// <summary>
    /// Interface for dispatching log entries to registered sinks.
    /// </summary>
    public interface ILogDispatcher
    {
        /// <summary>
        /// Dispatches a log entry to all appropriate sinks based on its level and category.
        /// </summary>
        Task Dispatch(LogEntryDto dto, IEnumerable<ILogSink> sinks);
    }
}