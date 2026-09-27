using EldritchGames.EldritchLogger.Domain;
using EldritchGames.EldritchLogger.Dto;

namespace EldritchGames.EldritchLogger.Mapper
{
    /// <summary>
    /// Converts a pipeline <see cref="LogEntry"/> into the <see cref="LogEntryDto"/> sinks consume.
    /// </summary>
    public interface ILogEntryMapper
    {
        LogEntryDto ToDto(LogEntry entry);
    }
}
