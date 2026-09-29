using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Domain;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Pipeline
{
    /// <summary>Which of Unity's own log messages are forwarded into the logger.</summary>
    public enum UnityLogCapture
    {
        Off,
        /// <summary>Errors, asserts and exceptions (including uncaught ones).</summary>
        ErrorsAndExceptions,
        WarningsAndAbove,
        /// <summary>Everything, including plain <c>Debug.Log</c> calls.</summary>
        All
    }

    /// <summary>
    /// Forwards Unity's log (<c>Debug.Log*</c>, engine errors, uncaught exceptions) into a logger, so they
    /// reach file and remote sinks. Entries use the <see cref="LogCategory.Unity"/> category and carry
    /// <see cref="LogPropertyKeys.Source"/> = <see cref="LogPropertyKeys.UnitySource"/>; sinks that write to
    /// the Unity Console skip them, since Unity already shows them.
    /// </summary>
    public sealed class UnityLogForwarder : IDisposable
    {
        [ThreadStatic] private static bool forwarding;

        private readonly IEldritchLogger target;
        private readonly UnityLogCapture capture;
        private bool disposed;

        public UnityLogForwarder(IEldritchLogger target, UnityLogCapture capture)
        {
            this.target = target ?? throw new ArgumentNullException(nameof(target));
            this.capture = capture;
            if (capture == UnityLogCapture.Off) return;

            Application.logMessageReceivedThreaded += OnLogMessage;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        }

        /// <summary>
        /// True for entries forwarded from Unity's own log. The logger keeps these away from
        /// <see cref="Sinks.IShowsUnityLog"/> sinks, which already show them.
        /// </summary>
        public static bool IsFromUnity(Dto.LogEntryDto entry) =>
            entry != null && entry.GetMetadata(LogPropertyKeys.Source) == LogPropertyKeys.UnitySource;

        public static LogLevel? ToLevel(LogType type, UnityLogCapture capture)
        {
            var level = type switch
            {
                LogType.Log => LogLevel.Info,
                LogType.Warning => LogLevel.Warning,
                _ => LogLevel.Error // Error, Assert, Exception
            };

            bool wanted = capture switch
            {
                UnityLogCapture.All => true,
                UnityLogCapture.WarningsAndAbove => level >= LogLevel.Warning,
                UnityLogCapture.ErrorsAndExceptions => level >= LogLevel.Error,
                _ => false
            };
            return wanted ? level : null;
        }

        [ThreadStatic] private static int suppressed;

        /// <summary>
        /// Unity log writes made on this thread inside the returned scope are not captured. Use it in custom sinks
        /// or tools that write to <c>Debug.*</c> themselves, so their output is not fed back into the logger:
        /// <code>using (UnityLogForwarder.SuppressCapture()) Debug.LogError(text);</code>
        /// The package's own Unity Console output (<see cref="Sinks.UnityConsoleSink"/>, <see cref="SelfLog"/>)
        /// is already excluded.
        /// </summary>
        public static IDisposable SuppressCapture()
        {
            suppressed++;
            return new SuppressionScope();
        }

        private sealed class SuppressionScope : IDisposable
        {
            private bool disposed;

            public void Dispose()
            {
                if (disposed) return;
                disposed = true;
                suppressed--;
            }
        }

        // An uncaught exception can reach us twice: through AppDomain.UnhandledException and through Unity's own
        // "Exception" log entry, in either order. Each source records what it forwarded (keyed by "Type: message");
        // an entry is skipped only when the *other* source already forwarded the same exception moments ago.
        // Repeats from the same source (e.g. an exception thrown every frame) are all forwarded.
        private static readonly TimeSpan DuplicateWindow = TimeSpan.FromSeconds(5);
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, DateTime> forwardedFromUnityLog = new();
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, DateTime> forwardedFromAppDomain = new();

        /// <summary>True if <paramref name="other"/> recently forwarded this exception (the match is consumed); otherwise records it in <paramref name="mine"/>.</summary>
        private static bool IsCounterpart(string fingerprint,
                                          System.Collections.Concurrent.ConcurrentDictionary<string, DateTime> mine,
                                          System.Collections.Concurrent.ConcurrentDictionary<string, DateTime> other)
        {
            var now = DateTime.UtcNow;
            if (other.TryRemove(fingerprint, out var seen) && now - seen <= DuplicateWindow) return true;

            foreach (var kv in mine)
                if (now - kv.Value > DuplicateWindow) mine.TryRemove(kv.Key, out _);
            mine[fingerprint] = now;
            return false;
        }

        private static string Fingerprint(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            int newline = text.IndexOf('\n');
            return (newline >= 0 ? text.Substring(0, newline) : text).Trim();
        }

        private void OnLogMessage(string condition, string stackTrace, LogType type)
        {
            // Never feed the logger's own output back in: sinks writing to the Unity Console while dispatching,
            // SelfLog diagnostics (which can come from sink background threads), and explicitly suppressed writes.
            if (disposed || forwarding || suppressed > 0 || LogDispatcher.IsDispatching || SelfLog.IsReporting) return;

            var level = ToLevel(type, capture);
            if (level == null) return;

            if (type == LogType.Exception &&
                IsCounterpart(Fingerprint(condition), forwardedFromUnityLog, forwardedFromAppDomain)) return;

            Forward(level.Value, condition, stackTrace, null);
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (disposed || forwarding) return;

            var exception = e.ExceptionObject as Exception;
            var fingerprint = exception != null ? $"{exception.GetType().Name}: {exception.Message}" : string.Empty;
            if (exception == null || !IsCounterpart(Fingerprint(fingerprint), forwardedFromAppDomain, forwardedFromUnityLog))
            {
                Forward(LogLevel.Critical, "Unhandled exception" + (e.IsTerminating ? " (terminating)" : string.Empty),
                        null, exception);
            }

            // The process may be about to die: push buffered entries out, but never block the crashing
            // thread for long (a slow HTTP endpoint could otherwise hold it for seconds).
            if (target is Core.EldritchLogger logger)
            {
                try
                {
                    System.Threading.Tasks.Task.Run(logger.Flush).Wait(CrashFlushTimeout);
                }
                catch (Exception ex)
                {
                    SelfLog.Report("Flushing after an unhandled exception failed", ex);
                }
            }
        }

        /// <summary>How long an unhandled exception waits for sinks to flush before giving up.</summary>
        public static TimeSpan CrashFlushTimeout { get; set; } = TimeSpan.FromSeconds(1);

        private void Forward(LogLevel level, string message, string stackTrace, Exception exception)
        {
            forwarding = true;
            try
            {
                var properties = new Dictionary<string, object> { [LogPropertyKeys.Source] = LogPropertyKeys.UnitySource };
                if (!string.IsNullOrEmpty(stackTrace)) properties[LogPropertyKeys.StackTrace] = stackTrace.TrimEnd();
                target.Log(new LogEntry(level, LogCategory.Unity, message, properties, exception));
            }
            catch (Exception ex)
            {
                SelfLog.Report("Failed to forward a Unity log message", ex);
            }
            finally
            {
                forwarding = false;
            }
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            Application.logMessageReceivedThreaded -= OnLogMessage;
            AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
        }
    }
}
