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
        }
        EditorGUILayout.Space();
    }

    private void DrawPreview(LogSettings settings)
    {
        EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
        string preview = $"[{System.DateTime.Now.ToString(settings.timestampFormat)}] " +
                         $"[{settings.logLevel}] Gameplay {settings.messagePrefix} Sample log message";
        EditorGUILayout.HelpBox(preview, MessageType.None);
    }
}