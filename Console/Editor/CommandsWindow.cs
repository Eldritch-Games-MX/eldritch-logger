using EldritchGames.EldritchLogger.Console.Commands;
using EldritchGames.EldritchLogger.Console.Commands.Reflection;
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
    /// Lists console commands. In Play Mode: the live registry (usage, aliases, source, cheats), skipped types and
    /// name conflicts, a box to run commands, the recent command history, and a watch panel for console variables.
    /// In Edit Mode: every discovered command group and attributed command, with the constructor dependencies the
    /// console will not provide, and every [ConsoleMethod]/[ConsoleVariable] member with the reason it would be skipped.
    /// </summary>
    public sealed class CommandsWindow : EditorWindow
    {
        private Vector2 scroll;
        private string search = string.Empty;
        private string commandLine = string.Empty;
        private int selectedConsole;
        private bool showHistory = true;
        private bool showVariables = true;

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
            DrawHistory(console);
            DrawVariables(console);
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

        private void DrawHistory(ConsoleBootstrap console)
        {
            var history = console.Services?.Get<CommandHistory>();
            if (history == null || history.Count == 0) return;

            EditorGUILayout.Space();
            showHistory = EditorGUILayout.Foldout(showHistory, $"History ({history.Count})", true);
            if (!showHistory) return;

            foreach (var entry in history.GetLast(10).Reverse())
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.SelectableLabel(entry, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                    if (GUILayout.Button(new GUIContent("Edit", "Copy into the run box"), GUILayout.Width(40))) commandLine = entry;
                    if (GUILayout.Button("Run", GUILayout.Width(40))) console.Executor.Execute(entry);
                }
            }
        }

        /// <summary>Live values of [ConsoleVariable] members, edited through the executor so ranges, read-only and cheats apply.</summary>
        private void DrawVariables(ConsoleBootstrap console)
        {
            var variables = console.Registry.All.OfType<VariableCommand>()
                .Where(v => MatchesSearch(v.Descriptor.Name, v.Description))
                .OrderBy(v => v.Descriptor.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (variables.Count == 0) return;

            EditorGUILayout.Space();
            showVariables = EditorGUILayout.Foldout(showVariables, $"Console Variables ({variables.Count})", true);
            if (!showVariables) return;

            bool cheatsAllowed = console.Services?.Get<ICheatPolicy>()?.CheatsAllowed ?? false;
            foreach (var variable in variables)
            {
                object value;
                try
                {
                    value = variable.Value;
                }
                catch (Exception ex)
                {
                    EditorGUILayout.LabelField(variable.Descriptor.Name, $"<error: {ex.GetBaseException().Message}>");
                    continue;
                }

                bool locked = !variable.CanWrite || (variable.IsCheat && !cheatsAllowed);
                using (new EditorGUILayout.HorizontalScope())
                {
                    var label = new GUIContent(variable.Descriptor.Name, variable.Description);
                    using (new EditorGUI.DisabledScope(locked))
                        DrawVariableEditor(console, variable, label, value);

                    if (!variable.CanWrite) GUILayout.Label("read-only", EditorStyles.miniLabel, GUILayout.Width(70));
                    else if (variable.IsCheat) GUILayout.Label(cheatsAllowed ? "cheat" : "cheat (off)", EditorStyles.miniLabel, GUILayout.Width(70));
                }
            }
        }

        private static void DrawVariableEditor(ConsoleBootstrap console, VariableCommand variable, GUIContent label, object value)
        {
            var name = variable.Descriptor.Name;
            if (variable.ValueType == typeof(bool))
            {
                bool current = value is true;
                bool next = EditorGUILayout.Toggle(label, current);
                if (next != current) console.Executor.Execute($"{name} {(next ? "true" : "false")}");
            }
            else if (variable.ValueType.IsEnum && value is Enum current)
            {
                var next = EditorGUILayout.EnumPopup(label, current);
                if (!Equals(next, current)) console.Executor.Execute($"{name} {next}");
            }
            else
            {
                var text = ArgumentTypeResolver.Format(value);
                var edited = EditorGUILayout.DelayedTextField(label, text);
                if (edited != text) console.Executor.Execute($"{name} {Quote(edited)}");
            }
        }

        /// <summary>Quotes a value with spaces so it reaches the variable as one argument.</summary>
        private static string Quote(string value) =>
            value.IndexOf(' ') >= 0 ? "\"" + value.Replace("\"", "\\\"") + "\"" : value;

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
            DrawMembers();
        }

        private void DrawMembers()
        {
            var inspections = CommandDiscovery.CommandMembers.Select(CommandDiscovery.Inspect)
                .OrderBy(i => i.Command == null ? 0 : 1) // problems first
                .ThenBy(i => i.MemberDescription, StringComparer.Ordinal)
                .ToList();

            EditorGUILayout.Space();
            int skipped = inspections.Count(i => i.Command == null);
            EditorGUILayout.LabelField($"[ConsoleMethod] / [ConsoleVariable] Members ({inspections.Count}{(skipped > 0 ? $", {skipped} skipped" : "")})", EditorStyles.boldLabel);

            foreach (var inspection in inspections)
            {
                var usage = inspection.Command?.Descriptor.Usage;
                if (!MatchesSearch(inspection.MemberDescription, usage, inspection.SkipReason)) continue;

                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    var icon = EditorGUIUtility.IconContent(inspection.Command != null ? "TestPassed" : "console.warnicon.sml");
                    GUILayout.Label(icon, GUILayout.Width(18));

                    var kind = inspection.Command is VariableCommand ? "variable" : "method";
                    var text = inspection.Command != null
                        ? $"<b>{usage}</b>   <color=#9E9E9E>{kind} · {inspection.MemberDescription}</color>"
                        : $"<b>{inspection.MemberDescription}</b>\n{inspection.SkipReason}";
                    if (inspection.Command != null && CheatCommands.IsCheat(inspection.Command)) text += "   [cheat]";
                    GUILayout.Label(text, WrapStyle);

                    var script = inspection.DeclaringType != null ? FindScript(inspection.DeclaringType) : null;
                    using (new EditorGUI.DisabledScope(script == null))
                        if (GUILayout.Button("Open", GUILayout.Width(50)))
                            AssetDatabase.OpenAsset(script);
                }
            }
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
