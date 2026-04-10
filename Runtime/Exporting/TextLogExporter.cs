using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Dto;
using EldritchGames.EldritchLogger.Format;
using EldritchGames.EldritchLogger.Settings;
using System;
using System.Threading.Tasks;

namespace EldritchGames.EldritchLogger.Exporting
{
    public class TextLogExporter : IAsyncLogExporter, IDisposable
    {
        private readonly LogEntryFormatter formatter;
        private readonly ILogFileWriter fileWriter;

        public string TargetPath { get; }
        public SinkCategory Category => SinkCategory.Persistent;

        public TextLogExporter(string path, LogSettings settings, ILogFileWriter fileWriter)
        {
            formatter = new LogEntryFormatter(settings != null ? settings : throw new ArgumentNullException(nameof(settings)));
            this.fileWriter = fileWriter ?? throw new ArgumentNullException(nameof(fileWriter));
            TargetPath = path ?? throw new ArgumentNullException(nameof(path));
        }

        public void OnLogReceived(LogEntryDto entry) =>
            Export(entry, TargetPath).GetAwaiter().GetResult();

        public async Task Export(LogEntryDto dto, string path)
        {
            string line = formatter.Format(dto);
            await Task.Run(() => fileWriter.WriteLine(path, line, append: true));
        }

        public void Dispose()
        {
            if (fileWriter is IDisposable disposable)
                disposable.Dispose();
        }
    }
}
