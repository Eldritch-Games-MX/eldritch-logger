using EldritchGames.EldritchLogger.Core;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Settings
{
    /// <summary>
    /// Creates the logger from <c>Resources/LogSettings</c> before the first scene loads and
    /// registers it with <see cref="ELoggerFactory"/>. Disable <see cref="LogSettings.autoInitialize"/>
    /// to compose the logger yourself with <see cref="EldritchLoggerBuilder"/>.
    /// </summary>
    public static class LoggerBootstrap
    {
        public const string SettingsResourcePath = "LogSettings";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            var settings = Resources.Load<LogSettings>(SettingsResourcePath);
            if (settings == null)
            {
                Debug.LogError($"[EldritchLogger] LogSettings asset not found at Resources/{SettingsResourcePath}.");
                return;
            }

            if (!settings.autoInitialize) return;

            Install(EldritchLoggerBuilder.FromSettings(settings).Build());
        }

        /// <summary>
        /// Registers <paramref name="rootLogger"/> with <see cref="ELoggerFactory"/> and disposes it on quit.
        /// Use it when composing the logger manually.
        /// </summary>
        public static void Install(Core.EldritchLogger rootLogger)
        {
            Shutdown();
            SelfLog.Reset();

            installedFactory = new EldritchLoggerFactory(rootLogger);
            ELoggerFactory.SetFactory(installedFactory);
            Application.quitting += Shutdown;
        }

        private static EldritchLoggerFactory installedFactory;

        /// <summary>Unregisters and disposes the installed logger, if any. Runs automatically on quit.</summary>
        public static void Shutdown()
        {
            Application.quitting -= Shutdown;
            if (installedFactory == null) return;

            ELoggerFactory.ClearFactory();
            installedFactory.Dispose();
            installedFactory = null;
        }
    }
}
