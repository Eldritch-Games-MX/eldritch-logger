using EldritchGames.EldritchLogger.Builder;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Core
{
    /// <summary>
    /// Defines the contract for the Eldritch logging API.
    /// Provides an asynchronous <c>Log()</c> method and fluent builder entry points for common log levels.
    /// </summary>
    public interface IEldritchLogger
    {
        /// <summary>
        /// Logs a message asynchronously with the specified level and category.
        /// Callers should await this method to ensure logs are flushed.
        /// </summary>
        Task Log(LogLevel level, LogCategory category, string message,
                 Dictionary<string, object> metadata = null, Exception exception = null);

        /// <summary>
        /// Creates a fluent builder for a debug-level log entry.
        /// </summary>
        ILogBuilder AtDebug(LogCategory category = LogCategory.General);

        /// <summary>
        /// Creates a fluent builder for an info-level log entry.
        /// </summary>
        ILogBuilder AtInfo(LogCategory category = LogCategory.General);

        /// <summary>
        /// Creates a fluent builder for a warning-level log entry.
        /// </summary>
        ILogBuilder AtWarning(LogCategory category = LogCategory.General);

        /// <summary>
        /// Creates a fluent builder for an error-level log entry.
        /// </summary>
        ILogBuilder AtError(LogCategory category = LogCategory.General);

        /// <summary>
        /// Creates a fluent builder for a critical-level log entry.
        /// </summary>
        ILogBuilder AtCritical(LogCategory category = LogCategory.General);
    }
}
