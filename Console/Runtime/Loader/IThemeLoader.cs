using EldritchGames.EldritchLogger.Console.Settings;

namespace EldritchGames.EldritchLogger.Console.Loader
{
    public interface IThemeLoader
    {
        ConsoleTheme LoadTheme(string themeName);
        ConsoleTheme[] LoadAllThemes();
    }
}