using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

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
        private readonly Regex sessionName;

        /// <param name="directory">Target directory. Empty means <c>Application.persistentDataPath</c>.</param>
        /// <param name="baseName">File name without extension.</param>
        /// <param name="extension">Extension including the dot (e.g. <c>.jsonl</c>).</param>
        public LogFileLocator(string directory, string baseName, string extension)
        {
            this.directory = ResolveDirectory(directory);
            this.baseName = string.IsNullOrWhiteSpace(baseName) ? "eldritch_logs" : baseName;
            this.extension = extension ?? throw new ArgumentNullException(nameof(extension));
            // "game_20260928_142501": exactly this base name plus a stamp, so "game_net_..." (another sink) never matches.
            sessionName = new Regex("^" + Regex.Escape(this.baseName) + @"_\d{8}_\d{6}$", RegexOptions.CultureInvariant);
        }

        /// <summary>The directory file sinks write to: <paramref name="directory"/>, or <c>Application.persistentDataPath</c> when empty.</summary>
        public static string ResolveDirectory(string directory) =>
            string.IsNullOrEmpty(directory) ? UnityEngine.Application.persistentDataPath : directory;

        /// <summary>Path of the single, overwritten-each-session file.</summary>
        public string SingleFilePath => Path.Combine(directory, baseName + extension);

        /// <summary>Path of a file for the session that started at <paramref name="sessionStartUtc"/>.</summary>
        public string SessionFilePath(DateTime sessionStartUtc) =>
            Path.Combine(directory, $"{baseName}_{sessionStartUtc.ToLocalTime().ToString(SessionStampFormat)}{extension}");

        /// <summary>
        /// Deletes the oldest session files so that at most <paramref name="keep"/> remain. Only files named
        /// exactly like <see cref="SessionFilePath"/> count, so sinks with overlapping names never touch each other's files.
        /// Call before creating the new session's file with <c>keep = maxSessions - 1</c>.
        /// </summary>
        public void DeleteOldSessions(int keep)
        {
            if (keep < 0 || !Directory.Exists(directory)) return;

            var staleFiles = FilesWithExtension(directory, $"{baseName}_*{extension}")
                .Where(f => sessionName.IsMatch(Path.GetFileNameWithoutExtension(f.Name)))
                .OrderByDescending(f => f.Name, StringComparer.Ordinal) // the stamp sorts chronologically
                .Skip(keep);

            foreach (var file in staleFiles)
                LogFiles.TryDelete(file.FullName, out _); // in use: left for the next session
        }

        /// <summary>
        /// Files matching <paramref name="pattern"/> whose extension is exactly this locator's
        /// (Windows patterns like <c>*.txt</c> also match <c>.txtx</c>).
        /// </summary>
        private IEnumerable<FileInfo> FilesWithExtension(string dir, string pattern) =>
            new DirectoryInfo(dir).GetFiles(pattern)
                .Where(f => string.Equals(f.Extension, extension, StringComparison.OrdinalIgnoreCase));
    }
}
