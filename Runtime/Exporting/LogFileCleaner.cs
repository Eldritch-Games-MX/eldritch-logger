using EldritchGames.EldritchLogger.Settings;
using System;
using System.IO;

namespace EldritchGames.EldritchLogger.Exporting
{
    public class LogFileCleaner
    {
        private readonly LogSettings settings;
        private readonly LogPathResolver resolver;

        public LogFileCleaner(LogSettings settings)
        {
            this.settings = settings != null ? settings : throw new ArgumentNullException(nameof(settings));
            this.resolver = new LogPathResolver(settings);
        }

        public void DeletePreviousFiles()
        {
            foreach (var fmt in settings.exportFormats)
            {
                var path = resolver.ResolvePath(fmt);
                if (File.Exists(path))
                    File.Delete(path);
            }
        }
    }
}
