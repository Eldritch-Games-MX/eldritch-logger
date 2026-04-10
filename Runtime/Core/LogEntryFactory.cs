using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Domain;
using System;
using System.Collections.Generic;

public class LogEntryFactory : ILogEntryFactory
{
    public LogEntry Create(LogLevel level, LogCategory category, string message,
                           Dictionary<string, object> metadata, Exception exception)
    {
        return new LogEntry(DateTime.Now, level, category, message, metadata, exception);
    }
}
