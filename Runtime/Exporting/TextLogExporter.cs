using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Dto;
using EldritchGames.EldritchLogger.Exporting;
using EldritchGames.EldritchLogger.Format;
using EldritchGames.EldritchLogger.Settings;
using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Exporting
{
    public class TextLogExporter : IAsyncLogExporter, IDisposable
    {
        private readonly LogEntryFormatter formatter;
        private readonly ILogFileWriter fileWriter;

        public string TargetPath { get; }
        public SinkCategory Category => SinkCategory.Persistent;

        public TextLogExporter(LogSettings settings, ILogFileWriter fileWriter)
        {
            this.formatter = new LogEntryFormatter(settings ?? throw new ArgumentNullException(nameof(settings)));
            this.fileWriter = fileWriter ?? throw new ArgumentNullException(nameof(fileWriter));

            string directory = string.IsNullOrEmpty(settings.exportDirectory)
                ? Application.persistentDataPath
                : settings.exportDirectory;

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            TargetPath = Path.Combine(directory, settings.exportFileName + ".txt");
        }

        public void OnLogReceived(LogEntryDto entry) => Export(entry, TargetPath).GetAwaiter().GetResult();

        public async Task Export(LogEntryDto dto, string path)
        {
            string line = formatter.Format(dto);
            await Task.Run(() => fileWriter.WriteLine(path, line));
        }

        public void Dispose()
        {
            if (fileWriter is IDisposable disposable)
                disposable.Dispose();
        }
    }
}
