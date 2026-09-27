using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Domain;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EldritchGames.EldritchLogger.Pipeline
{
    /// <summary>
    /// Adds properties to every entry (scene, build version, user id...).
    /// Runs on the logging thread: must be thread-safe and must not call main-thread-only Unity APIs.
    /// </summary>
    public interface ILogEnricher
    {
        /// <param name="entry">The entry being logged (read-only).</param>
        /// <param name="properties">The entry's properties; add or overwrite keys here.</param>
        void Enrich(LogEntry entry, IDictionary<string, object> properties);
    }

    /// <summary>Adds <see cref="LogPropertyKeys.BuildVersion"/> (<c>Application.version</c>).</summary>
    public sealed class BuildVersionEnricher : ILogEnricher
    {
        private readonly string version;

        /// <remarks>Construct on the main thread.</remarks>
        public BuildVersionEnricher() => version = Application.version;

        public void Enrich(LogEntry entry, IDictionary<string, object> properties)
        {
            if (!properties.ContainsKey(LogPropertyKeys.BuildVersion))
                properties[LogPropertyKeys.BuildVersion] = version;
        }
    }

    /// <summary>
    /// Adds <see cref="LogPropertyKeys.Scene"/>. The active scene name is cached on the main thread
    /// and refreshed on <see cref="SceneManager.activeSceneChanged"/>, so logging from worker threads is safe.
    /// </summary>
    public sealed class SceneEnricher : ILogEnricher, System.IDisposable
    {
        private volatile string sceneName;

        /// <remarks>Construct on the main thread.</remarks>
        public SceneEnricher()
        {
            sceneName = SceneManager.GetActiveScene().name;
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
        }

        private void OnActiveSceneChanged(Scene previous, Scene current) => sceneName = current.name;

        public void Enrich(LogEntry entry, IDictionary<string, object> properties)
        {
            if (!properties.ContainsKey(LogPropertyKeys.Scene))
                properties[LogPropertyKeys.Scene] = sceneName;
        }

        public void Dispose() => SceneManager.activeSceneChanged -= OnActiveSceneChanged;
    }
}
