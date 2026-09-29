using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Dto;
using EldritchGames.EldritchLogger.Pipeline;
using EldritchGames.EldritchLogger.Settings;
using EldritchGames.EldritchLogger.Sinks;
using System;

namespace EldritchGames.EldritchLogger.EditorTools
{
    /// <summary>The sample entry shown by the inspector previews, built by a real logger so it looks like live output.</summary>
    public static class SinkPreview
    {
        private sealed class CaptureSink : ILogSink
        {
            public LogEntryDto Last;
            public string Name => "Preview";
            public LogLevel MinimumLevel => LogLevel.Debug;
            public void Emit(LogEntryDto entry) => Last = entry;
        }

        /// <summary>
        /// A <see cref="LogCategory.Gameplay"/> entry at the settings' minimum level, logged with a message template
        /// (<c>Name</c>, <c>Damage</c>), a scope property (<c>MatchId</c>), a logger name, a GameObject and an exception.
        /// </summary>
        public static LogEntryDto CreateSample(LogSettings settings)
        {
            var capture = new CaptureSink();
            using var logger = new EldritchLoggerBuilder().ClearEnrichers().AddSink(capture).Build();
            var level = settings != null ? settings.minimumLevel : LogLevel.Info;

            using (LogScope.Push("MatchId", 42))
            {
                logger.At(level, LogCategory.Gameplay)
                    .AddKeyValue(LogPropertyKeys.Logger, "PlayerController")
                    .AddKeyValue(LogPropertyKeys.GameObject, "PlayerPawn")
                    .WithException(new InvalidOperationException("Preview exception message"))
                    .Log("Player {Name} took {Damage:0.0} damage", "Bob", 12.5f);
            }
            return capture.Last;
        }
    }
}
