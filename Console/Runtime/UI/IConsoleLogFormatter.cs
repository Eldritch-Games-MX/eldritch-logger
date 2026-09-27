using EldritchGames.EldritchLogger.Console.Settings;

namespace EldritchGames.EldritchLogger.Console.Loader
{
    public interface IConsoleLogFormatter
    {
        string Format(CommandConsoleSettings logSettings, string logMessage);
    }
}