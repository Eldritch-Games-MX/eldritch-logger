using EldritchGames.EldritchLogger.Domain;
using EldritchGames.EldritchLogger.Dto;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace EldritchGames.EldritchLogger.Mapper
{
    public class LogEntryMapper : ILogEntryMapper
    {
        private const string LoggerNamespace = "EldritchGames.EldritchLogger";

        private static readonly Regex LambdaFrame = new("<.*?>b__\\d+(_\\d+)?", RegexOptions.Compiled);
        private static readonly Regex StateMachineFrame = new("<.*?>d__\\d+\\.MoveNext", RegexOptions.Compiled);
        private static readonly Regex LocalFunctionFrame = new("<.*?>g__.*?\\|\\d+(_\\d+)?", RegexOptions.Compiled);

        private readonly bool filterLoggerFrames;

        /// <param name="filterLoggerFrames">Remove the logger's own frames from exception stack traces.</param>
        public LogEntryMapper(bool filterLoggerFrames = true)
        {
            this.filterLoggerFrames = filterLoggerFrames;
        }

        public LogEntryDto ToDto(LogEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));

            var metadata = new List<MetadataEntry>(entry.Properties.Count);
            foreach (var kv in entry.Properties)
                metadata.Add(new MetadataEntry
                {
                    Key = kv.Key,
                    Value = FormatValue(kv.Value),
                    InMessage = Contains(entry.RenderedProperties, kv.Key)
                });

            return new LogEntryDto
            {
                Timestamp = entry.TimestampUtc,
                Level = entry.Level,
                Category = entry.Category.Name,
                Message = entry.Message,
                Metadata = metadata,
                Exception = entry.Exception != null ? DescribeException(entry.Exception) : null,
                Context = entry.Context
            };
        }

        /// <summary>
        /// Property values are written with the invariant culture, so files and remote sinks get
        /// <c>182.4</c> on every machine (not <c>182,4</c> on a German one) and match the rendered message.
        /// </summary>
        private static string FormatValue(object value) => Core.LogValues.Format(value);

        private static bool Contains(IReadOnlyList<string> keys, string key)
        {
            for (int i = 0; i < keys.Count; i++)
                if (keys[i] == key) return true;
            return false;
        }

        private string DescribeException(Exception exception)
        {
            var trace = exception.StackTrace ?? string.Empty;
            if (filterLoggerFrames)
                trace = RemoveLoggerFrames(trace);

            return Normalize($"{exception.GetType().Name}: {exception.Message}\n{trace}".TrimEnd());
        }

        private static string Normalize(string text)
        {
            text = LambdaFrame.Replace(text, "AnonymousHandler");
            text = StateMachineFrame.Replace(text, "AsyncStateMachine");
            text = LocalFunctionFrame.Replace(text, "LocalFunction");
            return text;
        }

        private static string RemoveLoggerFrames(string raw)
        {
            var sb = new StringBuilder(raw.Length);
            foreach (var line in raw.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (line.Contains(LoggerNamespace)) continue;
                if (sb.Length > 0) sb.Append('\n');
                sb.Append(line.Trim());
            }
            return sb.ToString();
        }
    }
}
