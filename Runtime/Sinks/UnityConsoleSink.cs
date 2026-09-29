using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Dto;
using EldritchGames.EldritchLogger.Formatting;
using System;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Sinks
{
    /// <summary>
    /// Writes entries to the Unity Console through <see cref="Debug"/>.
    /// </summary>
    public sealed class UnityConsoleSink : ILogSink, IShowsUnityLog
    {
        private readonly ILogFormatter formatter;
        private readonly bool useContextObjects;

        public string Name => "Unity Console";
        public LogLevel MinimumLevel { get; }

        public UnityConsoleSink(ILogFormatter formatter,
                                LogLevel minimumLevel = LogLevel.Debug,
                                bool useContextObjects = true,
                                bool suppressUnityStackTrace = false)
        {
            this.formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
            this.useContextObjects = useContextObjects;
            MinimumLevel = minimumLevel;

            // Configure once: this is global Unity state, not per-entry work.
            if (suppressUnityStackTrace)
            {
                Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
                Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);
                Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.None);
                Application.SetStackTraceLogType(LogType.Exception, StackTraceLogType.None);
            }
        }

        public void Emit(LogEntryDto entry)
        {
            string message = formatter.Format(entry);
            UnityEngine.Object context = useContextObjects ? entry.Context : null;

            switch (entry.Level)
            {
                case LogLevel.Warning:
                    Debug.LogWarning(message, context);
                    break;
                case LogLevel.Error:
                case LogLevel.Critical:
                    Debug.LogError(message, context);
                    break;
                default:
                    Debug.Log(message, context);
                    break;
            }
        }
    }
}
