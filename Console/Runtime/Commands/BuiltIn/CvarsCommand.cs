using EldritchGames.EldritchLogger.Console.Arguments;
using EldritchGames.EldritchLogger.Console.Commands.Reflection;
using EldritchGames.EldritchLogger.Console.Output;
using EldritchGames.EldritchLogger.Console.Registry;
using System;
using System.Linq;

namespace EldritchGames.EldritchLogger.Console.Commands.BuiltIn
{
    /// <summary>Lists console variables with their current values.</summary>
    public sealed class CvarsCommand : IConsoleCommand
    {
        private readonly ICommandRegistry registry;

        public CommandDescriptor Descriptor { get; } = new(
            "cvars",
            "Lists console variables and their values.",
            parameters: new[] { ParameterSpec.Optional("filter", ArgumentTypes.String, "Only variables whose name contains this text.") });

        public CvarsCommand(ICommandRegistry registry)
        {
            this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        public void Execute(CommandContext context)
        {
            var filter = context.Arguments.GetOrDefault<string>("filter");
            var variables = registry.All.OfType<VariableCommand>()
                .Where(v => filter == null || v.Descriptor.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderBy(v => v.Descriptor.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (variables.Count == 0)
            {
                context.Output.Info(filter == null ? "No console variables." : $"No console variables matching '{filter}'.");
                return;
            }

            foreach (var variable in variables)
            {
                var flags = (variable.CanWrite ? "" : " [read-only]") + (variable.IsCheat ? " [cheat]" : "");
                var description = string.IsNullOrEmpty(variable.Description) ? "" : $"  ({variable.Description})";
                context.Output.Info($"{variable.Descriptor.Name} = {ArgumentTypeResolver.Format(variable.Value)}{flags}{description}");
            }
        }
    }
}
