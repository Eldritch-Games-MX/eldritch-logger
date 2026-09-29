using Newtonsoft.Json;

namespace EldritchGames.EldritchLogger.Dto
{
    /// <summary>
    /// JSON settings shared by everything that writes or reads <see cref="LogEntryDto"/> as JSON
    /// (JSON Lines files, HTTP payloads, the editor log viewer), so they always agree.
    /// </summary>
    public static class LogJson
    {
        /// <summary>Compact, null-free, UTC timestamps. Treat as read-only.</summary>
        public static readonly JsonSerializerSettings Settings = new()
        {
            Formatting = Newtonsoft.Json.Formatting.None, // qualified: the package has its own Formatting namespace
            NullValueHandling = NullValueHandling.Ignore,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc
        };

        public static string Serialize(object value) => JsonConvert.SerializeObject(value, Settings);

        public static LogEntryDto Deserialize(string json) => JsonConvert.DeserializeObject<LogEntryDto>(json, Settings);
    }
}
