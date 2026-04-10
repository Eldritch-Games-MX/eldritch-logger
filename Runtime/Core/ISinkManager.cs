using EldritchGames.EldritchLogger.Core;
using System.Collections.Generic;

public interface ISinkManager
{
    IEnumerable<ILogSink> GetAllSinks();
    IEnumerable<ILogSink> GetSinks(SinkCategory category);
    void AddSink(SinkCategory category, ILogSink sink);
    void RemoveSink(SinkCategory category, ILogSink sink);
}