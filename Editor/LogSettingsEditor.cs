using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.EditorTools.CodeGen;
using EldritchGames.EldritchLogger.EditorTools.ProjectSettings;
using EldritchGames.EldritchLogger.Sinks;
using EldritchGames.EldritchLogger.Sinks.Files;
using EldritchGames.EldritchLogger.Dto;
using EldritchGames.EldritchLogger.Formatting;
using EldritchGames.EldritchLogger.Settings;
using EldritchGames.EldritchLogger.Sinks.Config;
using EldritchGames.EldritchLogger.Sinks.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace EldritchGames.EldritchLogger.EditorTools
{
    [CustomEditor(typeof(LogSettings))]
    public class LogSettingsEditor : Editor
    {
        private SerializedProperty minimumLevel;
        private SerializedProperty useCategoryColors;
        private SerializedProperty timestampFormat;
        private SerializedProperty messagePrefix;
        private SerializedProperty filterLoggerFrames;
        private SerializedProperty sinks;
        private SerializedProperty autoInitialize;
        private SerializedProperty captureUnityLogs;

        private bool showAdvanced;
        private string newCategoryName = "";
        private string addCategoryError = "";
        private HashSet<string> unusedCategories;
        private LogEntryDto sample;
        private readonly HashSet<LogSinkConfig> previewOpen = new();
        private readonly Dictionary<LogSinkConfig, Task<(bool ok, string result)>> httpTests = new();

        private void OnEnable()
        {
            minimumLevel = serializedObject.FindProperty(nameof(LogSettings.minimumLevel));
            useCategoryColors = serializedObject.FindProperty(nameof(LogSettings.useCategoryColors));
            timestampFormat = serializedObject.FindProperty(nameof(LogSettings.timestampFormat));
            messagePrefix = serializedObject.FindProperty(nameof(LogSettings.messagePrefix));
            filterLoggerFrames = serializedObject.FindProperty(nameof(LogSettings.filterLoggerFrames));
            sinks = serializedObject.FindProperty(nameof(LogSettings.sinks));
            autoInitialize = serializedObject.FindProperty(nameof(LogSettings.autoInitialize));
            captureUnityLogs = serializedObject.FindProperty(nameof(LogSettings.captureUnityLogs));
        }

        public override void OnInspectorGUI()
        {
            var settings = (LogSettings)target;

            DrawPresets(settings);

            serializedObject.Update();
            EditorGUILayout.PropertyField(minimumLevel, new GUIContent("Minimum Log Level"));
            EditorGUILayout.Space();
            serializedObject.ApplyModifiedProperties();

            DrawCategories(settings);

            serializedObject.Update();
            DrawSinks();
            DrawUnityCapture(settings);
            DrawAdvanced();
            serializedObject.ApplyModifiedProperties();

            DrawPreview(settings);
            DrawRunningSinks();
        }

        public override bool RequiresConstantRepaint() =>
            EditorApplication.isPlaying || httpTests.Values.Any(t => !t.IsCompleted);

        /// <summary>Built once (it runs a small logger), and again when the minimum level changes.</summary>
        private LogEntryDto Sample(LogSettings settings)
        {
            if (sample == null || sample.Level != settings.minimumLevel) sample = SinkPreview.CreateSample(settings);
            return sample;
        }

        private static void DrawRunningSinks()
        {
            if (!EditorApplication.isPlaying) return;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(new GUIContent("Running Sinks", "Sinks attached to the logger in this Play Mode session."), EditorStyles.boldLabel);

            var registry = ELoggerFactory.Sinks;
            if (registry == null)
            {
                EditorGUILayout.HelpBox("No logger is installed.", MessageType.Info);
                return;
            }

            foreach (var sink in registry.All)
            {
                var diagnostics = sink as ISinkDiagnostics;
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                EditorGUILayout.LabelField(sink.Name, EditorStyles.boldLabel, GUILayout.MinWidth(120));
                EditorGUILayout.LabelField($"≥ {sink.MinimumLevel}", GUILayout.Width(80));
                if (diagnostics != null)
                {
                    var dropped = diagnostics.DroppedCount;
                    var style = dropped > 0 ? EditorStyles.boldLabel : EditorStyles.label;
                    EditorGUILayout.LabelField($"dropped: {dropped}", style, GUILayout.Width(100));
                    if (!string.IsNullOrEmpty(diagnostics.Location) && System.IO.File.Exists(diagnostics.Location) &&
                        GUILayout.Button(new GUIContent("Reveal", diagnostics.Location), GUILayout.Width(60)))
                        EditorUtility.RevealInFinder(diagnostics.Location);
                }
                EditorGUILayout.EndHorizontal();

                if (sink is BatchingLogSink batching)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.LabelField($"queued {batching.QueuedCount} · sent {batching.SentCount} · retries {batching.RetryCount}", EditorStyles.miniLabel);
                    if (batching.LastError != null)
                        EditorGUILayout.HelpBox($"Last error ({batching.LastErrorUtc?.ToLocalTime():HH:mm:ss}): {batching.LastError}", MessageType.Warning);
                    EditorGUI.indentLevel--;
                }
            }
        }

        private static void RevealDirectory(string directory)
        {
            System.IO.Directory.CreateDirectory(directory);
            EditorUtility.RevealInFinder(directory);
        }

        private void DrawPresets(LogSettings settings)
        {
            EditorGUILayout.LabelField("Presets", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("Verbose", "All categories, Debug level.")))
                Modify(settings, "Apply Verbose Preset", LogSettingsPresets.ApplyVerbose);
            if (GUILayout.Button(new GUIContent("Normal", "Balanced logging for development.")))
                Modify(settings, "Apply Normal Preset", LogSettingsPresets.ApplyNormal);
            if (GUILayout.Button(new GUIContent("Production", "Warnings and above, core categories.")))
                Modify(settings, "Apply Production Preset", LogSettingsPresets.ApplyProduction);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
        }

        private void DrawCategories(LogSettings settings)
        {
            EditorGUILayout.LabelField(new GUIContent("Categories", "Enable/disable categories and customize their colors."), EditorStyles.boldLabel);

            string toRemove = null;
            EditorGUI.BeginChangeCheck();
            var edits = new List<(CategorySetting entry, bool enabled, Color color)>();

            foreach (var entry in settings.categories)
            {
                EditorGUILayout.BeginHorizontal();
                bool enabled = EditorGUILayout.ToggleLeft(entry.name, entry.enabled, GUILayout.Width(150));
                Color color = settings.useCategoryColors ? EditorGUILayout.ColorField(entry.color) : entry.color;
                if (unusedCategories != null && unusedCategories.Contains(entry.name))
                    GUILayout.Label(new GUIContent("unused?", "No script under Assets/ mentions this category (as a string or generated field)."),
                                    EditorStyles.miniButton, GUILayout.Width(60));

                if (entry.IsBuiltIn)
                    GUILayout.Space(28);
                else if (GUILayout.Button(new GUIContent("✕", "Remove category"), GUILayout.Width(24)))
                    toRemove = entry.name;

                EditorGUILayout.EndHorizontal();
                edits.Add((entry, enabled, color));
            }

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(settings, "Edit Log Categories");
                foreach (var (entry, enabled, color) in edits)
                {
                    entry.enabled = enabled;
                    entry.color = color;
                }
                EditorUtility.SetDirty(settings);
            }

            if (toRemove != null)
            {
                Modify(settings, "Remove Log Category", s => s.RemoveCategory(toRemove));
                CategoryCodeGeneration.GenerateIfEnabled(settings);
            }

            EditorGUILayout.BeginHorizontal();
            newCategoryName = EditorGUILayout.TextField(newCategoryName);
            if (GUILayout.Button("Add", GUILayout.Width(50)))
            {
                var name = newCategoryName.Trim();
                bool added = false;
                Modify(settings, "Add Log Category", s => added = s.AddCategory(name, Color.white));
                if (added)
                {
                    CategoryCodeGeneration.GenerateIfEnabled(settings);
                    newCategoryName = "";
                    addCategoryError = "";
                }
                else
                {
                    addCategoryError = string.IsNullOrWhiteSpace(name)
                        ? "Name cannot be empty."
                        : $"\"{name}\" already exists.";
                }
            }
            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(addCategoryError))
                EditorGUILayout.HelpBox(addCategoryError, MessageType.Error);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Enable All"))
                Modify(settings, "Enable All Categories", s => s.SetAllCategoriesEnabled(true));
            if (GUILayout.Button("Disable All"))
                Modify(settings, "Disable All Categories", s => s.SetAllCategoriesEnabled(false));
            if (GUILayout.Button(new GUIContent("Generate Class", "Generate a static class with one field per category (see Project Settings > Eldritch Logger)."), GUILayout.Width(110)))
                CategoryCodeGeneration.Generate(settings);
            if (GUILayout.Button(new GUIContent("⚙", "Code generation options"), GUILayout.Width(24)))
                EldritchLoggerSettingsProvider.Open();
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button(new GUIContent("Find Unused Categories", "Search scripts under Assets/ for custom categories nobody logs to.")))
                unusedCategories = new HashSet<string>(CategoryUsage.FindUnreferencedInProject(settings), StringComparer.OrdinalIgnoreCase);
            if (unusedCategories != null)
            {
                var message = unusedCategories.Count == 0
                    ? "Every custom category is mentioned by at least one script."
                    : $"Not mentioned by any script: {string.Join(", ", unusedCategories)}. The search is textual (names built at runtime are not found), so check before removing.";
                EditorGUILayout.HelpBox(message, unusedCategories.Count == 0 ? MessageType.Info : MessageType.Warning);
            }

            if (!settings.categories.Any(c => c.enabled))
                EditorGUILayout.HelpBox("No categories enabled. No logs will be output.", MessageType.Warning);

            EditorGUILayout.Space();
        }

        private void DrawSinks()
        {
            EditorGUILayout.LabelField(new GUIContent("Sinks", "Where log entries are written."), EditorStyles.boldLabel);

            int removeIndex = -1;
            for (int i = 0; i < sinks.arraySize; i++)
            {
                var element = sinks.GetArrayElementAtIndex(i);
                var config = element.managedReferenceValue as LogSinkConfig;

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                element.isExpanded = EditorGUILayout.Foldout(element.isExpanded,
                    config != null ? config.DisplayName : "(missing sink type)", true);
                if (config is FileSinkConfig fileConfig && GUILayout.Button(new GUIContent("Open Folder", "Reveal the log directory."), GUILayout.Width(90)))
                    RevealDirectory(LogFileLocator.ResolveDirectory(fileConfig.directory));
                if (GUILayout.Button(new GUIContent("✕", "Remove sink"), GUILayout.Width(24)))
                    removeIndex = i;
                EditorGUILayout.EndHorizontal();

                if (element.isExpanded && config != null)
                {
                    EditorGUI.indentLevel++;
                    var child = element.Copy();
                    var end = element.GetEndProperty();
                    if (child.NextVisible(true))
                    {
                        do
                        {
                            if (SerializedProperty.EqualContents(child, end)) break;
                            EditorGUILayout.PropertyField(child, true);
                        } while (child.NextVisible(false));
                    }
                    if (config is HttpSinkConfig http) DrawHttpTest(http);
                    DrawSinkPreview(config);
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndVertical();
            }

            if (removeIndex >= 0)
                sinks.DeleteArrayElementAtIndex(removeIndex);

            if (GUILayout.Button("Add Sink ▾"))
                ShowAddSinkMenu();

            EditorGUILayout.Space();
        }

        private void DrawSinkPreview(LogSinkConfig config)
        {
            var settings = (LogSettings)target;
            string preview;
            try
            {
                preview = config.Preview(Sample(settings), settings);
            }
            catch (Exception ex)
            {
                preview = $"(preview failed: {ex.Message})";
            }
            if (preview == null) return;

            bool open = EditorGUILayout.Foldout(previewOpen.Contains(config), new GUIContent("Output Preview", "How a sample entry (template, scope and exception) is written by this sink."), true);
            if (open) previewOpen.Add(config); else previewOpen.Remove(config);
            if (!open) return;

            if (config.PreviewIsRichText)
            {
                var style = new GUIStyle(EditorStyles.label) { richText = true, wordWrap = true };
                EditorGUILayout.LabelField(preview, style);
            }
            else
            {
                var style = new GUIStyle(EditorStyles.textArea) { wordWrap = true };
                float height = style.CalcHeight(new GUIContent(preview), EditorGUIUtility.currentViewWidth - 60);
                EditorGUILayout.SelectableLabel(preview, style, GUILayout.Height(Mathf.Min(height, 200)));
            }
        }

        private void DrawHttpTest(HttpSinkConfig config)
        {
            httpTests.TryGetValue(config, out var test);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(EditorGUI.indentLevel * 15);
                using (new EditorGUI.DisabledScope(test != null && !test.IsCompleted))
                {
                    if (GUILayout.Button(new GUIContent("Send Test Entry", "Post the sample entry once with these settings, to check the URL and headers."), GUILayout.Width(130)))
                    {
                        var entry = Sample((LogSettings)target);
                        httpTests[config] = Task.Run(() => (config.TrySendTest(entry, out var result), result));
                    }
                }

                if (test == null) GUILayout.FlexibleSpace();
                else if (!test.IsCompleted) GUILayout.Label("Sending…");
                else
                {
                    var (ok, result) = test.Result;
                    GUILayout.Label(new GUIContent((ok ? "✔ " : "✖ ") + result, result), ok ? EditorStyles.label : EditorStyles.boldLabel);
                }
            }
        }

        private void ShowAddSinkMenu()
        {
            var menu = new GenericMenu();
            var types = TypeCache.GetTypesDerivedFrom<LogSinkConfig>()
                .Where(t => !t.IsAbstract && !t.IsGenericType && t.GetConstructor(Type.EmptyTypes) != null)
                .OrderBy(t => t.Name);

            foreach (var type in types)
            {
                var label = ((LogSinkConfig)Activator.CreateInstance(type)).DisplayName;
                menu.AddItem(new GUIContent(label), false, () =>
                {
                    serializedObject.Update();
                    int index = sinks.arraySize;
                    sinks.InsertArrayElementAtIndex(index);
                    var element = sinks.GetArrayElementAtIndex(index);
                    element.managedReferenceValue = Activator.CreateInstance(type);
                    element.isExpanded = true;
                    serializedObject.ApplyModifiedProperties();
                });
            }

            menu.ShowAsContext();
        }

        private void DrawUnityCapture(LogSettings settings)
        {
            EditorGUILayout.PropertyField(captureUnityLogs, new GUIContent("Capture Unity Logs",
                "Forward Unity's own messages (Debug.Log, engine errors, uncaught exceptions) into the logger under the 'Unity' category."));

            if (settings.captureUnityLogs != Pipeline.UnityLogCapture.Off && !settings.IsCategoryEnabled(LogCategory.Unity))
            {
                EditorGUILayout.HelpBox("The 'Unity' category is missing or disabled, so captured messages are discarded.", MessageType.Warning);
                if (GUILayout.Button("Enable 'Unity' category"))
                {
                    Modify(settings, "Enable Unity Category", s =>
                    {
                        if (!s.AddCategory(LogCategory.Unity.Name, Color.gray))
                            s.FindCategory(LogCategory.Unity).enabled = true;
                    });
                    CategoryCodeGeneration.GenerateIfEnabled(settings);
                }
            }
            EditorGUILayout.Space();
        }

        private void DrawAdvanced()
        {
            showAdvanced = EditorGUILayout.Foldout(showAdvanced, "Advanced Settings", true);
            if (!showAdvanced) return;

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(timestampFormat, new GUIContent("Timestamp Format", "e.g. yyyy-MM-dd HH:mm:ss"));
            EditorGUILayout.PropertyField(messagePrefix, new GUIContent("Message Prefix"));
            EditorGUILayout.PropertyField(useCategoryColors, new GUIContent("Use Category Colors"));
            EditorGUILayout.PropertyField(filterLoggerFrames, new GUIContent("Filter Logger Internals", "Remove logger frames from exception stack traces."));
            EditorGUILayout.PropertyField(autoInitialize, new GUIContent("Auto Initialize", "Create the logger before the first scene loads."));
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }

        private void DrawPreview(LogSettings settings)
        {
            EditorGUILayout.LabelField(new GUIContent("Preview", "A sample entry with the current settings."), EditorStyles.boldLabel);

            string preview = new TextLogFormatter(settings, richText: true).Format(Sample(settings));
            var style = new GUIStyle(EditorStyles.label) { richText = true, wordWrap = true };
            EditorGUILayout.LabelField(preview, style);
        }

        private static void Modify(LogSettings settings, string undoName, Action<LogSettings> change)
        {
            Undo.RecordObject(settings, undoName);
            change(settings);
            EditorUtility.SetDirty(settings);
        }

        /// <summary>The sample entry shown by the previews (see <see cref="SinkPreview.CreateSample"/>).</summary>
        public static LogEntryDto SampleDto(LogSettings settings) => SinkPreview.CreateSample(settings);
    }
}
