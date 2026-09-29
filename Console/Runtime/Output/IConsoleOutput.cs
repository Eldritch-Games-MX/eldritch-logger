using EldritchGames.EldritchLogger.Console.UI;
using EldritchGames.EldritchLogger.Core;
using System;

namespace EldritchGames.EldritchLogger.Console.Output
{
    public enum ConsoleMessageType
    {
        Info,
        Warning,
        Error
    }

    /// <summary>
    /// Output channel for console commands. Independent from Unity logging, so command output
    /// is shown even when Unity logs are hidden and does not pollute game logs.
    /// </summary>
    public interface IConsoleOutput
    {
        void Write(string message, ConsoleMessageType type = ConsoleMessageType.Info);

        void Clear();
    }

    public static class ConsoleOutputExtensions
    {
        public static void Info(this IConsoleOutput output, string message) => output.Write(message, ConsoleMessageType.Info);
        public static void Warn(this IConsoleOutput output, string message) => output.Write(message, ConsoleMessageType.Warning);
        public static void Error(this IConsoleOutput output, string message) => output.Write(message, ConsoleMessageType.Error);
    }

    /// <summary>
    /// Writes to the console view, optionally mirroring each message to an EldritchLogger.
    /// </summary>
    public sealed class ConsoleOutput : IConsoleOutput
    {
        /// <summary>Property set on mirrored entries so the console sink does not display them twice.</summary>
        public const string MirroredPropertyKey = "ConsoleCommandOutput";

        private static readonly LogCategory Category = new("Console");

        private readonly IConsoleOutputView view;
        private readonly IEldritchLogger mirror;

        /// <param name="view">Where messages are displayed.</param>
        /// <param name="mirror">Optional logger that also receives every message (e.g. to keep it in log files).</param>
        public ConsoleOutput(IConsoleOutputView view, IEldritchLogger mirror = null)
        {
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            this.mirror = mirror;
        }

        public void Write(string message, ConsoleMessageType type = ConsoleMessageType.Info)
        {
            view.AppendLog(type switch
            {
                ConsoleMessageType.Warning => $"<color=#FFC107>{message}</color>",
                ConsoleMessageType.Error => $"<color=#FF5252>{message}</color>",
                _ => message
            });

            if (mirror == null) return;

            var level = type switch
            {
                ConsoleMessageType.Warning => LogLevel.Warning,
                ConsoleMessageType.Error => LogLevel.Error,
                _ => LogLevel.Info
            };
            mirror.At(level, Category).AddKeyValue(MirroredPropertyKey, true).Log(message);
        }

        public void Clear() => view.Clear();
    }
}
