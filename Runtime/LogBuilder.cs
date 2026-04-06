using System;
using System.Collections.Generic;

namespace EldritchGames.EldritchLogger
{
    public sealed class LogBuilder
    {
        private readonly LogLevel level;
        private readonly LogCategory category;
        private readonly IReadOnlyDictionary<string, object>? metadata;

        internal LogBuilder(LogLevel level,
                            LogCategory category = default,
                            IReadOnlyDictionary<string, object>? metadata = null)
        {
            this.level = level;
            this.category = category;
            this.metadata = metadata;
        }

        public LogBuilder Category(LogCategory category) =>
            new(level, category, metadata);

        public LogBuilder AddKeyValue(string key, object value)
        {
            var newMetadata = metadata is null
                ? new Dictionary<string, object>()
                : new Dictionary<string, object>(metadata);

            newMetadata[key] = value;
            return new LogBuilder(level, category, newMetadata);
        }

        public void Log(string message)
        {
            EldritchLogger.Log(level, category, message,
                metadata as Dictionary<string, object>);
        }
    }
}