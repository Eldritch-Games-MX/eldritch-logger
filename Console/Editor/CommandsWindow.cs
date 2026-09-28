using EldritchGames.EldritchLogger.Console.Commands;
using EldritchGames.EldritchLogger.Console.Execution;
using EldritchGames.EldritchLogger.Console.Services;
using EldritchGames.EldritchLogger.Console.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Console.EditorTools
{
    /// <summary>
    /// Lists console commands. In Play Mode: the live registry (usage, aliases, source, cheats),
    /// skipped types and name conflicts, plus a box to run commands. In Edit Mode: every discovered
    /// command group and attributed command, with the constructor dependencies the console will not provide.
    /// </summary>
    public sealed class CommandsWindow : EditorWindow
    {
        private Vector2 scroll;
        private string search = string.Empty;
        private string commandLine = string.Empty;
        private int selectedConsole;

        private static GUIStyle wrapStyle;
        private static GUIStyle WrapStyle => wrapStyle ??= new GUIStyle(EditorStyles.label) { wordWrap = true, richText = true };

        [MenuItem("Tools/Eldritch Logger/Console Commands")]
        public static void Open() => GetWindow<CommandsWindow>("Console Commands");

        private void OnInspectorUpdate()
        {
            if (EditorApplication.isPlaying) Repaint();
        }

        private void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label(EditorApplication.isPlaying ? "Live registry" : "Discovered types (Edit Mode)", EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                search = GUILayout.TextField(search, EditorStyles.toolbarSearchField, GUILayout.Width(220));
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);
            if (EditorApplication.isPlaying) DrawLive();
            else DrawStatic();
            EditorGUILayout.EndScrollView();
        }

        // ---------------------------------------------------------------- Play Mode

        private void DrawLive()
        {
            var consoles = FindObjectsByType<ConsoleBootstrap>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(b => b.Registry != null)
                .ToArray();

            if (consoles.Length == 0)
            {
                EditorGUILayout.HelpBox("No running console in the loaded scenes (or it is unavailable here).", MessageType.Info);
                return;
            }

            if (consoles.Length > 1)
                selectedConsole = EditorGUILayout.Popup("Console", Mathf.Clamp(selectedConsole, 0, consoles.Length - 1),
                                                        consoles.Select(c => c.name).ToArray());
            var console = consoles[Mathf.Clamp(selectedConsole, 0, consoles.Length - 1)];

            DrawRunBox(console);
            DrawReport(console.Discovery);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Commands ({console.Registry.All.Count})", EditorStyles.boldLabel);
            foreach (var command in console.Registry.All.OrderBy(c => c.Descriptor.Name, StringComparer.OrdinalIgnoreCase))
            {
                var d = command.Descriptor;
                if (!MatchesSearch(d.Name, d.Description, string.Join(" ", d.Aliases))) continue;

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Label($"<b>{d.Usage}</b>", WrapStyle);
                        if (CheatCommands.IsCheat(command)) GUILayout.Label("cheat", EditorStyles.miniButton, GUILayout.Width(44));
                        if (GUILayout.Button("Run", GUILayout.Width(40)))
                            commandLine = d.Name + " ";
                    }
                    if (!string.IsNullOrEmpty(d.Description)) GUILayout.Label(d.Description, WrapStyle);

                    var details = new List<string>();
                    if (d.Aliases.Count > 0) details.Add("aliases: " + string.Join(", ", d.Aliases));
                    var source = console.Discovery?.SourceOf(command);
                    details.Add("from " + (source != null ? source.FullName : command.GetType().FullName));
                    GUILayout.Label(string.Join("   ·   ", details), EditorStyles.miniLabel);
                }
            }
        }

        private void DrawRunBox(ConsoleBootstrap console)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.SetNextControlName("CommandLine");
                commandLine = EditorGUILayout.TextField("Run", commandLine);
                bool submit = Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return
                              && GUI.GetNameOfFocusedControl() == "CommandLine";
                if ((GUILayout.Button("Execute", GUILayout.Width(70)) || submit) && !string.IsNullOrWhiteSpace(commandLine))
                {
                    console.Executor.Execute(commandLine);
                    commandLine = string.Empty;
                    GUI.FocusControl("CommandLine");
                    if (submit) Event.current.Use();
                }
            }
            EditorGUILayout.LabelField("Output appears in the in-game console.", EditorStyles.miniLabel);
        }

        private static void DrawReport(DiscoveryReport report)
        {
            if (report == null) return;

            if (report.SkippedTypes.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField($"Skipped ({report.SkippedTypes.Count})", EditorStyles.boldLabel);
                foreach (var skipped in report.SkippedTypes)
                    EditorGUILayout.HelpBox(skipped.Reason, MessageType.Warning);
            }

            if (report.Conflicts.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField($"Conflicts ({report.Conflicts.Count})", EditorStyles.boldLabel);
                foreach (var conflict in report.Conflicts)
                    EditorGUILayout.HelpBox(conflict, MessageType.Warning);
            }
        }

        // ---------------------------------------------------------------- Edit Mode

        private void DrawStatic()
        {
            EditorGUILayout.HelpBox(
                "Command names and conflicts are only known once commands are created. Enter Play Mode to see the live registry.\n" +
                "Below: discovered types and whether the console can create them.",
                MessageType.Info);

            var available = new HashSet<Type>(ConsoleBootstrap.DefaultServiceTypes);
            DrawTypes("Command Groups", CommandDiscovery.GroupTypes, available);
            DrawTypes("[ConsoleCommand] Commands", CommandDiscovery.CommandTypes, available);
        }

        private void DrawTypes(string title, IReadOnlyList<Type> types, HashSet<Type> available)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"{title} ({types.Count})", EditorStyles.boldLabel);

            foreach (var type in types.OrderBy(t => t.FullName))
            {
                if (!MatchesSearch(type.FullName)) continue;

                var missing = ConsoleServiceProvider.FindMissingDependencies(type, available);
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    var icon = EditorGUIUtility.IconContent(missing.Count == 0 ? "TestPassed" : "console.warnicon.sml");
                    GUILayout.Label(icon, GUILayout.Width(18));
                    var text = $"<b>{type.FullName}</b>";
                    if (missing.Count > 0)
                    {
                        var names = string.Join(", ", missing.Select(m => m == typeof(void) ? "a public constructor" : m.Name));
                        text += $"\nNeeds {names}: register it with ConsoleBootstrap.ConfiguringServices, or it will be skipped.";
                    }
                    GUILayout.Label(text, WrapStyle);

                    var script = FindScript(type);
                    using (new EditorGUI.DisabledScope(script == null))
                        if (GUILayout.Button("Open", GUILayout.Width(50)))
                            AssetDatabase.OpenAsset(script);
                }
            }
        }

        /// <summary>The script file named after the type (plain C# classes have no direct MonoScript mapping).</summary>
        private static MonoScript FindScript(Type type) =>
            AssetDatabase.FindAssets($"t:MonoScript {type.Name}")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<MonoScript>)
                .FirstOrDefault(s => s != null && s.name == type.Name);

        private bool MatchesSearch(params string[] fields) =>
            string.IsNullOrWhiteSpace(search) ||
            fields.Any(f => f != null && f.IndexOf(search.Trim(), StringComparison.OrdinalIgnoreCase) >= 0);
    }
}
