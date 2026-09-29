using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Dto;
using EldritchGames.EldritchLogger.Sinks.Files;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace EldritchGames.EldritchLogger.EditorTools.LogViewer
{
    /// <summary>
    /// Browses EldritchLogger entries: live from Play Mode, or from a <c>.jsonl</c> file (optionally followed as it grows).
    /// Filter by level, category, logger, origin, property values and text; group entries by message template;
    /// open stack trace locations; double-click an entry to select its context object.
    /// </summary>
    public sealed class LogViewerWindow : EditorWindow
    {
        private enum Source { Live, File }

        private static readonly LogLevel[] Levels = (LogLevel[])Enum.GetValues(typeof(LogLevel));
        private const double FollowInterval = 0.5;

        [SerializeField] private Source source = Source.Live;
        [SerializeField] private string filePath;
        [SerializeField] private bool autoScroll = true;
        [SerializeField] private bool grouped;
        [SerializeField] private bool follow;

        private readonly LogViewerModel model = new();
        private readonly List<LogViewerEntry> pending = new();
        private readonly Dictionary<LogLevel, ToolbarToggle> levelToggles = new();
        private List<LogGroup> groups = new();
        private long liveCursor;
        private int liveSession = -1;
        private JsonLinesTail tail;
        private long fileSequence;
        private double nextFollowPoll;

        private ListView list;
        private ScrollView details;
        private VisualElement filterBar;
        private Label status;
        private ToolbarMenu sourceMenu;
        private ToolbarMenu originMenu;
        private ToolbarMenu categoryMenu;
        private ToolbarMenu loggerMenu;
        private ToolbarButton refreshButton;
        private ToolbarToggle followToggle;
        private ToolbarToggle groupToggle;
        private ToolbarSearchField searchField;

        [MenuItem("Tools/Eldritch Logger/Log Viewer")]
        public static void Open() => GetWindow<LogViewerWindow>("Log Viewer");

        /// <summary>Opens the viewer on a JSON Lines file.</summary>
        public static void OpenFile(string path)
        {
            var window = GetWindow<LogViewerWindow>("Log Viewer");
            window.LoadFile(path);
        }

        private void OnEnable()
        {
            model.Changed += OnModelChanged;
            EditorApplication.update += Poll;
        }

        private void OnDisable()
        {
            model.Changed -= OnModelChanged;
            EditorApplication.update -= Poll;
            CloseTail();
        }

        private void CreateGUI()
        {
            rootVisualElement.Add(BuildToolbar());

            filterBar = new VisualElement { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap, paddingLeft = 4, paddingTop = 2, paddingBottom = 2 } };
            rootVisualElement.Add(filterBar);

            var split = new TwoPaneSplitView(1, 160, TwoPaneSplitViewOrientation.Vertical);
            list = new ListView
            {
                fixedItemHeight = 18,
                selectionType = SelectionType.Single,
                makeItem = () => new Label { enableRichText = true, style = { unityTextAlign = TextAnchor.MiddleLeft, paddingLeft = 4 } },
                bindItem = (element, index) => ((Label)element).text = grouped ? GroupRowText(groups[index]) : RowText(model.Visible[index].Entry)
            };
            list.selectionChanged += _ => ShowDetails();
            list.itemsChosen += items => OnItemChosen(items.FirstOrDefault());

            details = new ScrollView { style = { paddingLeft = 6, paddingRight = 6, paddingTop = 4 } };

            split.Add(list);
            split.Add(details);
            split.style.flexGrow = 1;
            rootVisualElement.Add(split);

            status = new Label { style = { paddingLeft = 4, paddingTop = 2, paddingBottom = 2 } };
            rootVisualElement.Add(status);

            if (source == Source.File && !string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                LoadFile(filePath);
            else
                SwitchToLive();
        }

        // ------------------------------------------------------------------ toolbar

        private Toolbar BuildToolbar()
        {
            var toolbar = new Toolbar();

            sourceMenu = new ToolbarMenu();
            sourceMenu.menu.AppendAction("Live (Play Mode)", _ => SwitchToLive(),
                _ => source == Source.Live ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
            sourceMenu.menu.AppendAction("Open File...", _ => BrowseFile());
            sourceMenu.menu.AppendAction("Open Log Folder", _ => RevealLogFolder());
            toolbar.Add(sourceMenu);

            toolbar.Add(new ToolbarButton(ClearEntries) { text = "Clear" });
            refreshButton = new ToolbarButton(() => LoadFile(filePath)) { text = "Reload" };
            toolbar.Add(refreshButton);
            followToggle = new ToolbarToggle { text = "Follow", value = follow, tooltip = "Keep reading the file as it grows (live tail)." };
            followToggle.RegisterValueChangedCallback(e =>
            {
                follow = e.newValue;
                if (source == Source.File) LoadFile(filePath);
            });
            toolbar.Add(followToggle);
            toolbar.Add(new ToolbarSpacer());

            foreach (var level in Levels)
            {
                var toggle = new ToolbarToggle { value = model.IsLevelVisible(level) };
                toggle.RegisterValueChangedCallback(e => model.SetLevelVisible(level, e.newValue));
                levelToggles[level] = toggle;
                toolbar.Add(toggle);
            }

            toolbar.Add(new ToolbarSpacer());
            categoryMenu = new ToolbarMenu { text = "Categories" };
            toolbar.Add(categoryMenu);
            loggerMenu = new ToolbarMenu { text = "Logger: All" };
            toolbar.Add(loggerMenu);
            originMenu = new ToolbarMenu { tooltip = "Entries logged by your code, captured from Unity's own log, or both." };
            foreach (LogSourceFilter option in Enum.GetValues(typeof(LogSourceFilter)))
            {
                var value = option;
                originMenu.menu.AppendAction(OriginLabel(value), _ => model.SetSourceFilter(value),
                    _ => model.SourceFilter == value ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
            }
            toolbar.Add(originMenu);

            groupToggle = new ToolbarToggle { text = "Group", value = grouped, tooltip = "Group entries by message template, most frequent first." };
            groupToggle.RegisterValueChangedCallback(e =>
            {
                grouped = e.newValue;
                OnModelChanged();
            });
            toolbar.Add(groupToggle);

            searchField = new ToolbarSearchField();
            searchField.style.flexGrow = 1;
            searchField.RegisterValueChangedCallback(e => model.SetSearch(e.newValue));
            toolbar.Add(searchField);

            var scrollToggle = new ToolbarToggle { text = "Auto-scroll", value = autoScroll };
            scrollToggle.RegisterValueChangedCallback(e => autoScroll = e.newValue);
            toolbar.Add(scrollToggle);

            var captureToggle = new ToolbarToggle { text = "Capture", value = LiveLogCapture.Enabled, tooltip = "Capture logger entries in Play Mode (applies from the next Play Mode session)." };
            captureToggle.RegisterValueChangedCallback(e => LiveLogCapture.Enabled = e.newValue);
            toolbar.Add(captureToggle);

            return toolbar;
        }

        private static string OriginLabel(LogSourceFilter filter) => filter switch
        {
            LogSourceFilter.Game => "Origin: Game",
            LogSourceFilter.Unity => "Origin: Unity",
            _ => "Origin: All"
        };

        // ------------------------------------------------------------------ sources

        private void SwitchToLive()
        {
            CloseTail();
            source = Source.Live;
            filePath = null;
            model.Clear();
            liveCursor = 0;
            liveSession = LiveLogCapture.Session;
            PollLive();
            UpdateChrome();
        }

        private void BrowseFile()
        {
            var start = string.IsNullOrEmpty(filePath) ? LogFileLocator.ResolveDirectory(null) : Path.GetDirectoryName(filePath);
            var path = EditorUtility.OpenFilePanel("Open JSON Lines log", start, "jsonl");
            if (!string.IsNullOrEmpty(path)) LoadFile(path);
        }

        private static void RevealLogFolder()
        {
            var directory = LogFileLocator.ResolveDirectory(null);
            Directory.CreateDirectory(directory);
            EditorUtility.RevealInFinder(directory);
        }

        private void LoadFile(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            CloseTail();

            List<LogEntryDto> entries;
            int invalid;
            try
            {
                tail = new JsonLinesTail(path);
                // When following, an incomplete last line is still being written: wait for it.
                entries = tail.ReadNew(out invalid, includePartialLastLine: !follow);
                if (!follow) CloseTail();
            }
            catch (IOException ex)
            {
                CloseTail();
                EditorUtility.DisplayDialog("Log Viewer", $"Could not read {path}:\n{ex.Message}", "OK");
                return;
            }

            source = Source.File;
            filePath = path;
            fileSequence = 0;
            model.Clear();
            model.AddRange(entries.Select(e => new LogViewerEntry(e, fileSequence++)));
            if (invalid > 0) Debug.LogWarning($"[EldritchLogger] {invalid} unreadable line(s) in {path}.");
            UpdateChrome();
        }

        private void CloseTail()
        {
            tail?.Dispose();
            tail = null;
        }

        private void ClearEntries()
        {
            model.Clear();
            if (source == Source.Live) liveCursor = LiveLogCapture.Sink.NextSequence;
        }

        private void Poll()
        {
            if (list == null) return;
            if (source == Source.Live) PollLive();
            else if (tail != null && EditorApplication.timeSinceStartup >= nextFollowPoll) PollFile();
        }

        private void PollLive()
        {
            if (source != Source.Live || list == null) return;

            if (liveSession != LiveLogCapture.Session)
            {
                liveSession = LiveLogCapture.Session;
                liveCursor = 0;
                model.Clear();
            }

            if (LiveLogCapture.Sink.NextSequence == liveCursor) return;

            pending.Clear();
            liveCursor = LiveLogCapture.Sink.CopySince(liveCursor, pending);
            if (pending.Count > 0) model.AddRange(pending);
        }

        private void PollFile()
        {
            nextFollowPoll = EditorApplication.timeSinceStartup + FollowInterval;
            try
            {
                var entries = tail.ReadNew(out _);
                if (tail.Restarted)
                {
                    fileSequence = 0;
                    model.Clear();
                }
                if (entries.Count > 0) model.AddRange(entries.Select(e => new LogViewerEntry(e, fileSequence++)));
            }
            catch (IOException)
            {
                // The file was deleted or replaced: stop following.
                CloseTail();
                follow = false;
                followToggle.SetValueWithoutNotify(false);
                UpdateChrome();
            }
        }

        // ------------------------------------------------------------------ list

        private void OnModelChanged()
        {
            if (list == null) return;

            if (grouped)
            {
                groups = model.BuildGroups();
                list.itemsSource = groups;
            }
            else
            {
                list.itemsSource = (System.Collections.IList)model.Visible;
            }
            list.RefreshItems();

            int count = list.itemsSource.Count;
            if (autoScroll && !grouped && count > 0) list.ScrollToItem(count - 1);
            UpdateChrome();
        }

        private void OnItemChosen(object item)
        {
            switch (item)
            {
                case LogGroup group:
                    // Drill into the group: show its entries.
                    model.SetGroupFilter(group.Key);
                    grouped = false;
                    groupToggle.SetValueWithoutNotify(false);
                    OnModelChanged();
                    break;
                case LogViewerEntry row:
                    SelectContext(row);
                    break;
            }
        }

        private void UpdateChrome()
        {
            if (status == null) return;

            sourceMenu.text = source == Source.Live ? "Source: Live" : $"Source: {Path.GetFileName(filePath)}";
            refreshButton.style.display = source == Source.File ? DisplayStyle.Flex : DisplayStyle.None;
            followToggle.style.display = source == Source.File ? DisplayStyle.Flex : DisplayStyle.None;
            originMenu.text = OriginLabel(model.SourceFilter);

            foreach (var level in Levels)
                levelToggles[level].text = $"{level} {model.CountOf(level)}";

            RebuildCategoryMenu();
            RebuildLoggerMenu();
            RebuildFilterBar();

            var origin = source == Source.Live
                ? (EditorApplication.isPlaying ? "live" : "live (enter Play Mode to capture)")
                : filePath + (tail != null ? " (following)" : string.Empty);
            var shown = grouped ? $"{groups.Count} groups from {model.Visible.Count}" : model.Visible.Count.ToString();
            status.text = $"Showing {shown} of {model.Entries.Count} · {origin}";
        }

        private void RebuildFilterBar()
        {
            filterBar.Clear();

            foreach (var filter in model.PropertyFilters)
            {
                var f = filter;
                filterBar.Add(Chip($"{f.Key} = {f.Value}", () => model.RemovePropertyFilter(f)));
            }
            if (model.GroupFilter != null)
                filterBar.Add(Chip($"Template: {Shorten(model.GroupFilter, 60)}", () => model.SetGroupFilter(null)));
            if (model.LoggerFilter != null)
                filterBar.Add(Chip($"Logger: {model.LoggerFilter}", () => model.SetLoggerFilter(null)));
            if (model.SourceFilter != LogSourceFilter.All)
                filterBar.Add(Chip(OriginLabel(model.SourceFilter), () => model.SetSourceFilter(LogSourceFilter.All)));

            if (filterBar.childCount > 1 || (filterBar.childCount == 1 && model.Search.Length > 0))
                filterBar.Add(new Button(() =>
                {
                    searchField.SetValueWithoutNotify(string.Empty);
                    model.ClearFilters();
                }) { text = "Clear filters" });

            filterBar.style.display = filterBar.childCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private static VisualElement Chip(string text, Action remove)
        {
            var chip = new Button(remove) { text = text + "  ✕", tooltip = "Remove this filter" };
            chip.style.borderTopLeftRadius = chip.style.borderTopRightRadius =
                chip.style.borderBottomLeftRadius = chip.style.borderBottomRightRadius = 8;
            return chip;
        }

        private void RebuildCategoryMenu()
        {
            var menu = categoryMenu.menu;
            menu.ClearItems();
            menu.AppendAction("All", _ => model.ShowAllCategories());
            menu.AppendAction("None", _ => model.HideAllCategories());
            menu.AppendSeparator();
            foreach (var category in model.Categories)
            {
                var name = category;
                menu.AppendAction(name, _ => model.SetCategoryVisible(name, !model.IsCategoryVisible(name)),
                    _ => model.IsCategoryVisible(name) ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
            }
        }

        private void RebuildLoggerMenu()
        {
            loggerMenu.text = "Logger: " + (model.LoggerFilter ?? "All");
            var menu = loggerMenu.menu;
            menu.ClearItems();
            menu.AppendAction("All", _ => model.SetLoggerFilter(null),
                _ => model.LoggerFilter == null ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
            foreach (var logger in model.Loggers)
            {
                var name = logger;
                menu.AppendAction(name, _ => model.SetLoggerFilter(name),
                    _ => model.LoggerFilter == name ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
            }
        }

        // ------------------------------------------------------------------ details

        private void ShowDetails()
        {
            details.Clear();
            switch (list.selectedItem)
            {
                case LogViewerEntry row:
                    ShowEntry(row.Entry);
                    break;
                case LogGroup group:
                    details.Add(Heading($"{group.Count} × {group.HighestLevel}"));
                    details.Add(Text($"First {group.First.Entry.Timestamp.ToLocalTime():HH:mm:ss.fff} · last {group.Last.Entry.Timestamp.ToLocalTime():HH:mm:ss.fff}"));
                    details.Add(Text(group.Key, selectable: true));
                    details.Add(new Button(() => OnItemChosen(group)) { text = "Show these entries", style = { alignSelf = Align.FlexStart, marginTop = 4 } });
                    details.Add(Heading("Latest"));
                    ShowEntry(group.Last.Entry);
                    break;
            }
        }

        private void ShowEntry(LogEntryDto e)
        {
            details.Add(Text($"{e.Timestamp.ToLocalTime():yyyy-MM-dd HH:mm:ss.fff}   {e.Level}   {e.Category}"));
            details.Add(Text(e.Message, selectable: true));

            if (e.Metadata != null && e.Metadata.Count > 0)
            {
                details.Add(Heading("Properties"));
                foreach (var m in e.Metadata)
                {
                    if (m.Key == LogPropertyKeys.StackTrace) continue;
                    var property = m;
                    var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
                    row.Add(new Label(property.Key) { style = { width = 160, unityFontStyleAndWeight = FontStyle.Bold } });
                    var value = Text(property.Value, selectable: true);
                    value.style.flexGrow = 1;
                    row.Add(value);
                    row.Add(new Button(() => model.AddPropertyFilter(property.Key, property.Value))
                    {
                        text = "Filter",
                        tooltip = $"Show only entries where {property.Key} = {property.Value}"
                    });
                    details.Add(row);
                }
            }

            var unityTrace = e.GetMetadata(LogPropertyKeys.StackTrace);
            if (!string.IsNullOrEmpty(e.Exception)) AddStackTrace("Exception", e.Exception);
            if (!string.IsNullOrEmpty(unityTrace)) AddStackTrace("Stack trace", unityTrace);

            if (e.Context != null)
            {
                var context = e.Context;
                details.Add(new Button(() => SelectContext(context)) { text = $"Select context: {context.name}", style = { alignSelf = Align.FlexStart, marginTop = 6 } });
            }
        }

        private void AddStackTrace(string title, string trace)
        {
            details.Add(Heading(title));
            foreach (var frame in StackTraceLinks.Parse(trace))
            {
                var label = new Label(frame.Text) { enableRichText = false };
                label.style.whiteSpace = WhiteSpace.Normal;
                if (frame.FilePath != null)
                {
                    var target = frame;
                    label.style.color = new Color(0.35f, 0.6f, 1f);
                    label.tooltip = $"Open {target.FilePath}:{target.Line}";
                    label.RegisterCallback<MouseDownEvent>(_ => OpenLocation(target));
                }
                details.Add(label);
            }
        }

        private static Label Heading(string text) =>
            new(text) { style = { unityFontStyleAndWeight = FontStyle.Bold, marginTop = 8, marginBottom = 2 } };

        private static VisualElement Text(string text, bool selectable = false)
        {
            if (!selectable) return new Label(text ?? string.Empty) { enableRichText = false, style = { whiteSpace = WhiteSpace.Normal } };
            var field = new TextField { value = text ?? string.Empty, isReadOnly = true, multiline = true };
            field.style.whiteSpace = WhiteSpace.Normal;
            return field;
        }

        private static void OpenLocation(StackFrameLink frame)
        {
            var projectRoot = Path.GetDirectoryName(Application.dataPath);
            var path = StackTraceLinks.ToProjectPath(frame.FilePath, projectRoot);
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
            if (script != null)
            {
                AssetDatabase.OpenAsset(script, frame.Line);
                return;
            }

            var absolute = Path.IsPathRooted(path) ? path : Path.Combine(projectRoot, path);
            if (File.Exists(absolute)) InternalEditorUtility.OpenFileAtLineExternal(absolute, frame.Line);
            else Debug.LogWarning($"[EldritchLogger] Source file not found: {frame.FilePath}");
        }

        private static void SelectContext(LogViewerEntry row) => SelectContext(row?.Entry.Context);

        private static void SelectContext(UnityEngine.Object context)
        {
            if (context == null) return;
            Selection.activeObject = context;
            EditorGUIUtility.PingObject(context);
        }

        // ------------------------------------------------------------------ rows

        private static string LevelText(LogLevel level)
        {
            string color = level switch
            {
                LogLevel.Debug => "#9E9E9E",
                LogLevel.Warning => "#FFC107",
                LogLevel.Error => "#FF5252",
                LogLevel.Critical => "#FF1744",
                _ => null
            };
            return color != null ? $"<color={color}>{level,-8}</color>" : $"{level,-8}";
        }

        private static string FirstLine(string text)
        {
            text ??= string.Empty;
            int newline = text.IndexOf('\n');
            return newline >= 0 ? text.Substring(0, newline) + " …" : text;
        }

        private static string Shorten(string text, int max) =>
            text.Length <= max ? text : text.Substring(0, max - 1) + "…";

        private static string RowText(LogEntryDto e)
        {
            var unity = e.GetMetadata(LogPropertyKeys.Source) == LogPropertyKeys.UnitySource ? "<color=#8FA4B8>[Unity]</color> " : string.Empty;
            return $"{e.Timestamp.ToLocalTime():HH:mm:ss.fff}  {LevelText(e.Level)} <b><noparse>{e.Category}</noparse></b>  {unity}<noparse>{FirstLine(e.Message)}</noparse>";
        }

        private static string GroupRowText(LogGroup g) =>
            $"<b>{g.Count,6}×</b>  {LevelText(g.HighestLevel)} <b><noparse>{g.Last.Entry.Category}</noparse></b>  <noparse>{FirstLine(g.Key)}</noparse>" +
            $"  <color=#9E9E9E>last {g.Last.Entry.Timestamp.ToLocalTime():HH:mm:ss}</color>";
    }
}
