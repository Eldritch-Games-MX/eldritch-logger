using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Dto;
using System;
using System.Collections.Generic;

namespace EldritchGames.EldritchLogger.EditorTools.LogViewer
{
    /// <summary>One row of the log viewer.</summary>
    public sealed class LogViewerEntry
    {
        public LogEntryDto Entry { get; }
        public long Sequence { get; }

        /// <summary>Cached <see cref="LogPropertyKeys.Logger"/> value, or null.</summary>
        public string Logger { get; }

        public LogViewerEntry(LogEntryDto entry, long sequence)
        {
            Entry = entry ?? throw new ArgumentNullException(nameof(entry));
            Sequence = sequence;
            Logger = entry.GetMetadata(LogPropertyKeys.Logger);
        }
    }

    /// <summary>
    /// Entries plus filters for the log viewer window. Pure C# so it can be unit tested.
    /// </summary>
    public sealed class LogViewerModel
    {
        private readonly List<LogViewerEntry> entries = new();
        private readonly List<LogViewerEntry> visible = new();
        private readonly HashSet<LogLevel> hiddenLevels = new();
        private readonly HashSet<string> hiddenCategories = new(StringComparer.OrdinalIgnoreCase);
        private readonly SortedSet<string> categories = new(StringComparer.OrdinalIgnoreCase);
        private readonly SortedSet<string> loggers = new(StringComparer.Ordinal);
        private readonly int[] levelCounts = new int[Enum.GetValues(typeof(LogLevel)).Length];
        private string search = string.Empty;
        private string loggerFilter;

        /// <summary>Oldest entries are discarded beyond this count.</summary>
        public int Capacity { get; }

        public IReadOnlyList<LogViewerEntry> Entries => entries;
        public IReadOnlyList<LogViewerEntry> Visible => visible;
        public IReadOnlyCollection<string> Categories => categories;
        public IReadOnlyCollection<string> Loggers => loggers;
        public string Search => search;

        /// <summary>Only entries from this logger are shown, or all when null.</summary>
        public string LoggerFilter => loggerFilter;

        /// <summary>Raised after entries or filters change.</summary>
        public event Action Changed;

        public LogViewerModel(int capacity = 20_000)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            Capacity = capacity;
        }

        public int CountOf(LogLevel level) => levelCounts[(int)level];

        public bool IsLevelVisible(LogLevel level) => !hiddenLevels.Contains(level);

        public bool IsCategoryVisible(string category) => !hiddenCategories.Contains(category ?? string.Empty);

        public void Add(LogViewerEntry entry) => AddRange(new[] { entry });

        public void AddRange(IEnumerable<LogViewerEntry> newEntries)
        {
            bool trimmed = false;
            foreach (var entry in newEntries)
            {
                entries.Add(entry);
                Track(entry);
                if (Matches(entry)) visible.Add(entry);
            }

            // Trim in chunks so a full buffer does not shift the whole list on every entry.
            int excess = entries.Count - Capacity;
            if (excess > 0)
            {
                int remove = Math.Max(excess, Capacity / 10);
                remove = Math.Min(remove, entries.Count);
                for (int i = 0; i < remove; i++) levelCounts[(int)entries[i].Entry.Level]--;
                entries.RemoveRange(0, remove);
                trimmed = true;
            }

            if (trimmed) Refilter();
            else Changed?.Invoke();
        }

        public void Clear()
        {
            entries.Clear();
            visible.Clear();
            categories.Clear();
            loggers.Clear();
            Array.Clear(levelCounts, 0, levelCounts.Length);
            Changed?.Invoke();
        }

        public void SetLevelVisible(LogLevel level, bool isVisible)
        {
            if (isVisible ? hiddenLevels.Remove(level) : hiddenLevels.Add(level)) Refilter();
        }

        public void SetCategoryVisible(string category, bool isVisible)
        {
            if (isVisible ? hiddenCategories.Remove(category) : hiddenCategories.Add(category)) Refilter();
        }

        public void ShowAllCategories()
        {
            if (hiddenCategories.Count == 0) return;
            hiddenCategories.Clear();
            Refilter();
        }

        public void HideAllCategories()
        {
            hiddenCategories.UnionWith(categories);
            Refilter();
        }

        public void SetSearch(string text)
        {
            text = text?.Trim() ?? string.Empty;
            if (text == search) return;
            search = text;
            Refilter();
        }

        public void SetLoggerFilter(string logger)
        {
            if (logger == loggerFilter) return;
            loggerFilter = logger;
            Refilter();
        }

        public bool Matches(LogViewerEntry row)
        {
            var e = row.Entry;
            if (hiddenLevels.Contains(e.Level)) return false;
            if (hiddenCategories.Contains(e.Category ?? string.Empty)) return false;
            if (loggerFilter != null && row.Logger != loggerFilter) return false;
            if (search.Length == 0) return true;

            if (Contains(e.Message) || Contains(e.Category) || Contains(e.Exception)) return true;
            if (e.Metadata != null)
                foreach (var m in e.Metadata)
                    if (Contains(m.Key) || Contains(m.Value)) return true;
            return false;
        }

        private bool Contains(string text) =>
            text != null && text.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;

        private void Track(LogViewerEntry entry)
        {
            levelCounts[(int)entry.Entry.Level]++;
            if (!string.IsNullOrEmpty(entry.Entry.Category)) categories.Add(entry.Entry.Category);
            if (!string.IsNullOrEmpty(entry.Logger)) loggers.Add(entry.Logger);
        }

        private void Refilter()
        {
            visible.Clear();
            foreach (var entry in entries)
                if (Matches(entry)) visible.Add(entry);
            Changed?.Invoke();
        }
    }
}
