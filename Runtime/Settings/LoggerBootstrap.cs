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

        /// <summary>Raised after a logger is installed (editor tooling uses it to attach live sinks).</summary>
        public static event System.Action<ISinkRegistry> Installed;

        /// <summary>Raised before the installed logger is disposed.</summary>
        public static event System.Action Uninstalling;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            var settings = Resources.Load<LogSettings>(SettingsResourcePath);
            if (settings == null)
            {
                Debug.LogError($"[EldritchLogger] LogSettings asset not found at Resources/{SettingsResourcePath}. " +
                               "Create one from Edit > Project Settings > Eldritch Logger.");
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
            try { Installed?.Invoke(installedFactory); }
            catch (System.Exception ex) { SelfLog.Report("A LoggerBootstrap.Installed handler failed", ex); }
        }

        private static EldritchLoggerFactory installedFactory;

        /// <summary>Unregisters and disposes the installed logger, if any. Runs automatically on quit.</summary>
        public static void Shutdown()
        {
            Application.quitting -= Shutdown;
            if (installedFactory == null) return;

            try { Uninstalling?.Invoke(); }
            catch (System.Exception ex) { SelfLog.Report("A LoggerBootstrap.Uninstalling handler failed", ex); }

            ELoggerFactory.ClearFactory();
            installedFactory.Dispose();
            installedFactory = null;
        }
    }
}
