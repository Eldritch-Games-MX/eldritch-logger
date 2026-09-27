using System.Linq;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Console.Themes
{
    /// <summary>
    /// A UI element that restyles itself from a <see cref="ConsoleTheme"/>.
    /// Add a component implementing this to any element under the console to have it themed;
    /// <see cref="ConsoleThemeApplier"/> needs no changes.
    /// </summary>
    public interface IThemeable
    {
        void ApplyTheme(ConsoleTheme theme);
    }

    public interface IThemeLoader
    {
        ConsoleTheme LoadTheme(string name);
        ConsoleTheme[] LoadAllThemes();
    }

    public interface IConsoleThemeApplier
    {
        ConsoleTheme CurrentTheme { get; }

        /// <summary>Re-applies the current theme.</summary>
        void ApplyTheme();

        void ApplyTheme(ConsoleTheme theme);
    }

    /// <summary>Loads themes from a <c>Resources</c> folder.</summary>
    public sealed class ResourcesThemeLoader : IThemeLoader
    {
        private readonly string folder;

        public ResourcesThemeLoader(string folder = "Themes")
        {
            this.folder = string.IsNullOrWhiteSpace(folder) ? "Themes" : folder.TrimEnd('/');
        }

        public ConsoleTheme LoadTheme(string name) =>
            string.IsNullOrWhiteSpace(name)
                ? null
                : Resources.Load<ConsoleTheme>($"{folder}/{name}")
                  ?? LoadAllThemes().FirstOrDefault(t => string.Equals(t.name, name, System.StringComparison.OrdinalIgnoreCase));

        public ConsoleTheme[] LoadAllThemes() => Resources.LoadAll<ConsoleTheme>(folder);
    }
}
