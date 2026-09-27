using EldritchGames.EldritchLogger.Core;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Console.Settings
{
    [CreateAssetMenu(fileName = "Eldritch Console Settings", menuName = "Eldritch Logger/Console Settings")]
    public class CommandConsoleSettings : ScriptableObject
    {
        [Header("History")]
        [Tooltip("Maximum number of commands stored in history.")]
        [Min(1)] public int historySize = 50;

        [Tooltip("Entries shown by 'history' without --limit.")]
        [Min(1)] public int defaultHistoryLimit = 20;

        [Tooltip("Ignore a command identical to the previous one.")]
        public bool ignoreConsecutiveDuplicates = true;

        [Header("Rendering")]
        [Tooltip("Strip rich-text tags from displayed lines.")]
        public bool stripRichTextTags = false;

        [Tooltip("Number of visible line objects (UI elements are pooled and reused).")]
        [Min(1)] public int poolSize = 20;

        [Tooltip("Lines kept in memory. Older lines are discarded.")]
        [Min(1)] public int maxBufferedLines = 500;

        [Header("Logger Integration")]
        [Tooltip("Show EldritchLogger entries in the console (registers a console log sink).")]
        public bool showEldritchLogs = true;

        [Tooltip("Minimum level of EldritchLogger entries shown in the console.")]
        public LogLevel minimumLoggerLevel = LogLevel.Debug;

        [Tooltip("Show messages sent through UnityEngine.Debug (Application.logMessageReceived).")]
        public bool showUnityLogs = true;

        [Tooltip("Also send command output to EldritchLogger (category 'Console'), e.g. to keep it in log files.")]
        public bool mirrorCommandOutputToLogger = false;

        [Tooltip("Maximum log entries waiting to be shown. When full, the oldest are dropped.")]
        [Min(1)] public int loggerQueueCapacity = 1000;

        [Header("Commands")]
        [Tooltip("Upper bound for 'repeat'.")]
        [Min(1)] public int maxRepeatCount = 1000;

        [Tooltip("Resources folder that contains ConsoleTheme assets.")]
        public string themesResourcePath = "Themes";
    }
}
