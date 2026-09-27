using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Dto;
using EldritchGames.EldritchLogger.Sinks;
using System;
using System.Collections.Generic;

namespace EldritchGames.EldritchLogger.Pipeline
{
    /// <summary>
    /// Routes an entry to the sinks whose minimum level it meets.
    /// </summary>
    public interface ILogDispatcher
    {
        void Dispatch(LogEntryDto entry, IReadOnlyList<ILogSink> sinks);
    }

    /// <summary>
    /// Default dispatcher. Each sink is isolated: an exception is reported through
    /// <see cref="SelfLog"/> and the remaining sinks still receive the entry.
    /// </summary>
    public sealed class LogDispatcher : ILogDispatcher
    {
        [ThreadStatic] private static bool dispatching;

        /// <summary>
        /// True while the current thread is delivering an entry to sinks. Lets listeners of
        /// <c>Application.logMessageReceived</c> recognise the echo produced by <see cref="UnityConsoleSink"/>.
        /// </summary>
        public static bool IsDispatching => dispatching;

        public void Dispatch(LogEntryDto entry, IReadOnlyList<ILogSink> sinks)
        {
            bool wasDispatching = dispatching;
            dispatching = true;
            try
            {
                for (int i = 0; i < sinks.Count; i++)
                {
                    var sink = sinks[i];
                    if (entry.Level < sink.MinimumLevel) continue;

                    try
                    {
                        sink.Emit(entry);
                    }
                    catch (Exception ex)
                    {
                        SelfLog.Report($"Sink '{sink.Name}' failed", ex);
                    }
                }
            }
            finally
            {
                dispatching = wasDispatching;
            }
        }
    }
}
