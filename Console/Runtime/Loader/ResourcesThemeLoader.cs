using EldritchGames.EldritchLogger.Console.Settings;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Console.Loader
{
    public class ResourcesThemeLoader : IThemeLoader
    {
        public ConsoleTheme LoadTheme(string name) =>
            Resources.Load<ConsoleTheme>($"Themes/{name}");

        public ConsoleTheme[] LoadAllThemes() =>
            Resources.LoadAll<ConsoleTheme>("Themes");
    }
}