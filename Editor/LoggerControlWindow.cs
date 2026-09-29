using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Sinks;
using EldritchGames.EldritchLogger.Sinks.Network;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EldritchGames.EldritchLogger.EditorTools
{
    /// <summary>
    /// Play Mode panel for the running logger: level and category overrides (never written to the asset),
    /// the running sinks with their statistics, flushing, and a test entry. The editor counterpart of the
    /// console's <c>log.*</c> commands, for scenes without an in-game console.
    /// </summary>
    public sealed class LoggerControlWindow : EditorWindow
    {
        private Vector2 scroll;
        private string testMessage = "Test entry";
        private LogLevel testLevel = LogLevel.Info;

        [MenuItem("Tools/Eldritch Logger/Logger Control")]
        public static void Open() => GetWindow<LoggerControlWindow>("Logger Control");

        private void OnInspectorUpdate()
        {
            if (EditorApplication.isPlaying) Repaint();
        }

        private void OnGUI()
        {
            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to control the running logger. Overrides last for the session only; the LogSettings asset is never changed.", MessageType.Info);
                return;
            }

            var control = ELoggerFactory.Control;
            var sinks = ELoggerFactory.Sinks;
            if (control == null)
            {
                EditorGUILayout.HelpBox("No logger is installed, or it uses a custom filter that has no runtime control.", MessageType.Info);
                return;
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);
            DrawLevel(control, sinks);
            EditorGUILayout.Space();
            DrawCategories(control);
            EditorGUILayout.Space();
            DrawSinks(control, sinks);
            EditorGUILayout.Space();
            DrawTestEntry();
            EditorGUILayout.EndScrollView();
        }

        private static void DrawLevel(ILogControl control, ISinkRegistry sinks)
        {
            EditorGUILayout.LabelField("Minimum Level", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                var level = (LogLevel)EditorGUILayout.EnumPopup(control.MinimumLevel);
                if (level != control.MinimumLevel) control.MinimumLevelOverride = level;

                GUILayout.Label(control.MinimumLevelOverride.HasValue ? "override" : "from settings", EditorStyles.miniLabel, GUILayout.Width(80));
                using (new EditorGUI.DisabledScope(!control.MinimumLevelOverride.HasValue))
                    if (GUILayout.Button("Reset", GUILayout.Width(60)))
                        control.MinimumLevelOverride = null;
            }

            if (sinks.LevelHiddenBySinks(control.MinimumLevel) is { } lowest)
                EditorGUILayout.HelpBox($"No sink accepts entries below {lowest}, so {control.MinimumLevel} entries are still discarded. Lower a sink's minimum level in the LogSettings asset.", MessageType.Warning);
        }

        private static void DrawCategories(ILogControl control)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Categories", EditorStyles.boldLabel);
                if (GUILayout.Button("Reset All Overrides", GUILayout.Width(140)))
                    control.ClearOverrides();
            }

            foreach (var state in control.Categories.OrderBy(c => c.Category.Name))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    bool enabled = EditorGUILayout.ToggleLeft(state.Category.Name, state.Enabled);
                    if (enabled != state.Enabled) control.SetCategoryOverride(state.Category, enabled);

                    if (state.Overridden)
                    {
                        GUILayout.Label("override", EditorStyles.miniLabel, GUILayout.Width(55));
                        if (GUILayout.Button("Reset", GUILayout.Width(60))) control.SetCategoryOverride(state.Category, null);
                    }
                }
            }
        }

        private static void DrawSinks(ILogControl control, ISinkRegistry sinks)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Sinks", EditorStyles.boldLabel);
                if (GUILayout.Button(new GUIContent("Flush", "Write out everything buffered by the sinks."), GUILayout.Width(60)))
                    control.Flush();
            }

            if (sinks == null) return;
            foreach (var sink in sinks.All)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(sink.Name, EditorStyles.boldLabel);
                        GUILayout.Label($"≥ {sink.MinimumLevel}", GUILayout.Width(80));
                    }

                    if (sink is ISinkDiagnostics diagnostics)
                    {
                        var line = $"dropped {diagnostics.DroppedCount}";
                        if (sink is BatchingLogSink batching)
                            line = $"queued {batching.QueuedCount} · sent {batching.SentCount} · retries {batching.RetryCount} · " + line;
                        EditorGUILayout.LabelField(line, EditorStyles.miniLabel);

                        if (!string.IsNullOrEmpty(diagnostics.Location))
                        {
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                EditorGUILayout.SelectableLabel(diagnostics.Location, EditorStyles.miniLabel, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                                if (File.Exists(diagnostics.Location) && GUILayout.Button("Reveal", GUILayout.Width(60)))
                                    EditorUtility.RevealInFinder(diagnostics.Location);
                            }
                        }
                    }

                    if (sink is BatchingLogSink { LastError: { } error } failing)
                        EditorGUILayout.HelpBox($"Last error ({failing.LastErrorUtc?.ToLocalTime():HH:mm:ss}): {error}", MessageType.Warning);
                }
            }
        }

        private void DrawTestEntry()
        {
            EditorGUILayout.LabelField("Test Entry", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                testLevel = (LogLevel)EditorGUILayout.EnumPopup(testLevel, GUILayout.Width(80));
                testMessage = EditorGUILayout.TextField(testMessage);
                if (GUILayout.Button("Log", GUILayout.Width(50)))
                {
                    var logger = ELoggerFactory.GetLogger("LoggerControl");
                    if (!logger.IsEnabled(testLevel, LogCategory.General))
                        Debug.LogWarning($"[EldritchLogger] {testLevel} entries in General are filtered out right now; the test entry was discarded.");
                    logger.Log(testLevel, LogCategory.General, testMessage);
                }
            }
        }
    }
}
