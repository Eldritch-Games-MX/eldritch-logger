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
        /// Set by the logger around any <see cref="ILogDispatcher"/>, so custom dispatchers keep the protection.
        /// </summary>
        public static bool IsDispatching => dispatching;

        /// <summary>Marks the current thread as dispatching; pass the result to <see cref="EndDispatch"/>.</summary>
        internal static bool BeginDispatch()
        {
            bool previous = dispatching;
            dispatching = true;
            return previous;
        }

        internal static void EndDispatch(bool previous) => dispatching = previous;

        public void Dispatch(LogEntryDto entry, IReadOnlyList<ILogSink> sinks)
        {
            bool wasDispatching = BeginDispatch();
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
                EndDispatch(wasDispatching);
            }
        }
    }
}
