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

        /// <summary>What entries are grouped by: the message template, or the message when there is none.</summary>
        public string GroupKey { get; }

        /// <summary>True for entries captured from Unity's own log.</summary>
        public bool IsFromUnity { get; }

        public LogViewerEntry(LogEntryDto entry, long sequence)
        {
            Entry = entry ?? throw new ArgumentNullException(nameof(entry));
            Sequence = sequence;
            Logger = entry.GetMetadata(LogPropertyKeys.Logger);
            GroupKey = entry.GetMetadata(LogPropertyKeys.MessageTemplate) ?? entry.Message ?? string.Empty;
            IsFromUnity = entry.GetMetadata(LogPropertyKeys.Source) == LogPropertyKeys.UnitySource;
        }
    }

    /// <summary>Which entries to show by origin.</summary>
    public enum LogSourceFilter
    {
        All,
        /// <summary>Entries logged through the logger API.</summary>
        Game,
        /// <summary>Entries captured from Unity's own log (engine errors, uncaught exceptions, Debug.Log).</summary>
        Unity
    }

    /// <summary>A required property value: only entries whose property <see cref="Key"/> equals <see cref="Value"/> are shown.</summary>
    public readonly struct PropertyFilter : IEquatable<PropertyFilter>
    {
        public string Key { get; }
        public string Value { get; }

        public PropertyFilter(string key, string value)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            Value = value;
        }

        public bool Equals(PropertyFilter other) => Key == other.Key && Value == other.Value;
        public override bool Equals(object obj) => obj is PropertyFilter other && Equals(other);
        public override int GetHashCode() => (Key.GetHashCode() * 397) ^ (Value?.GetHashCode() ?? 0);
        public override string ToString() => $"{Key} = {Value}";
    }

    /// <summary>Visible entries sharing a message template (or, for plain messages, the same text).</summary>
    public sealed class LogGroup
    {
        public string Key { get; }
        public int Count { get; internal set; }
        public LogViewerEntry First { get; internal set; }
        public LogViewerEntry Last { get; internal set; }

        /// <summary>The most severe level in the group.</summary>
        public LogLevel HighestLevel { get; internal set; }

        public LogGroup(string key, LogViewerEntry first)
        {
            Key = key;
            First = Last = first;
            HighestLevel = first.Entry.Level;
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
        private readonly List<PropertyFilter> propertyFilters = new();
        private readonly int[] levelCounts = new int[Enum.GetValues(typeof(LogLevel)).Length];
        private string search = string.Empty;
        private string loggerFilter;
        private string groupFilter;
        private LogSourceFilter sourceFilter;

        /// <summary>Oldest entries are discarded beyond this count.</summary>
        public int Capacity { get; }

        public IReadOnlyList<LogViewerEntry> Entries => entries;
        public IReadOnlyList<LogViewerEntry> Visible => visible;
        public IReadOnlyCollection<string> Categories => categories;
        public IReadOnlyCollection<string> Loggers => loggers;
        public string Search => search;

        /// <summary>Only entries from this logger are shown, or all when null.</summary>
        public string LoggerFilter => loggerFilter;

        /// <summary>Required property values (all must match).</summary>
        public IReadOnlyList<PropertyFilter> PropertyFilters => propertyFilters;

        /// <summary>Only entries with this <see cref="LogViewerEntry.GroupKey"/> are shown, or all when null.</summary>
        public string GroupFilter => groupFilter;

        public LogSourceFilter SourceFilter => sourceFilter;

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

        /// <summary>Shows only entries whose property <paramref name="key"/> equals <paramref name="value"/>.</summary>
        public void AddPropertyFilter(string key, string value)
        {
            var filter = new PropertyFilter(key, value);
            if (propertyFilters.Contains(filter)) return;
            propertyFilters.Add(filter);
            Refilter();
        }

        public void RemovePropertyFilter(PropertyFilter filter)
        {
            if (propertyFilters.Remove(filter)) Refilter();
        }

        /// <summary>Shows only entries of one group (see <see cref="BuildGroups"/>), or all when null.</summary>
        public void SetGroupFilter(string groupKey)
        {
            if (groupKey == groupFilter) return;
            groupFilter = groupKey;
            Refilter();
        }

        public void SetSourceFilter(LogSourceFilter filter)
        {
            if (filter == sourceFilter) return;
            sourceFilter = filter;
            Refilter();
        }

        /// <summary>Removes the property, group, logger and source filters and the search text.</summary>
        public void ClearFilters()
        {
            propertyFilters.Clear();
            groupFilter = null;
            loggerFilter = null;
            sourceFilter = LogSourceFilter.All;
            search = string.Empty;
            Refilter();
        }

        /// <summary>
        /// Groups the visible entries by template (or message), most frequent first. Entries logged with
        /// <c>"Lost {Packets} packets"</c> form one group however the values differ.
        /// </summary>
        public List<LogGroup> BuildGroups()
        {
            var byKey = new Dictionary<string, LogGroup>(StringComparer.Ordinal);
            var groups = new List<LogGroup>();
            foreach (var row in visible)
            {
                if (!byKey.TryGetValue(row.GroupKey, out var group))
                {
                    group = new LogGroup(row.GroupKey, row);
                    byKey.Add(row.GroupKey, group);
                    groups.Add(group);
                }
                group.Count++;
                group.Last = row;
                if (row.Entry.Level > group.HighestLevel) group.HighestLevel = row.Entry.Level;
            }

            // Stable: equal counts keep first-seen order.
            var order = new Dictionary<LogGroup, int>();
            for (int i = 0; i < groups.Count; i++) order[groups[i]] = i;
            groups.Sort((a, b) => a.Count != b.Count ? b.Count.CompareTo(a.Count) : order[a].CompareTo(order[b]));
            return groups;
        }

        public bool Matches(LogViewerEntry row)
        {
            var e = row.Entry;
            if (hiddenLevels.Contains(e.Level)) return false;
            if (hiddenCategories.Contains(e.Category ?? string.Empty)) return false;
            if (loggerFilter != null && row.Logger != loggerFilter) return false;
            if (groupFilter != null && row.GroupKey != groupFilter) return false;
            if (sourceFilter == LogSourceFilter.Game && row.IsFromUnity) return false;
            if (sourceFilter == LogSourceFilter.Unity && !row.IsFromUnity) return false;
            foreach (var filter in propertyFilters)
                if (!HasProperty(e, filter)) return false;
            if (search.Length == 0) return true;

            if (Contains(e.Message) || Contains(e.Category) || Contains(e.Exception)) return true;
            if (e.Metadata != null)
                foreach (var m in e.Metadata)
                    if (Contains(m.Key) || Contains(m.Value)) return true;
            return false;
        }

        private static bool HasProperty(LogEntryDto entry, PropertyFilter filter)
        {
            if (entry.Metadata == null) return false;
            foreach (var m in entry.Metadata)
                if (m.Key == filter.Key && m.Value == filter.Value) return true;
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
