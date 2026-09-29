using System;
using System.Threading;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Core
{
    /// <summary>
    /// Reports failures inside the logger itself (a sink throwing, a file that cannot be written).
    /// Writes straight to the Unity Console and never re-enters the logging pipeline.
    /// </summary>
    public static class SelfLog
    {
        private const string Prefix = "[EldritchLogger] ";

        [ThreadStatic] private static bool reporting;
        private static int reportCount;

        /// <summary>
        /// True while the current thread is writing a SelfLog report. Unity log listeners inside the package
        /// (e.g. the Unity log forwarder) use it to avoid feeding the logger's own diagnostics back into it.
        /// </summary>
        public static bool IsReporting => reporting;

        /// <summary>Maximum number of reports per session, to avoid flooding the console.</summary>
        public static int MaxReports { get; set; } = 100;

        /// <summary>Replaces the output (for tests). Null restores the Unity Console.</summary>
        public static Action<string> Output { get; set; }

        public static void Report(string message, Exception exception = null)
        {
            if (reporting) return;
            if (Interlocked.Increment(ref reportCount) > MaxReports) return;

            reporting = true;
            try
            {
                string text = exception == null
                    ? Prefix + message
                    : $"{Prefix}{message}: {exception.GetType().Name}: {exception.Message}";

                if (Output != null) Output(text);
                else Debug.LogWarning(text);
            }
            catch
            {
                // Diagnostics must never throw.
            }
            finally
            {
                reporting = false;
            }
        }

        /// <summary>Resets the report budget (for tests and new sessions).</summary>
        public static void Reset() => Interlocked.Exchange(ref reportCount, 0);
    }
}
