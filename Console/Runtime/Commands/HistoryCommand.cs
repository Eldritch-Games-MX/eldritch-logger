using EldritchGames.EldritchLogger.Console.Core;
using EldritchGames.EldritchLogger.Console.Parsing;
using System.Collections.Generic;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Console.Commands
{
    [ConsoleCommand]
    public class HistoryCommand : IAdvancedConsoleCommand
    {
        private readonly CommandHistory history;

        public HistoryCommand(CommandHistory history)
        {
            this.history = history;
        }

        public string Name => "history";
        public string Description => "Displays previously entered commands. Use --limit N to show only the last N entries.";

        public ArgSpec[] ExpectedArgs => new[]
        {
            new ArgSpec { Name = "limit", Type = "int", Required = false, MustBePositive = true }
        };

        public void Execute(List<string> args, Dictionary<string, string> flags)
        {
            int limit = history.Count;

            if (flags.TryGetValue("limit", out var limitValue) && int.TryParse(limitValue, out var parsed))
            {
                limit = parsed;
            }

            int i = 1;
            foreach (var entry in history.GetLast(limit))
            {
                Debug.Log($"{i++}: {entry}");
            }
        }
    }
}