using EldritchGames.EldritchLogger.Console.Core;
using EldritchGames.EldritchLogger.Console.Parsing;
using EldritchGames.EldritchLogger.Console.Loader;
using System;

namespace EldritchGames.EldritchLogger.Console.Commands
{
    [ConsoleCommand]
    public class ClearCommand : IConsoleCommand
    {
        private readonly IConsoleView view;

        public ClearCommand(IConsoleView view)
        {
            this.view = view;
        }

        public string Name => "clear";
        public string Description => "Clears the console output.";
        public ArgSpec[] ExpectedArgs => Array.Empty<ArgSpec>();
        public void Execute(string[] args)
        {
            view.Clear();
        }
    }
}
