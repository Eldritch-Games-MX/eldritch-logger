using EldritchGames.EldritchLogger.Console.Arguments;
using EldritchGames.EldritchLogger.Console.Execution;
using EldritchGames.EldritchLogger.Console.Output;
using EldritchGames.EldritchLogger.Console.Registry;
using System;
using System.Linq;

namespace EldritchGames.EldritchLogger.Console.Commands.BuiltIn
{
    public sealed class HelpCommand : IConsoleCommand
    {
        private readonly ICommandRegistry registry;

        public CommandDescriptor Descriptor { get; }

        public HelpCommand(ICommandRegistry registry)
        {
            this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
            Descriptor = new CommandDescriptor(
                "help",
                "Lists all commands, or shows usage for one command.",
                parameters: new[]
                {
                    ParameterSpec.Optional("command",
                        ArgumentTypes.Choice("command", () => registry.All.Select(c => c.Descriptor.Name)))
                },
                aliases: new[] { "?" });
        }

        public void Execute(CommandContext context)
        {
            var name = context.Arguments.GetOrDefault<string>("command");
            if (name != null)
            {
                if (registry.TryGet(name, out var command))
                    Describe(context.Output, command, detailed: true);
                else
                    context.Output.Warn($"Unknown command '{name}'.");
                return;
            }

            context.Output.Info("Available commands:");
            foreach (var command in registry.All.OrderBy(c => c.Descriptor.Name, StringComparer.OrdinalIgnoreCase))
                Describe(context.Output, command, detailed: false);
        }

        private static void Describe(IConsoleOutput output, ICommand command, bool detailed)
        {
            var descriptor = command.Descriptor;
            var cheat = CheatCommands.IsCheat(command) ? " [cheat]" : string.Empty;
            output.Info($"{descriptor.Usage} - {descriptor.Description}{cheat}");
            if (!detailed) return;

            if (descriptor.Aliases.Count > 0)
                output.Info($"  aliases: {string.Join(", ", descriptor.Aliases)}");
            foreach (var p in descriptor.Parameters.Where(p => !string.IsNullOrEmpty(p.Description)))
                output.Info($"  <{p.Name}>: {p.Description}");
            foreach (var f in descriptor.Flags.Where(f => !string.IsNullOrEmpty(f.Description)))
                output.Info($"  --{f.Name}: {f.Description}");
        }
    }
}
