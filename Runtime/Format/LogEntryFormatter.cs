using EldritchGames.EldritchLogger.Dto;
using EldritchGames.EldritchLogger.Settings;
using EldritchGames.EldritchLogger.Visuals;
using System;
using System.Linq;

namespace EldritchGames.EldritchLogger.Format
{
    public class LogEntryFormatter
    {
        private readonly LogSettings settings;

        public LogEntryFormatter(LogSettings settings)
        {
            this.settings = settings;
        }

        public string Format(LogEntryDto dto)
        {
            string format = settings != null ? settings.timestampFormat : null ?? "HH:mm:ss";
            string prefix = settings != null ? settings.messagePrefix : null ?? "";

            // Parse back to enum once, guaranteed to succeed because mapper used ToString()
            var categoryEnum = Enum.Parse<LogCategory>(dto.Category);

            string categoryColor = LogColors.GetColorString(categoryEnum, settings);
            string categoryText = $"<color={categoryColor}>{dto.Category}</color>";

            string baseMessage = $"[{dto.Timestamp.ToString(format)}] [{dto.Level}] {categoryText} {prefix}{dto.Message} {FormatMetadata(dto)}";

            if (!string.IsNullOrEmpty(dto.Exception))
            {
                baseMessage += $"\nException: {dto.Exception}";
            }

            return baseMessage;
        }


        private string FormatMetadata(LogEntryDto dto)
        {
            if (dto.Metadata == null || dto.Metadata.Count == 0)
                return "";

            return string.Join(" ", dto.Metadata.Select(kv =>
            {
                return kv.Key switch
                {
                    "ComponentContext" => $"[Component={kv.Value}]",
                    "CSharpEvent" => $"[C#Event={kv.Value}]",
                    "UnityEvent" => $"[UnityEvent={kv.Value}]",
                    "GameObject" => $"[GameObject={kv.Value}]",
                    _ => $"{kv.Key}={kv.Value}"
                };
            }));
        }
    }
}
