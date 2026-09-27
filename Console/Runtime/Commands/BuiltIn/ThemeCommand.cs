using EldritchGames.EldritchLogger.Console.Arguments;
using EldritchGames.EldritchLogger.Console.Output;
using EldritchGames.EldritchLogger.Console.Themes;
using System;
using System.Linq;

namespace EldritchGames.EldritchLogger.Console.Commands.BuiltIn
{
    public sealed class ThemeCommand : IConsoleCommand
    {
        private readonly IConsoleThemeApplier themeApplier;
        private readonly IThemeLoader themeLoader;

        public CommandDescriptor Descriptor { get; }

        public ThemeCommand(IConsoleThemeApplier themeApplier, IThemeLoader themeLoader)
        {
            this.themeApplier = themeApplier ?? throw new ArgumentNullException(nameof(themeApplier));
            this.themeLoader = themeLoader ?? throw new ArgumentNullException(nameof(themeLoader));

            Descriptor = new CommandDescriptor(
                "theme",
                "Applies a console theme, or lists the available themes.",
                parameters: new[]
                {
                    ParameterSpec.Optional("name",
                        ArgumentTypes.Choice("theme", () => themeLoader.LoadAllThemes().Select(t => t.name)))
                });
        }

        public void Execute(CommandContext context)
        {
            var name = context.Arguments.GetOrDefault<string>("name");
            if (name == null)
            {
                var themes = themeLoader.LoadAllThemes();
                if (themes.Length == 0)
                {
                    context.Output.Info("No themes found.");
                    return;
                }

                context.Output.Info("Available themes:");
                foreach (var theme in themes)
                    context.Output.Info($"- {theme.name}");
                return;
            }

            var selected = themeLoader.LoadTheme(name);
            if (selected == null)
            {
                context.Output.Warn($"Theme '{name}' not found.");
                return;
            }

            themeApplier.ApplyTheme(selected);
            context.Output.Info($"Theme '{selected.name}' applied.");
        }
    }
}
