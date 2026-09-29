using EldritchGames.EldritchLogger.Console.Commands;
using EldritchGames.EldritchLogger.Console.Settings;
using System;
using System.Collections.Generic;

namespace EldritchGames.EldritchLogger.Console.Execution
{
    /// <summary>
    /// Decides whether cheat commands may run. Register your own implementation through
    /// <c>ConsoleBootstrap.ConfiguringServices</c> (e.g. only the multiplayer host, or only after a password).
    /// </summary>
    public interface ICheatPolicy
    {
        bool CheatsAllowed { get; }
    }

    /// <summary>Uses <see cref="CommandConsoleSettings.allowCheats"/>.</summary>
    public sealed class SettingsCheatPolicy : ICheatPolicy
    {
        private readonly CommandConsoleSettings settings;

        public SettingsCheatPolicy(CommandConsoleSettings settings) => this.settings = settings;

        public bool CheatsAllowed => settings == null || settings.allowCheats;
    }

    public sealed class FixedCheatPolicy : ICheatPolicy
    {
        public static readonly FixedCheatPolicy Allow = new(true);
        public static readonly FixedCheatPolicy Deny = new(false);

        public bool CheatsAllowed { get; }

        public FixedCheatPolicy(bool allowed) => CheatsAllowed = allowed;
    }

    /// <summary>Marks a command class as a cheat. Equivalent to <c>isCheat: true</c> on its descriptor.</summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class CheatAttribute : Attribute
    {
    }

    public static class CheatCommands
    {
        private static readonly Dictionary<Type, bool> AttributeCache = new();

        /// <summary>True when the command is flagged as a cheat by its descriptor or <see cref="CheatAttribute"/>.</summary>
        public static bool IsCheat(ICommand command)
        {
            if (command == null) return false;
            if (command.Descriptor.IsCheat) return true;

            var type = command.GetType();
            lock (AttributeCache)
            {
                if (!AttributeCache.TryGetValue(type, out var marked))
                    AttributeCache[type] = marked = type.IsDefined(typeof(CheatAttribute), false);
                return marked;
            }
        }
    }
}
