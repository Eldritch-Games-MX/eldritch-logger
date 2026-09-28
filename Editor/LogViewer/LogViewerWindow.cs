using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Dto;
using EldritchGames.EldritchLogger.Sinks.Files;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace EldritchGames.EldritchLogger.EditorTools.LogViewer
{
    /// <summary>
    /// Browses EldritchLogger entries: live from Play Mode, or from a <c>.jsonl</c> file.
    /// Filter by level, category, logger and text; double-click to select the entry's context object.
    /// </summary>
    public sealed class LogViewerWindow : EditorWindow
    {
        private enum Source { Live, File }

        private static readonly LogLevel[] Levels = (LogLevel[])Enum.GetValues(typeof(LogLevel));

        [SerializeField] private Source source = Source.Live;
        [SerializeField] private string filePath;
        [SerializeField] private bool autoScroll = true;

        private readonly LogViewerModel model = new();
        private readonly List<LogViewerEntry> pending = new();
        private readonly Dictionary<LogLevel, ToolbarToggle> levelToggles = new();
        private long liveCursor;
        private int liveSession = -1;

        private ListView list;
        private TextField details;
        private Label status;
        private ToolbarMenu sourceMenu;
        private ToolbarMenu categoryMenu;
        private ToolbarMenu loggerMenu;
        private ToolbarButton refreshButton;

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
            EditorApplication.update += PollLive;
        }

        private void OnDisable()
        {
            model.Changed -= OnModelChanged;
            EditorApplication.update -= PollLive;
        }

        private void CreateGUI()
        {
            rootVisualElement.Add(BuildToolbar());

            var split = new TwoPaneSplitView(1, 110, TwoPaneSplitViewOrientation.Vertical);
            list = new ListView
            {
                fixedItemHeight = 18,
                selectionType = SelectionType.Single,
                itemsSource = (System.Collections.IList)model.Visible,
                makeItem = () => new Label { enableRichText = true, style = { unityTextAlign = TextAnchor.MiddleLeft, paddingLeft = 4 } },
                bindItem = (element, index) => ((Label)element).text = RowText(model.Visible[index].Entry)
            };
            list.selectionChanged += _ => ShowDetails();
            list.itemsChosen += items => SelectContext(items.OfType<LogViewerEntry>().FirstOrDefault());

            details = new TextField { multiline = true, isReadOnly = true };
            details.style.flexGrow = 1;
            details.style.whiteSpace = WhiteSpace.Normal;
            var detailsScroll = new ScrollView();
            detailsScroll.Add(details);

            split.Add(list);
            split.Add(detailsScroll);
            split.style.flexGrow = 1;
            rootVisualElement.Add(split);

            status = new Label { style = { paddingLeft = 4, paddingTop = 2, paddingBottom = 2 } };
            rootVisualElement.Add(status);

            if (source == Source.File && !string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                LoadFile(filePath);
            else
                SwitchToLive();
        }

        private Toolbar BuildToolbar()
        {
            var toolbar = new Toolbar();

            sourceMenu = new ToolbarMenu();
            sourceMenu.menu.AppendAction("Live (Play Mode)", _ => SwitchToLive(),
                _ => source == Source.Live ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
            sourceMenu.menu.AppendAction("Open File...", _ => BrowseFile());
            sourceMenu.menu.AppendAction("Open Log Folder", _ => RevealLogFolder());
            toolbar.Add(sourceMenu);

            toolbar.Add(new ToolbarButton(ClearOrReload) { text = "Clear" });
            refreshButton = new ToolbarButton(() => LoadFile(filePath)) { text = "Reload" };
            toolbar.Add(refreshButton);
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

            var searchField = new ToolbarSearchField();
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

        private void SwitchToLive()
        {
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
            List<LogEntryDto> entries;
            int invalid;
            try
            {
                entries = JsonLinesLogReader.Read(path, out invalid);
            }
            catch (IOException ex)
            {
                EditorUtility.DisplayDialog("Log Viewer", $"Could not read {path}:\n{ex.Message}", "OK");
                return;
            }

            source = Source.File;
            filePath = path;
            model.Clear();
            model.AddRange(entries.Select((e, i) => new LogViewerEntry(e, i)));
            if (invalid > 0) Debug.LogWarning($"[EldritchLogger] {invalid} unreadable line(s) in {path}.");
            UpdateChrome();
        }

        private void ClearOrReload()
        {
            if (source == Source.Live)
            {
                model.Clear();
                liveCursor = LiveLogCapture.Sink.NextSequence;
            }
            else
            {
                model.Clear();
            }
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

        private void OnModelChanged()
        {
            if (list == null) return;

            list.itemsSource = (System.Collections.IList)model.Visible;
            list.RefreshItems();
            if (autoScroll && model.Visible.Count > 0)
                list.ScrollToItem(model.Visible.Count - 1);
            UpdateChrome();
        }

        private void UpdateChrome()
        {
            if (status == null) return;

            sourceMenu.text = source == Source.Live ? "Source: Live" : $"Source: {Path.GetFileName(filePath)}";
            refreshButton.style.display = source == Source.File ? DisplayStyle.Flex : DisplayStyle.None;

            foreach (var level in Levels)
                levelToggles[level].text = $"{level} {model.CountOf(level)}";

            RebuildCategoryMenu();
            RebuildLoggerMenu();

            var origin = source == Source.Live
                ? (EditorApplication.isPlaying ? "live" : "live (enter Play Mode to capture)")
                : filePath;
            status.text = $"Showing {model.Visible.Count} of {model.Entries.Count} · {origin}";
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

        private void ShowDetails()
        {
            if (list.selectedItem is not LogViewerEntry row)
            {
                details.value = string.Empty;
                return;
            }

            var e = row.Entry;
            var sb = new StringBuilder();
            sb.Append(e.Timestamp.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss.fff")).Append("  ")
              .Append(e.Level).Append("  ").AppendLine(e.Category);
            sb.AppendLine();
            sb.AppendLine(e.Message);
            if (e.Metadata != null && e.Metadata.Count > 0)
            {
                sb.AppendLine();
                foreach (var m in e.Metadata) sb.Append(m.Key).Append(" = ").AppendLine(m.Value);
            }
            if (!string.IsNullOrEmpty(e.Exception))
            {
                sb.AppendLine();
                sb.AppendLine(e.Exception);
            }
            if (e.Context != null)
            {
                sb.AppendLine();
                sb.Append("Context: ").Append(e.Context.name).AppendLine(" (double-click the row to select it)");
            }
            details.value = sb.ToString();
        }

        private static void SelectContext(LogViewerEntry row)
        {
            var context = row?.Entry.Context;
            if (context == null) return;
            Selection.activeObject = context;
            EditorGUIUtility.PingObject(context);
        }

        private static string RowText(LogEntryDto e)
        {
            string color = e.Level switch
            {
                LogLevel.Debug => "#9E9E9E",
                LogLevel.Warning => "#FFC107",
                LogLevel.Error => "#FF5252",
                LogLevel.Critical => "#FF1744",
                _ => null
            };
            var level = color != null ? $"<color={color}>{e.Level,-8}</color>" : $"{e.Level,-8}";
            var message = e.Message ?? string.Empty;
            int newline = message.IndexOf('\n');
            if (newline >= 0) message = message.Substring(0, newline) + " …";
            return $"{e.Timestamp.ToLocalTime():HH:mm:ss.fff}  {level} <b><noparse>{e.Category}</noparse></b>  <noparse>{message}</noparse>";
        }
    }
}
