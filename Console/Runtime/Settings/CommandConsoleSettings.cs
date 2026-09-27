using UnityEngine;

namespace EldritchGames.EldritchLogger.Console.Settings
{
    [CreateAssetMenu(fileName = "Eldritch Console Settings", menuName = "Eldritch Logger/Console Settings")]
    public class CommandConsoleSettings : ScriptableObject
    {
        [Header("History Settings")]
        [Tooltip("Maximum number of commands stored in history.")]
        public int historySize = 50;

        [Tooltip("Default number of entries shown when running 'history' without flags.")]
        public int defaultHistoryLimit = 20;

        [Tooltip("Whether duplicate consecutive commands should be ignored.")]
        public bool ignoreConsecutiveDuplicates = true;

        [Header("Rendering Settings")]
        [Tooltip("Whether rich text tags should be stripped from the output.")]
        public bool stripRichTextTags = false;
        [Tooltip("Number of log entries to pool for reuse.")]
        public int poolSize = 20;

        [Header("Logger Integration")]
        [Tooltip("Show EldritchLogger entries in the console (registers a console log sink).")]
        public bool showEldritchLogs = true;
        [Tooltip("Show messages sent through UnityEngine.Debug (Application.logMessageReceived).")]
        public bool showUnityLogs = true;
    }
}
