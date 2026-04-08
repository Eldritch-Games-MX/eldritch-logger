using EldritchGames.EldritchLogger.Dto;
using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace EldritchGames.EldritchLogger.Exporting
{
    public class XmlLogExporter : ILogExporter, IDisposable
    {
        private const string XmlDeclaration = "<?xml version=\"1.0\" encoding=\"utf-8\"?>";
        private const string RootOpenTag = "<Logs>";
        private const string RootCloseTag = "</Logs>";

        private bool initialized = false;
        private StreamWriter writer;
        private XmlSerializer serializer = new XmlSerializer(typeof(LogEntryDto));

        public void Export(LogEntryDto dto, string path)
        {
            if (!initialized)
            {
                writer = new StreamWriter(path, append: false);
                writer.WriteLine(XmlDeclaration);
                writer.WriteLine(RootOpenTag);
                initialized = true;
            }

            // Suppress declaration and namespaces for each entry
            var settings = new XmlWriterSettings
            {
                OmitXmlDeclaration = true,
                Indent = true
            };

            var ns = new XmlSerializerNamespaces();
            ns.Add("", ""); // remove xsi/xsd namespaces

            using var xmlWriter = XmlWriter.Create(writer, settings);
            serializer.Serialize(xmlWriter, dto, ns);

            writer.Flush();
        }

        public void Dispose()
        {
            if (initialized)
            {
                writer.WriteLine(RootCloseTag);
                writer.Dispose();
                initialized = false;
            }
        }
    }
}
