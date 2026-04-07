using EldritchGames.EldritchLogger;
using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class LogBuilder
{
    private readonly LogLevel level;
    private readonly LogCategory category;
    private readonly IReadOnlyDictionary<string, object> metadata;
    private readonly Exception exception;
    private readonly object evt; // can be Delegate or UnityEventBase
    private readonly GameObject gameObject;

    internal LogBuilder(LogLevel level,
                        LogCategory category = default,
                        IReadOnlyDictionary<string, object> metadata = null,
                        Exception exception = null,
                        object evt = null,
                        GameObject gameObject = null)
    {
        this.level = level;
        this.category = category;
        this.metadata = metadata;
        this.exception = exception;
        this.evt = evt;
        this.gameObject = gameObject;
    }

    public LogBuilder Category(LogCategory category) =>
        new LogBuilder(level, category, metadata, exception, evt, gameObject);

    public LogBuilder AddKeyValue(string key, object value)
    {
        var newMetadata = metadata == null
            ? new Dictionary<string, object>()
            : new Dictionary<string, object>(metadata);

        newMetadata[key] = value;
        return new LogBuilder(level, category, newMetadata, exception, evt, gameObject);
    }

    public LogBuilder WithException(Exception ex) =>
        new LogBuilder(level, category, metadata, ex, evt, gameObject);

    public LogBuilder WithEvent(object eventObj) =>
        new LogBuilder(level, category, metadata, exception, eventObj, gameObject);

    public LogBuilder WithComponent(Component component)
    {
        var newMetadata = metadata == null
            ? new Dictionary<string, object>()
            : new Dictionary<string, object>(metadata);

        newMetadata["ComponentContext"] = $"{component.GetType().Name}@{component.gameObject.name}";
        return new LogBuilder(level, category, newMetadata, exception, evt, component.gameObject);
    }

    public void Log(string message)
    {
        var dict = metadata == null ? new Dictionary<string, object>() : new Dictionary<string, object>(metadata);

        if (evt is Delegate del)
            dict["CSharpEvent"] = del.Method.Name;
        else if (evt is UnityEngine.Events.UnityEventBase unityEvt)
            dict["UnityEvent"] = unityEvt.GetType().Name;

        if (gameObject != null && !dict.ContainsKey("GameObject"))
            dict["GameObject"] = gameObject.name;

        EldritchLogger.Log(level, category, message, dict, exception);
    }

}