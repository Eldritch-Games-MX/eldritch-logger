using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Exporting;
using System;
using System.Collections.Generic;
using System.Linq;

public class SinkManager : ISinkManager
{
    private readonly object syncRoot = new();
    private readonly IDictionary<SinkCategory, List<ILogSink>> sinks;

    public SinkManager(LogSinkFactory factory)
    {
        sinks = factory.CreateSinks();
    }

    public IEnumerable<ILogSink> GetAllSinks()
    {
        lock (syncRoot) return sinks.Values.SelectMany(list => list).ToList();
    }

    public IEnumerable<ILogSink> GetSinks(SinkCategory category)
    {
        lock (syncRoot) return sinks.ContainsKey(category) ? sinks[category] : Array.Empty<ILogSink>();
    }

    public void AddSink(SinkCategory category, ILogSink sink)
    {
        lock (syncRoot)
        {
            if (!sinks.ContainsKey(category))
                sinks[category] = new List<ILogSink>();
            sinks[category].Add(sink);
        }
    }

    public void RemoveSink(SinkCategory category, ILogSink sink)
    {
        lock (syncRoot)
        {
            if (sinks.ContainsKey(category))
                sinks[category].Remove(sink);
        }
    }
}
