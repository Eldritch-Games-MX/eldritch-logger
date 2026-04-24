using EldritchGames.EldritchLogger.Builder;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Core
{
    /// <summary>
    /// Static facade over <see cref="EldritchLogger"/>. Eliminates .Instance at every call site.
    /// Initialized automatically via <see cref="Settings.LoggerBootstrap"/>.
    /// </summary>
    public static class ELogger
    {
        private static EldritchLogger Instance
        {
            get
            {
                if (EldritchLogger.Instance == null)
                    Debug.LogError("[ELogger] Logger not initialized. Ensure LoggerBootstrap has run.");
                return EldritchLogger.Instance;
            }
        }

        public static Task Log(LogLevel level, LogCategory category, string message,
                               Dictionary<string, object> metadata = null, Exception exception = null)
            => Instance.Log(level, category, message, metadata, exception);

        public static ILogBuilder AtDebug(LogCategory category = LogCategory.General)
            => Instance.AtDebug(category);

        public static ILogBuilder AtInfo(LogCategory category = LogCategory.General)
            => Instance.AtInfo(category);

        public static ILogBuilder AtWarning(LogCategory category = LogCategory.General)
            => Instance.AtWarning(category);

        public static ILogBuilder AtError(LogCategory category = LogCategory.General)
            => Instance.AtError(category);

        public static ILogBuilder AtCritical(LogCategory category = LogCategory.General)
            => Instance.AtCritical(category);
    }
}
