using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Dto;
using EldritchGames.EldritchLogger.Formatting;
using EldritchGames.EldritchLogger.Settings;
using EldritchGames.EldritchLogger.Sinks.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EldritchGames.EldritchLogger.UI
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

        private bool showAdvanced;
        private string newCategoryName = "";
        private string addCategoryError = "";

        private void OnEnable()
        {
            minimumLevel = serializedObject.FindProperty(nameof(LogSettings.minimumLevel));
            useCategoryColors = serializedObject.FindProperty(nameof(LogSettings.useCategoryColors));
            timestampFormat = serializedObject.FindProperty(nameof(LogSettings.timestampFormat));
            messagePrefix = serializedObject.FindProperty(nameof(LogSettings.messagePrefix));
            filterLoggerFrames = serializedObject.FindProperty(nameof(LogSettings.filterLoggerFrames));
            sinks = serializedObject.FindProperty(nameof(LogSettings.sinks));
            autoInitialize = serializedObject.FindProperty(nameof(LogSettings.autoInitialize));
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
            DrawAdvanced();
            serializedObject.ApplyModifiedProperties();

            DrawPreview(settings);
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
                Modify(settings, "Remove Log Category", s => s.RemoveCategory(toRemove));

            EditorGUILayout.BeginHorizontal();
            newCategoryName = EditorGUILayout.TextField(newCategoryName);
            if (GUILayout.Button("Add", GUILayout.Width(50)))
            {
                var name = newCategoryName.Trim();
                bool added = false;
                Modify(settings, "Add Log Category", s => added = s.AddCategory(name, Color.white));
                if (added)
                {
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
            EditorGUILayout.EndHorizontal();

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

        private static void DrawPreview(LogSettings settings)
        {
            EditorGUILayout.LabelField(new GUIContent("Preview", "A sample entry with the current settings."), EditorStyles.boldLabel);

            string preview = new TextLogFormatter(settings, richText: true).Format(SampleDto(settings));
            var style = new GUIStyle(EditorStyles.label) { richText = true, wordWrap = true };
            EditorGUILayout.LabelField(preview, style, GUILayout.Height(60));
        }

        private static void Modify(LogSettings settings, string undoName, Action<LogSettings> change)
        {
            Undo.RecordObject(settings, undoName);
            change(settings);
            EditorUtility.SetDirty(settings);
        }

        public static LogEntryDto SampleDto(LogSettings settings) =>
            new()
            {
                Timestamp = DateTime.UtcNow,
                Level = settings.minimumLevel,
                Category = LogCategory.Gameplay.Name,
                Message = "Sample log message",
                Metadata = new List<MetadataEntry> { new() { Key = LogPropertyKeys.GameObject, Value = "PlayerPawn" } },
                Exception = "InvalidOperationException: Preview exception message"
            };
    }
}
