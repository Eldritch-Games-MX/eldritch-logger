using EldritchGames.EldritchLogger.Dto;
using EldritchGames.EldritchLogger.Exporting;
using System;
using System.IO;
using Unity.Plastic.Newtonsoft.Json;

namespace EldritchGames.EldritchLogger.Exporting
{
    public class JsonLogExporter : ILogExporter, IDisposable
    {
        private const string RootOpenBracket = "[";
        private const string RootCloseBracket = "]";
        private bool initialized = false;
        private bool firstEntry = true;
        private StreamWriter writer;

        public void Export(LogEntryDto dto, string path)
        {
            if (!initialized)
            {
                writer = new StreamWriter(path, append: false);
                writer.WriteLine(RootOpenBracket);
                initialized = true;
            }

            if (!firstEntry)
                writer.WriteLine(",");

            string json = JsonConvert.SerializeObject(dto, Formatting.Indented);
            writer.Write(json);

            firstEntry = false;
            writer.Flush();
        }

        public void Dispose()
        {
            if (initialized)
            {
                writer.WriteLine();
                writer.WriteLine(RootCloseBracket);
                writer.Dispose();
                initialized = false;
            }
        }
    }

}
