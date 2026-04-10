using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Dto;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace EldritchGames.EldritchLogger.Exporting
{
    public class XmlLogExporter : IAsyncLogExporter, IDisposable
    {
        private readonly ILogFileWriter fileWriter;
        private readonly XmlSerializer serializer = new(typeof(LogEntryDto));
        private bool initialized;

        public string TargetPath { get; }
        public SinkCategory Category => SinkCategory.Persistent;

        public XmlLogExporter(string path, ILogFileWriter fileWriter)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Export path must be provided.", nameof(path));
            this.fileWriter = fileWriter ?? throw new ArgumentNullException(nameof(fileWriter));

            var dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            TargetPath = path;
        }

        public void OnLogReceived(LogEntryDto entry) =>
            Export(entry, TargetPath).GetAwaiter().GetResult();

        public async Task Export(LogEntryDto dto, string path)
        {
            await Task.Run(() =>
            {
                if (!initialized)
                {
                    fileWriter.WriteLine(path, "<?xml version=\"1.0\" encoding=\"utf-8\"?>", append: false);
                    fileWriter.WriteLine(path, "<Logs>");
                    initialized = true;
                }

                var settings = new XmlWriterSettings { OmitXmlDeclaration = true, Indent = true };
                var ns = new XmlSerializerNamespaces();
                ns.Add("", "");

                using var stringWriter = new StringWriter();
                using var xmlWriter = XmlWriter.Create(stringWriter, settings);
                serializer.Serialize(xmlWriter, dto, ns);
                fileWriter.WriteLine(path, stringWriter.ToString());
            });
        }

        public void Dispose()
        {
            fileWriter.WriteLine(TargetPath, "</Logs>");
            if (fileWriter is IDisposable disposable)
                disposable.Dispose();
        }
    }
}
