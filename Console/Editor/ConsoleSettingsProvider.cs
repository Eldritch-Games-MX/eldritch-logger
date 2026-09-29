using EldritchGames.EldritchLogger.Console.Settings;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Console.EditorTools
{
    /// <summary>Edit &gt; Project Settings &gt; Eldritch Logger &gt; Console: release safety at a glance.</summary>
    public sealed class ConsoleSettingsProvider : SettingsProvider
    {
        public const string Path = "Project/Eldritch Logger/Console";

        private ConsoleSettingsProvider()
            : base(Path, SettingsScope.Project, new HashSet<string> { "console", "cheat", "release", "command" }) { }

        [SettingsProvider]
        public static SettingsProvider Create() => new ConsoleSettingsProvider();

        public override void OnGUI(string searchContext)
        {
            EditorGUIUtility.labelWidth = 220;
            DrawDefine();
            EditorGUILayout.Space(12);
            DrawSettingsAssets();
            EditorGUILayout.Space(12);
            if (GUILayout.Button("Open Commands Window", GUILayout.Width(200)))
                CommandsWindow.Open();
        }

        private static void DrawDefine()
        {
            EditorGUILayout.LabelField("Release Safety", EditorStyles.boldLabel);

            var target = ConsoleDefines.CurrentTarget;
            bool disabled = ConsoleDefines.HasDefine(target);
            bool newValue = EditorGUILayout.Toggle(
                new GUIContent($"Disable console ({target.TargetName})",
                    $"Adds the {ConsoleAvailabilityPolicy.DisableDefine} scripting define for the active build target. " +
                    "The console then removes itself everywhere and is stripped from built scenes."),
                disabled);
            if (newValue != disabled)
                ConsoleDefines.SetDefine(target, newValue);

            EditorGUILayout.HelpBox(
                "Each CommandConsoleSettings asset also has an Availability (below). Consoles outside their availability " +
                "remove themselves at startup and are stripped from scenes when building. " +
                "Development builds keep consoles set to 'Development Builds'.",
                MessageType.Info);
        }

        private static void DrawSettingsAssets()
        {
            EditorGUILayout.LabelField("Console Settings Assets", EditorStyles.boldLabel);

            var assets = AssetDatabase.FindAssets("t:" + nameof(CommandConsoleSettings))
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<CommandConsoleSettings>)
                .Where(s => s != null)
                .ToArray();

            if (assets.Length == 0)
            {
                EditorGUILayout.HelpBox("No CommandConsoleSettings assets. Create one from Assets > Create > Eldritch Logger > Console Settings.", MessageType.None);
                return;
            }

            foreach (var settings in assets)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(settings, typeof(CommandConsoleSettings), false, GUILayout.MinWidth(160));

                EditorGUI.BeginChangeCheck();
                var availability = (ConsoleAvailability)EditorGUILayout.EnumPopup(settings.availability, GUILayout.Width(150));
                bool cheats = EditorGUILayout.ToggleLeft("Allow cheats", settings.allowCheats, GUILayout.Width(100));
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(settings, "Edit Console Release Safety");
                    settings.availability = availability;
                    settings.allowCheats = cheats;
                    EditorUtility.SetDirty(settings);
                }
                EditorGUILayout.EndHorizontal();

                if (settings.availability == ConsoleAvailability.Always)
                    EditorGUILayout.HelpBox($"{settings.name}: the console ships in release builds.", MessageType.Warning);
            }
        }
    }
}
