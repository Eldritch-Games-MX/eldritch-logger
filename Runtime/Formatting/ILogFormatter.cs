using EldritchGames.EldritchLogger.Dto;

namespace EldritchGames.EldritchLogger.Formatting
{
    /// <summary>
    /// Renders an entry as a single string. Formatters must be thread-safe.
    /// </summary>
    public interface ILogFormatter
    {
        string Format(LogEntryDto entry);
    }
}
