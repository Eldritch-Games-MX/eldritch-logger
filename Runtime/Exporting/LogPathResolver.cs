using System.IO;
using UnityEngine;
using EldritchGames.EldritchLogger.Settings;

namespace EldritchGames.EldritchLogger.Exporting
{
    public class LogPathResolver
    {
        private readonly LogSettings settings;

        public LogPathResolver(LogSettings settings)
        {
            this.settings = settings ?? throw new System.ArgumentNullException(nameof(settings));
        }

        public string ResolvePath(ExportFormat fmt)
        {
            string dir = string.IsNullOrEmpty(settings.exportDirectory)
                ? Application.persistentDataPath
                : settings.exportDirectory;

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            string ext = fmt switch
            {
                ExportFormat.Json => ".json",
                ExportFormat.Xml => ".xml",
                ExportFormat.Text => ".txt",
                _ => ".log"
            };

            return Path.Combine(dir, settings.exportFileName + ext);
        }
    }
}
