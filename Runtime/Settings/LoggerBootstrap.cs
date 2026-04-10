using System;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Settings
{
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

            // Initialize logger
            var logger = new Core.EldritchLogger(settings);

            // Ensure proper cleanup on application quit
            Application.quitting += () =>
            {
                logger.Dispose();
            };

            Debug.Log("EldritchLogger initialized with settings: " + settings.name);
        }
    }
}