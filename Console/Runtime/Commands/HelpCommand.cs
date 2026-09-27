using EldritchGames.EldritchLogger.Console.Core;
using EldritchGames.EldritchLogger.Console.Parsing;
using EldritchGames.EldritchLogger.Console.Registry;
using System;
using System.Linq;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Console.Commands
{
    [ConsoleCommand]
    public class HelpCommand : IConsoleCommand
    {
        private readonly ICommandRegistry registry;

        public HelpCommand(ICommandRegistry registry)
        {
            this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        public string Name => "help";
        public string Description => "Lists all available commands, or usage for a specific command.";
        public ArgSpec[] ExpectedArgs => new[]
        {
            new ArgSpec { Name = "command", Type = "string", Required = false }
        };

        public void Execute(string[] args)
        {
            if (args.Length > 0)
            {
                var target = args[0];
                var cmd = registry.GetAllMetadata()
                    .FirstOrDefault(c => c.Name.Equals(target, StringComparison.OrdinalIgnoreCase));

                if (cmd != null)
                {
                    Debug.Log($"{cmd.Name} {FormatUsage(cmd.ExpectedArgs)} - {cmd.Description}");
                }
                else
                {
                    Debug.LogWarning($"Unknown command '{target}'.");
                }
                return;
            }

            Debug.Log("Available commands:");
            foreach (var cmd in registry.GetAllMetadata())
            {
                Debug.Log($"{cmd.Name} {FormatUsage(cmd.ExpectedArgs)} - {cmd.Description}");
            }
        }

        private string FormatUsage(ArgSpec[] specs)
        {
            if (specs == null || specs.Length == 0)
                return string.Empty;

            return string.Join(" ", specs.Select(s =>
            {
                if (s.Type == "flag")
                {
                    return s.Required ? s.Name : $"[{s.Name}]";
                }

                var typeSuffix = s.Type;
                if (s.MustBePositive) typeSuffix += "+";

                var token = s.AllowMultiple
                    ? $"<{s.Name}:{typeSuffix}...>"
                    : $"<{s.Name}:{typeSuffix}>";

                return s.Required ? token : $"[{token}]";
            }));
        }
    }
}
