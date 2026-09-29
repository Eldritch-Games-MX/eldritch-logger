using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Dto;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace EldritchGames.EldritchLogger.Sinks.Files
{
    /// <summary>
    /// XML log file: a <c>&lt;Logs&gt;</c> root with one <c>&lt;LogEntryDto&gt;</c> element per entry.
    /// The closing tag is written on dispose.
    /// </summary>
    public sealed class XmlFileSink : FileLogSink
    {
        private static readonly XmlSerializer Serializer = new(typeof(LogEntryDto));
        private static readonly XmlWriterSettings WriterSettings = new() { OmitXmlDeclaration = true, Indent = true };
        private static readonly XmlSerializerNamespaces NoNamespaces = CreateNamespaces();

        public XmlFileSink(string path,
                           LogLevel minimumLevel = LogLevel.Debug,
                           int queueCapacity = BackgroundLogWriter.DefaultCapacity)
            : base(path, minimumLevel, queueCapacity) { }

        protected override string Header => "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<Logs>\n";

        protected override string Footer => "</Logs>\n";

        protected override string Serialize(LogEntryDto entry) => Format(entry) + "\n";

        /// <summary>One <c>&lt;LogEntryDto&gt;</c> element, as written to the file.</summary>
        public static string Format(LogEntryDto entry)
        {
            using var stringWriter = new StringWriter();
            using (var xmlWriter = XmlWriter.Create(stringWriter, WriterSettings))
                Serializer.Serialize(xmlWriter, entry, NoNamespaces);
            return stringWriter.ToString();
        }

        private static XmlSerializerNamespaces CreateNamespaces()
        {
            var ns = new XmlSerializerNamespaces();
            ns.Add("", "");
            return ns;
        }
    }
}
