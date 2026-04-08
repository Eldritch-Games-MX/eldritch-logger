using EldritchGames.EldritchLogger;
using EldritchGames.EldritchLogger.Exporting;
using EldritchGames.EldritchLogger.Settings;
using System;
using UnityEngine;

public static class LoggerBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        var settings = Resources.Load<LogSettings>("LogSettings");
        if (settings == null)
        {
            Debug.LogError("LogSettings asset not found in Resources!");
            return;
        }

        new EldritchLogger(settings);

        Application.quitting += () =>
        {
            if (EldritchLogger.Instance != null)
            {
                foreach (var exporter in EldritchLogger.Instance.Exporters)
                {
                    if (exporter is IDisposable disposable)
                        disposable.Dispose();
                }
            }
        };

        Debug.Log("EldritchLogger initialized with settings: " + settings.name);
    }
}
