using System.Collections.Generic;

namespace EldritchGames.EldritchLogger.Core
{
    /// <summary>The effective state of one category at runtime.</summary>
    public readonly struct CategoryState
    {
        public LogCategory Category { get; }

        /// <summary>Whether entries in this category are currently logged.</summary>
        public bool Enabled { get; }

        /// <summary>True when <see cref="Enabled"/> comes from a runtime override rather than the settings asset.</summary>
        public bool Overridden { get; }

        public CategoryState(LogCategory category, bool enabled, bool overridden)
        {
            Category = category;
            Enabled = enabled;
            Overridden = overridden;
        }
    }

    /// <summary>
    /// Runtime control of a running logger (used by the console's <c>log.*</c> commands).
    /// Changes are in-memory overrides: the settings asset is never modified.
    /// </summary>
    public interface ILogControl
    {
        /// <summary>The minimum level currently applied (override or settings).</summary>
        LogLevel MinimumLevel { get; }

        /// <summary>Overrides the minimum level; null returns to the settings value.</summary>
        LogLevel? MinimumLevelOverride { get; set; }

        /// <summary>Every known category (from settings and overrides) with its effective state.</summary>
        IReadOnlyList<CategoryState> Categories { get; }

        /// <summary>Forces a category on or off; null returns it to the settings value.</summary>
        void SetCategoryOverride(LogCategory category, bool? enabled);

        /// <summary>Removes every runtime override.</summary>
        void ClearOverrides();

        /// <summary>Writes out everything buffered by the sinks.</summary>
        void Flush();
    }
}
