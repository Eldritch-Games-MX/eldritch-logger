using EldritchGames.EldritchLogger.EditorTools.CodeGen;
using EldritchGames.EldritchLogger.Settings;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EldritchGames.EldritchLogger.EditorTools.ProjectSettings
{
    /// <summary>Edit &gt; Project Settings &gt; Eldritch Logger.</summary>
    public sealed class EldritchLoggerSettingsProvider : SettingsProvider
    {
        public const string Path = "Project/Eldritch Logger";

        private UnityEditor.Editor settingsEditor;
        private Vector2 scroll;

        private EldritchLoggerSettingsProvider()
            : base(Path, SettingsScope.Project, new HashSet<string> { "log", "logger", "logging", "sink", "category" }) { }

        [SettingsProvider]
        public static SettingsProvider Create() => new EldritchLoggerSettingsProvider();

        [MenuItem("Tools/Eldritch Logger/Project Settings")]
        public static void Open() => SettingsService.OpenProjectSettings(Path);

        public override void OnDeactivate()
        {
            if (settingsEditor != null) Object.DestroyImmediate(settingsEditor);
            settingsEditor = null;
        }

        public override void OnGUI(string searchContext)
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUIUtility.labelWidth = 220;

            var settings = LogSettingsAssets.FindActive();
            DrawSettingsAsset(settings);
            EditorGUILayout.Space(12);
            DrawCodeGeneration(settings);

            if (settings != null)
            {
                EditorGUILayout.Space(12);
                EditorGUILayout.LabelField("Log Settings", EditorStyles.boldLabel);
                UnityEditor.Editor.CreateCachedEditor(settings, null, ref settingsEditor);
                settingsEditor.OnInspectorGUI();
            }

            EditorGUILayout.EndScrollView();
        }

        private static void DrawSettingsAsset(LogSettings settings)
        {
            EditorGUILayout.LabelField("Settings Asset", EditorStyles.boldLabel);

            if (settings == null)
            {
                var misplaced = LogSettingsAssets.FindAllPaths();
                EditorGUILayout.HelpBox(
                    "No LogSettings asset found at Resources/LogSettings. The logger will not start." +
                    (misplaced.Length > 0 ? "\nFound LogSettings assets that are not loadable:\n  " + string.Join("\n  ", misplaced) : ""),
                    MessageType.Warning);
                if (GUILayout.Button($"Create {LogSettingsAssets.DefaultPath}"))
                    LogSettingsAssets.CreateDefault();
                return;
            }

            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.ObjectField("Active settings", settings, typeof(LogSettings), false);
        }

        private static void DrawCodeGeneration(LogSettings settings)
        {
            var options = EldritchLoggerProjectSettings.instance;
            EditorGUILayout.LabelField("Category Code Generation", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Generates a static class with one LogCategory field per category, so category names are checked by the compiler.",
                MessageType.None);

            EditorGUI.BeginChangeCheck();
            options.categoriesOutputPath = EditorGUILayout.TextField("Output file", options.categoriesOutputPath);
            options.categoriesNamespace = EditorGUILayout.TextField("Namespace", options.categoriesNamespace);
            options.categoriesClassName = EditorGUILayout.TextField("Class name", options.categoriesClassName);
            options.autoGenerateCategories = EditorGUILayout.Toggle(
                new GUIContent("Regenerate automatically", "Regenerate when categories are added or removed in the LogSettings inspector."),
                options.autoGenerateCategories);
            if (EditorGUI.EndChangeCheck()) options.SaveSettings();

            using (new EditorGUI.DisabledScope(settings == null))
            {
                if (GUILayout.Button("Generate Now", GUILayout.Width(160)))
                {
                    bool written = CategoryCodeGeneration.Generate(settings);
                    Debug.Log(written
                        ? $"[EldritchLogger] Generated {options.categoriesOutputPath} ({settings.categories.Count} categories)."
                        : $"[EldritchLogger] {options.categoriesOutputPath} is already up to date.");
                }
            }
        }
    }
}
