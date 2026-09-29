using EldritchGames.EldritchLogger.Core;
using System;
using System.Collections.Generic;

namespace EldritchGames.EldritchLogger.Pipeline
{
    /// <summary><see cref="ILogControl"/> over a <see cref="SettingsLogFilter"/> and a flush action.</summary>
    internal sealed class SettingsLogControl : ILogControl
    {
        private readonly SettingsLogFilter filter;
        private readonly Action flush;

        public SettingsLogControl(SettingsLogFilter filter, Action flush)
        {
            this.filter = filter ?? throw new ArgumentNullException(nameof(filter));
            this.flush = flush ?? throw new ArgumentNullException(nameof(flush));
        }

        public LogLevel MinimumLevel => filter.MinimumLevel;

        public LogLevel? MinimumLevelOverride
        {
            get => filter.MinimumLevelOverride;
            set => filter.MinimumLevelOverride = value;
        }

        public IReadOnlyList<CategoryState> Categories => filter.Categories;

        public void SetCategoryOverride(LogCategory category, bool? enabled) => filter.SetCategoryOverride(category, enabled);

        public void ClearOverrides() => filter.ClearOverrides();

        public void Flush() => flush();
    }
}
