using EldritchGames.EldritchLogger.Console.Core;
using EldritchGames.EldritchLogger.Console.Parsing;
using EldritchGames.EldritchLogger.Console.Loader;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Console.Commands
{
    [ConsoleCommand]
    public class ThemeCommand : IConsoleCommand
    {
        private readonly IConsoleThemeApplier themeApplier;
        private readonly IThemeLoader themeLoader;
        public ThemeCommand(IConsoleThemeApplier applier, IThemeLoader loader)
        {
            themeApplier = applier;
            themeLoader = loader;
        }

        public string Name => "theme";
        public string Description => "Changes the console theme. Usage: theme <name>";
        public ArgSpec[] ExpectedArgs => new[]
        {
            new ArgSpec { Name = "name", Type = "string", Required = false, AllowMultiple = false }
        };

        public void Execute(string[] args)
        {
            if (args.Length == 0)
            {
                var themes = themeLoader.LoadAllThemes();
                if (themes.Length == 0)
                {
                    Debug.Log("No themes found in Resources/Themes.");
                    return;
                }

                Debug.Log("Available themes:");
                foreach (var t in themes)
                {
                    Debug.Log($"- {t.name}");
                }
                return;
            }

            string themeName = args[0];
            var theme = themeLoader.LoadTheme(themeName);

            if (theme != null)
            {
                themeApplier.ApplyTheme(theme);
                Debug.Log($"Theme '{themeName}' applied.");
            }
            else
            {
                Debug.LogWarning($"Theme '{themeName}' not found in Resources/Themes.");
            }
        }
    }
}
