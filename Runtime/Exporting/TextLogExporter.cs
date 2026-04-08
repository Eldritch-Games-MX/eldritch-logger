using EldritchGames.EldritchLogger.Dto;
using EldritchGames.EldritchLogger.Format;
using EldritchGames.EldritchLogger.Mapper;
using EldritchGames.EldritchLogger.Settings;
using System;
using System.IO;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Exporting
{
    public class TextLogExporter : ILogExporter
    {
        private readonly LogSettings settings;

        public TextLogExporter(LogSettings settings)
        {
            this.settings = settings;
        }

        public void Export(LogEntryDto dto, string path)
        {
            var formatter = new LogEntryFormatter(settings);
            string formatted = formatter.Format(dto);
            File.AppendAllText(path, formatted + Environment.NewLine);
        }
    }

}
