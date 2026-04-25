using EldritchGames.EldritchLogger.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Builder
{
    /// <summary>
    /// Defines the fluent builder contract for constructing and dispatching log entries.
    /// Each method returns a new immutable builder with additional context.
    /// </summary>
    public interface ILogBuilder
    {
        ILogBuilder Category(LogCategory category);
        ILogBuilder AddKeyValue(string key, object value);
        ILogBuilder WithException(Exception ex);
        ILogBuilder WithEvent(object eventObj, string eventName);
        ILogBuilder WithComponent(Component component);

        /// <summary>
        /// Finalizes and dispatches the log entry with the given message.
        /// File exporters run asynchronously in the background; this call returns immediately.
        /// </summary>
        void Log(string message);
    }
}
