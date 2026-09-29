using EldritchGames.EldritchLogger.Console.Arguments;
using EldritchGames.EldritchLogger.Console.Output;
using System;

namespace EldritchGames.EldritchLogger.Console.Commands.BuiltIn
{
    public sealed class HistoryCommand : IConsoleCommand
    {
        private readonly CommandHistory history;
        private readonly int defaultLimit;

        public CommandDescriptor Descriptor { get; } = new(
            "history",
            "Shows previously entered commands.",
            flags: new[] { FlagSpec.WithValue("limit", ArgumentTypes.PositiveInt, description: "Show only the last N entries.") });

        /// <param name="defaultLimit">Entries shown when <c>--limit</c> is not given.</param>
        public HistoryCommand(CommandHistory history, int defaultLimit = 20)
        {
            this.history = history ?? throw new ArgumentNullException(nameof(history));
            this.defaultLimit = Math.Max(1, defaultLimit);
        }

        public void Execute(CommandContext context)
        {
            int limit = context.Arguments.GetFlag("limit", defaultLimit);

            if (history.Count == 0)
            {
                context.Output.Info("History is empty.");
                return;
            }

            int index = Math.Max(0, history.Count - limit) + 1;
            foreach (var entry in history.GetLast(limit))
                context.Output.Info($"{index++}: {entry}");
        }
    }
}
