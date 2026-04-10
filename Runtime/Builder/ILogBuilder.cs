using EldritchGames.EldritchLogger.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
        /// Finalizes and dispatches the log entry asynchronously with the given message.
        /// Callers should await this method.
        /// </summary>
        Task Log(string message);
    }
}
