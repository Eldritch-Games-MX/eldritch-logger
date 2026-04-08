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
            if (settings.enabledCategories == null)
                settings.enabledCategories = new List<LogCategory>();
            if (settings.categoryColors == null)
                settings.categoryColors = new List<CategoryColor>();

            // Make sure every category has a color entry
            foreach (LogCategory cat in Enum.GetValues(typeof(LogCategory)))
            {
                if (!settings.categoryColors.Exists(c => c.category == cat))
                    settings.categoryColors.Add(new CategoryColor(cat, Color.white));
            }

            // Draw each category row
            foreach (var entry in settings.categoryColors)
            {
                EditorGUILayout.BeginHorizontal();

                // Toggle for enabling/disabling category
                bool enabled = settings.enabledCategories.Contains(entry.category);
                bool newEnabled = EditorGUILayout.ToggleLeft(entry.category.ToString(), enabled, GUILayout.Width(150));
                if (newEnabled && !enabled)
                    settings.enabledCategories.Add(entry.category);
                else if (!newEnabled && enabled)
                    settings.enabledCategories.Remove(entry.category);

                // Color picker
                entry.color = EditorGUILayout.ColorField(entry.color);

                EditorGUILayout.EndHorizontal();
            }

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

            GUIStyle richTextStyle = new GUIStyle(EditorStyles.label)
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
                if (settings.exportFormats == null)
                    settings.exportFormats = new List<ExportFormat>();

                DrawFormatToggle(settings, ExportFormat.Json, "JSON");
                DrawFormatToggle(settings, ExportFormat.Xml, "XML");
                DrawFormatToggle(settings, ExportFormat.Text, "Text");

                settings.exportFileName = EditorGUILayout.TextField(
                    new GUIContent("File Name", "Name of the log file without extension."),
                    settings.exportFileName);

                settings.exportDirectory = EditorGUILayout.TextField(
                    new GUIContent("Directory", "Target directory for exported logs. Leave empty to use Application.persistentDataPath."),
                    settings.exportDirectory);

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
            new LogEntryDto
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
