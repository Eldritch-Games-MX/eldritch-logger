using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace EldritchGames.EldritchLogger.EditorTools.LogViewer
{
    /// <summary>A source location found in a stack trace line.</summary>
    public readonly struct StackFrameLink
    {
        /// <summary>The line of the stack trace this location was found in.</summary>
        public string Text { get; }

        /// <summary>The file as written in the trace (project-relative for Unity traces, absolute for .NET ones).</summary>
        public string FilePath { get; }

        public int Line { get; }

        public StackFrameLink(string text, string filePath, int line)
        {
            Text = text;
            FilePath = filePath;
            Line = line;
        }
    }

    /// <summary>Finds source locations in the stack traces the logger records, so the viewer can open them.</summary>
    public static class StackTraceLinks
    {
        // Unity:  "Player:Die () (at Assets/Scripts/Player.cs:42)"
        private static readonly Regex UnityFrame = new(@"\(at (?<file>[^()]+?\.cs):(?<line>\d+)\)", RegexOptions.Compiled);

        // .NET / Mono:  "at Player.Die () [0x00000] in C:\Game\Assets\Scripts\Player.cs:42"  or  "... in /path/Player.cs:line 42"
        private static readonly Regex DotNetFrame = new(@" in (?<file>.+?\.cs):(?:line )?(?<line>\d+)\s*$", RegexOptions.Compiled);

        /// <summary>One entry per line of <paramref name="stackTrace"/>; lines without a source location have a null path.</summary>
        public static List<StackFrameLink> Parse(string stackTrace)
        {
            var result = new List<StackFrameLink>();
            if (string.IsNullOrEmpty(stackTrace)) return result;

            foreach (var raw in stackTrace.Split('\n'))
            {
                var line = raw.TrimEnd('\r');
                if (line.Trim().Length == 0) continue;

                var match = UnityFrame.Match(line);
                if (!match.Success) match = DotNetFrame.Match(line);

                result.Add(match.Success && int.TryParse(match.Groups["line"].Value, out var number)
                    ? new StackFrameLink(line, match.Groups["file"].Value.Trim(), number)
                    : new StackFrameLink(line, null, 0));
            }
            return result;
        }

        /// <summary>
        /// The path relative to the project (<c>Assets/...</c> or <c>Packages/...</c>) when the file is inside it,
        /// otherwise the path unchanged. Null when there is no path.
        /// </summary>
        public static string ToProjectPath(string filePath, string projectRoot)
        {
            if (string.IsNullOrEmpty(filePath)) return null;
            var normalized = filePath.Replace('\\', '/');
            if (normalized.StartsWith("Assets/", StringComparison.Ordinal) || normalized.StartsWith("Packages/", StringComparison.Ordinal))
                return normalized;

            if (!string.IsNullOrEmpty(projectRoot))
            {
                var root = projectRoot.Replace('\\', '/').TrimEnd('/') + "/";
                if (normalized.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                    return normalized.Substring(root.Length);
            }

            // A path from another machine (a player build's log): match it by its Assets/ part.
            int assets = normalized.IndexOf("/Assets/", StringComparison.Ordinal);
            if (assets >= 0 && !File.Exists(filePath)) return normalized.Substring(assets + 1);
            return normalized;
        }
    }
}
