using System;
using System.IO;

namespace EldritchGames.EldritchLogger.Sinks.Files
{
    /// <summary>File helpers used by the file sinks.</summary>
    internal static class LogFiles
    {
        /// <summary>Deletes <paramref name="path"/>; I/O and permission errors are returned instead of thrown.</summary>
        public static bool TryDelete(string path, out Exception error)
        {
            try
            {
                File.Delete(path);
                error = null;
                return true;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                error = ex; // e.g. in use by another process: left for a later cleanup
                return false;
            }
        }
    }
}
