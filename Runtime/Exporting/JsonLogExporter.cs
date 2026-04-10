using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Dto;
using EldritchGames.EldritchLogger.Exporting;
using System;
using System.IO;
using System.Threading.Tasks;

namespace EldritchGames.EldritchLogger.Exporting
{
    public class JsonLogExporter : IAsyncLogExporter, IDisposable
    {
        public string TargetPath { get; }
        public SinkCategory Category => SinkCategory.Persistent;
        private readonly ILogFileWriter fileWriter;
        private bool firstEntry = true;

        public JsonLogExporter(string path, ILogFileWriter fileWriter)
        {
            TargetPath = path ?? throw new ArgumentNullException(nameof(path));
            this.fileWriter = fileWriter ?? throw new ArgumentNullException(nameof(fileWriter));
            var dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }

        public void OnLogReceived(LogEntryDto entry) => Export(entry, TargetPath).GetAwaiter().GetResult();

        public async Task Export(LogEntryDto dto, string path)
        {
            await Task.Run(() =>
            {
                if (firstEntry)
                {
                    fileWriter.WriteLine(path, "[", append: false);
                    firstEntry = false;
                }
                else
                {
                    fileWriter.WriteLine(path, ",");
                }

                string json = Unity.Plastic.Newtonsoft.Json.JsonConvert.SerializeObject(dto, Unity.Plastic.Newtonsoft.Json.Formatting.Indented);
                fileWriter.WriteLine(path, json);
            });
        }

        public void Dispose()
        {
            fileWriter.WriteLine(TargetPath, Environment.NewLine + "]");
            if (fileWriter is IDisposable disposable)
                disposable.Dispose();
        }
    }
}
