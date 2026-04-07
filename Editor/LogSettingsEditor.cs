using EldritchGames.EldritchLogger;
using EldritchGames.EldritchLogger.Settings;
using EldritchGames.EldritchLogger.Visuals;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

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
        if (GUILayout.Button("Verbose"))
        {
            settings.logLevel = LogLevel.Debug;
            settings.enabledCategories = new List<LogCategory>(
                (LogCategory[])System.Enum.GetValues(typeof(LogCategory)));
        }
        if (GUILayout.Button("Normal"))
        {
            settings.logLevel = LogLevel.Info;
            settings.enabledCategories = new List<LogCategory>
            {
                LogCategory.Gameplay,
                LogCategory.UI,
                LogCategory.Network
            };
        }
        if (GUILayout.Button("Production"))
        {
            settings.logLevel = LogLevel.Warning;
            settings.enabledCategories = new List<LogCategory>
            {
                LogCategory.Gameplay,
                LogCategory.Network
            };
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();
    }

    private void DrawLogLevel(LogSettings settings)
    {
        settings.logLevel = (LogLevel)EditorGUILayout.EnumPopup("Minimum Log Level", settings.logLevel);
        EditorGUILayout.Space();
    }

    private void DrawCategorySection(LogSettings settings)
    {
        EditorGUILayout.LabelField("Enabled Categories", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Enable All"))
        {
            settings.enabledCategories = new List<LogCategory>(
                (LogCategory[])System.Enum.GetValues(typeof(LogCategory)));
        }
        if (GUILayout.Button("Disable All"))
        {
            settings.enabledCategories.Clear();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        foreach (LogCategory category in System.Enum.GetValues(typeof(LogCategory)))
        {
            bool enabled = settings.enabledCategories.Contains(category);
            EditorGUILayout.BeginHorizontal();

            // Color preview
            Color color = LogColors.GetColor(category);
            GUIStyle colorBox = new GUIStyle(GUI.skin.box);
            colorBox.normal.background = Texture2D.whiteTexture;
            Color oldColor = GUI.color;
            GUI.color = color;
            GUILayout.Box("", colorBox, GUILayout.Width(20), GUILayout.Height(20));
            GUI.color = oldColor;

            // Toggle
            bool newEnabled = EditorGUILayout.Toggle(category.ToString(), enabled);
            if (newEnabled && !enabled)
                settings.enabledCategories.Add(category);
            else if (!newEnabled && enabled)
                settings.enabledCategories.Remove(category);

            EditorGUILayout.EndHorizontal();
        }

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
            settings.timestampFormat = EditorGUILayout.TextField(settings.timestampFormat);

            EditorGUILayout.LabelField("Message Prefix", EditorStyles.boldLabel);
            settings.messagePrefix = EditorGUILayout.TextField(settings.messagePrefix);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Stack Trace Settings", EditorStyles.boldLabel);

            settings.infoTrace = (StackTraceMode)EditorGUILayout.EnumPopup("Info/Debug Trace", settings.infoTrace);
            settings.warningTrace = (StackTraceMode)EditorGUILayout.EnumPopup("Warning Trace", settings.warningTrace);
            settings.errorTrace = (StackTraceMode)EditorGUILayout.EnumPopup("Error/Critical Trace", settings.errorTrace);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Context Settings", EditorStyles.boldLabel);

            settings.useContextObjects = EditorGUILayout.Toggle("Use Context Objects", settings.useContextObjects);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Exception Filtering", EditorStyles.boldLabel);

            settings.filterLoggerFrames = EditorGUILayout.Toggle("Filter Logger Internals", settings.filterLoggerFrames);
        }
        EditorGUILayout.Space();
    }
    private void DrawPreview(LogSettings settings)
    {
        EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

        // Build a sample LogEntry
        var sampleEntry = new LogEntry
        {
            Timestamp = System.DateTime.Now,
            Level = settings.logLevel,
            Category = LogCategory.Gameplay,
            Message = $"{settings.messagePrefix} Sample log message",
            Metadata = new Dictionary<string, object>
        {
            { "GameObject", "PlayerPawn" }
        },
            Exception = new System.Exception("Preview exception message")
        };

        string preview = sampleEntry.ToString();

        // Style with rich text enabled
        GUIStyle richTextStyle = new GUIStyle(EditorStyles.label)
        {
            richText = true,
            wordWrap = true
        };

        // Severity coloring
        string severityColor = settings.logLevel switch
        {
            LogLevel.Warning => "yellow",
            LogLevel.Error => "red",
            LogLevel.Critical => "red",
            LogLevel.Debug => "white",
            LogLevel.Info => "white",
            _ => "white"
        };

        // Category coloring (using your LogColors)
        string categoryColor = LogColors.GetColorString(LogCategory.Gameplay);

        // Replace category text with colored version
        string coloredPreview = preview.Replace(
            "Gameplay",
            $"<color={categoryColor}>Gameplay</color>"
        );

        // Wrap entire preview in severity color
        coloredPreview = $"<color={severityColor}>{coloredPreview}</color>";

        EditorGUILayout.LabelField(coloredPreview, richTextStyle, GUILayout.Height(100));
    }
}