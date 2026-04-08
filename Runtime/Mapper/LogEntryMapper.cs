using EldritchGames.EldritchLogger.Dto;
using EldritchGames.EldritchLogger.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace EldritchGames.EldritchLogger.Mapper
{
    public class LogEntryMapper : ILogEntryMapper
    {
        private readonly LogSettings settings;

        public LogEntryMapper(LogSettings settings)
        {
            this.settings = settings;
        }

        public LogEntryDto ToDto(LogEntry entry)
        {
            string exceptionMessage = null;

            if (entry.Exception != null)
            {
                var trace = entry.Exception.StackTrace ?? string.Empty;

                if (settings.filterLoggerFrames)
                {
                    trace = CleanStackTrace(trace);
                }

                exceptionMessage = $"{entry.Exception.GetType().Name}: {entry.Exception.Message}\n{trace}";
                exceptionMessage = Normalize(exceptionMessage);
            }

            return new LogEntryDto
            {
                Timestamp = entry.Timestamp,
                Level = entry.Level.ToString(),
                Category = entry.Category.ToString(),
                Message = entry.Message,
                Metadata = entry.Metadata?.Select(kv => new MetadataEntry
                {
                    Key = kv.Key,
                    Value = kv.Value?.ToString()
                }).ToList() ?? new List<MetadataEntry>(),
                Exception = exceptionMessage
            };
        }

        private string Normalize(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            // Lambdas / anonymous handlers
            text = Regex.Replace(text, "<.*?>b__\\d+(_\\d+)?", "AnonymousHandler");

            // Async state machines
            text = Regex.Replace(text, "<.*?>d__\\d+\\.MoveNext", "AsyncStateMachine");

            // Iterator blocks
            text = Regex.Replace(text, "<.*?>d__\\d+\\.MoveNext", "IteratorBlock");

            // Local functions
            text = Regex.Replace(text, "<.*?>g__.*?\\|\\d+(_\\d+)?", "LocalFunction");

            return text;
        }

        private string CleanStackTrace(string raw)
        {
            var lines = raw.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var filtered = lines
                .Where(line => !line.Contains("EldritchGames.EldritchLogger"))
                .Select(line => line.Trim());

            return string.Join("\n", filtered);
        }
    }
}
