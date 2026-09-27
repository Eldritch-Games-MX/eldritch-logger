using System;
using System.IO;
using System.Linq;

namespace EldritchGames.EldritchLogger.Sinks.Files
{
    /// <summary>
    /// Decides where a file sink writes and removes files from older sessions.
    /// </summary>
    public sealed class LogFileLocator
    {
        public const string SessionStampFormat = "yyyyMMdd_HHmmss";

        private readonly string directory;
        private readonly string baseName;
        private readonly string extension;

        /// <param name="directory">Target directory. Empty means <c>Application.persistentDataPath</c>.</param>
        /// <param name="baseName">File name without extension.</param>
        /// <param name="extension">Extension including the dot (e.g. <c>.jsonl</c>).</param>
        public LogFileLocator(string directory, string baseName, string extension)
        {
            this.directory = string.IsNullOrEmpty(directory) ? UnityEngine.Application.persistentDataPath : directory;
            this.baseName = string.IsNullOrWhiteSpace(baseName) ? "eldritch_logs" : baseName;
            this.extension = extension ?? throw new ArgumentNullException(nameof(extension));
        }

        /// <summary>Path of the single, overwritten-each-session file.</summary>
        public string SingleFilePath => Path.Combine(directory, baseName + extension);

        /// <summary>Path of a file for the session that started at <paramref name="sessionStartUtc"/>.</summary>
        public string SessionFilePath(DateTime sessionStartUtc) =>
            Path.Combine(directory, $"{baseName}_{sessionStartUtc.ToLocalTime().ToString(SessionStampFormat)}{extension}");

        /// <summary>
        /// Deletes the oldest session files so that at most <paramref name="keep"/> remain.
        /// Call before creating the new session's file with <c>keep = maxSessions - 1</c>.
        /// </summary>
        public void DeleteOldSessions(int keep)
        {
            if (keep < 0 || !Directory.Exists(directory)) return;

            var stale = new DirectoryInfo(directory)
                .GetFiles($"{baseName}_*{extension}")
                .OrderByDescending(f => f.Name, StringComparer.Ordinal) // the stamp sorts chronologically
                .Skip(keep);

            foreach (var file in stale)
            {
                try
                {
                    file.Delete();
                }
                catch (IOException)
                {
                    // In use by another process: leave it for the next session.
                }
                catch (UnauthorizedAccessException)
                {
                }
            }
        }
    }
}
