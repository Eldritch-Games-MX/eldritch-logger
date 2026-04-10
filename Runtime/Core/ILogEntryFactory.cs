using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Domain;
using System;
using System.Collections.Generic;

public interface ILogEntryFactory
{
    LogEntry Create(LogLevel level, LogCategory category, string message,
                    Dictionary<string, object> metadata, Exception exception);
}