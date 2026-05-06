using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Dto;
using EldritchGames.EldritchLogger.Format;
using EldritchGames.EldritchLogger.Settings;
using EldritchGames.EldritchLogger.Visuals;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace EldritchGames.EldritchLogger.UI
{
    [CustomEditor(typeof(LogSettings))]
    public class LogSettingsEditor : Editor
    {
        private bool showAdvanced = false;
        private string _newCategoryName = "";
        private string _addCategoryError = "";

        public override void OnInspectorGUI()
        {
            LogSettings settings = (LogSettings)target;

            DrawPresets(settings);
            DrawLogLevel(settings);
            DrawCategorySection(settings);
            DrawValidation(settings);
            DrawExport(settings);
            DrawAdvanced(settings);
            DrawPreview(settings);

            if (GUI.changed)
            {
                EditorUtility.SetDirty(settings);
            }
        }

        private void DrawPresets(LogSettings settings)
        {
            EditorGUILayout.LabelField("Presets", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("Verbose", "Enable all categories and lowest log level for maximum detail.")))
                settings.ApplyVerbosePreset();
            if (GUILayout.Button(new GUIContent("Normal", "Balanced logging for development use.")))
                settings.ApplyNormalPreset();
            if (GUILayout.Button(new GUIContent("Production", "Minimal logging for release builds.")))
                settings.ApplyProductionPreset();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
        }

        private void DrawCategorySection(LogSettings settings)
        {
            EditorGUILayout.LabelField(new GUIContent("Categories", "Enable/disable categories and customize their colors."), EditorStyles.boldLabel);

            // Ensure lists are initialized
            settings.enabledCategories ??= new List<LogCategory>();
            settings.categoryColors ??= new List<CategoryColor>();
            settings.customCategories ??= new List<CustomCategoryEntry>();

            // Make sure every built-in category has a color entry
            foreach (LogCategory cat in Enum.GetValues(typeof(LogCategory)))
            {
                if (!settings.categoryColors.Exists(c => c.category == cat))
                    settings.categoryColors.Add(new CategoryColor(cat, Color.white));
            }

            // --- Built-in categories ---
            foreach (var entry in settings.categoryColors)
            {
                EditorGUILayout.BeginHorizontal();

                bool enabled = settings.enabledCategories.Contains(entry.category);
                bool newEnabled = EditorGUILayout.ToggleLeft(entry.category.ToString(), enabled, GUILayout.Width(150));
                if (newEnabled && !enabled)
                    settings.enabledCategories.Add(entry.category);
                else if (!newEnabled && enabled)
                    settings.enabledCategories.Remove(entry.category);

                if (settings.useCategoryColors)
                    entry.color = EditorGUILayout.ColorField(entry.color);

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();

            // --- Custom categories ---
            EditorGUILayout.LabelField("Custom Categories", EditorStyles.boldLabel);

            var toRemove = new List<string>();
            foreach (var entry in settings.customCategories)
            {
                EditorGUILayout.BeginHorizontal();

                entry.enabled = EditorGUILayout.ToggleLeft(entry.name, entry.enabled, GUILayout.Width(150));

                if (settings.useCategoryColors)
                    entry.color = EditorGUILayout.ColorField(entry.color);

                if (GUILayout.Button("✕", GUILayout.Width(24)))
                    toRemove.Add(entry.name);

                EditorGUILayout.EndHorizontal();
            }

            foreach (var name in toRemove)
            {
                settings.RemoveCustomCategory(name);
                EditorUtility.SetDirty(settings);
            }

            // Add new custom category row
            EditorGUILayout.BeginHorizontal();
            _newCategoryName = EditorGUILayout.TextField(_newCategoryName);
            if (GUILayout.Button("Add", GUILayout.Width(50)))
            {
                if (settings.AddCustomCategory(_newCategoryName.Trim(), Color.white))
                {
                    _newCategoryName = "";
                    _addCategoryError = "";
                    EditorUtility.SetDirty(settings);
                }
                else
                {
                    _addCategoryError = string.IsNullOrWhiteSpace(_newCategoryName)
                        ? "Name cannot be empty."
                        : $"\"{_newCategoryName.Trim()}\" already exists or conflicts with a built-in category.";
                }
            }
            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(_addCategoryError))
                EditorGUILayout.HelpBox(_addCategoryError, MessageType.Error);

            EditorGUILayout.Space();

            // Bulk actions
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("Enable All", "Enable logging for all categories.")))
                settings.EnableAllCategories();
            if (GUILayout.Button(new GUIContent("Disable All", "Disable logging for all categories.")))
                settings.DisableAllCategories();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
        }


        private void DrawLogLevel(LogSettings settings)
        {
            settings.logLevel = (LogLevel)EditorGUILayout.EnumPopup(
                new GUIContent("Minimum Log Level", "Logs below this level will be ignored."),
                settings.logLevel);
            EditorGUILayout.Space();
        }

        private void DrawValidation(LogSettings settings)
        {
            if (settings.enabledCategories.Count == 0)
            {
                EditorGUILayout.HelpBox("No categories enabled. No logs will be output.", MessageType.Warning);
            }
        }

        private void DrawAdvanced(LogSettings settings)
        {
            showAdvanced = EditorGUILayout.Foldout(showAdvanced, "Advanced Settings");
            if (showAdvanced)
            {
                EditorGUILayout.LabelField("Timestamp Format", EditorStyles.boldLabel);
                settings.timestampFormat = EditorGUILayout.TextField(
                    new GUIContent("Format", "Custom date/time format string (e.g. yyyy-MM-dd HH:mm:ss)."),
                    settings.timestampFormat);

                EditorGUILayout.LabelField("Message Prefix", EditorStyles.boldLabel);
                settings.messagePrefix = EditorGUILayout.TextField(
                    new GUIContent("Prefix", "Optional text prepended to every log message."),
                    settings.messagePrefix);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Stack Trace Settings", EditorStyles.boldLabel);

                settings.suppressUnityStackTrace = EditorGUILayout.Toggle(
                    new GUIContent("Suppress Unity Stack Trace", "If enabled, Unity's automatic stack traces will be suppressed. EldritchLogger will handle exception traces itself."),
                    settings.suppressUnityStackTrace);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Context Settings", EditorStyles.boldLabel);

                settings.useContextObjects = EditorGUILayout.Toggle(
                    new GUIContent("Use Context Objects", "Attach Unity GameObject/Component context to logs."),
                    settings.useContextObjects);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Category Colors", EditorStyles.boldLabel);

                settings.useCategoryColors = EditorGUILayout.Toggle(
                    new GUIContent("Use Category Colors", "Enable per-category color customization in the inspector."),
                    settings.useCategoryColors);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Exception Filtering", EditorStyles.boldLabel);

                settings.filterLoggerFrames = EditorGUILayout.Toggle(
                    new GUIContent("Filter Logger Internals", "Remove logger framework internals from stack traces."),
                    settings.filterLoggerFrames);
            }
            EditorGUILayout.Space();
        }

        private void DrawPreview(LogSettings settings)
        {
            EditorGUILayout.LabelField(new GUIContent("Preview", "Shows a sample log entry with current settings applied."), EditorStyles.boldLabel);

            var sampleDto = SampleDto(settings);
            var formatter = new LogEntryFormatter(settings);
            string preview = formatter.Format(sampleDto);

            GUIStyle richTextStyle = new(EditorStyles.label)
            {
                richText = true,
                wordWrap = true
            };

            EditorGUILayout.LabelField(preview, richTextStyle, GUILayout.Height(100));
        }

        private void DrawExport(LogSettings settings)
        {
            EditorGUILayout.LabelField("Export Settings", EditorStyles.boldLabel);

            settings.enableExport = EditorGUILayout.Toggle(
                new GUIContent("Enable Export", "Toggle to write logs to disk."),
                settings.enableExport);

            if (settings.enableExport)
            {
                EditorGUILayout.LabelField("Formats", EditorStyles.boldLabel);

                // Ensure list is initialized
                settings.exportFormats ??= new List<ExportFormat>();

                DrawFormatToggle(settings, ExportFormat.Json, "JSON");
                DrawFormatToggle(settings, ExportFormat.Xml, "XML");
                DrawFormatToggle(settings, ExportFormat.Text, "Text");

                settings.exportFileName = EditorGUILayout.TextField(
                    new GUIContent("File Name", "Name of the log file without extension."),
                    settings.exportFileName);

                settings.exportDirectory = EditorGUILayout.TextField(
                    new GUIContent("Directory", "Target directory for exported logs. Leave empty to use Application.persistentDataPath."),
                    settings.exportDirectory);

                settings.clearOnStartup = EditorGUILayout.Toggle(
                    new GUIContent("Clear On Startup", "If enabled, previous session logs will be deleted when the logger initializes."),
                    settings.clearOnStartup);

                EditorGUILayout.HelpBox(
                    "If directory is empty, logs will be written to Application.persistentDataPath.",
                    MessageType.Info);
            }

            EditorGUILayout.Space();
        }

        private void DrawFormatToggle(LogSettings settings, ExportFormat fmt, string label)
        {
            bool enabled = settings.exportFormats.Contains(fmt);
            bool newEnabled = EditorGUILayout.Toggle(label, enabled);
            if (newEnabled && !enabled)
                settings.exportFormats.Add(fmt);
            else if (!newEnabled && enabled)
                settings.exportFormats.Remove(fmt);
        }


        public static LogEntryDto SampleDto(LogSettings settings) =>
            new()
            {
                Timestamp = DateTime.Now,
                Level = settings.logLevel.ToString(),
                Category = LogCategory.Gameplay.ToString(),
                Message = $"{settings.messagePrefix} Sample log message",
                Metadata = new List<MetadataEntry>
                {
                    new() { Key = "GameObject", Value = "PlayerPawn" }
                },
                Exception = "Preview exception message"
            };
    }
}
