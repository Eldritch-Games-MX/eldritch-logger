using EldritchGames.EldritchLogger.Console.Execution;
using EldritchGames.EldritchLogger.Console.Registry;
using EldritchGames.EldritchLogger.Console.Settings;
using EldritchGames.EldritchLogger.Console.Themes;
using System;

namespace EldritchGames.EldritchLogger.Console.Commands.BuiltIn
{
    /// <summary>The built-in commands: help, clear, history, repeat, theme.</summary>
    public sealed class CoreCommandGroup : ICommandGroup
    {
        private readonly CommandHistory history;
        private readonly ICommandExecutor executor;
        private readonly IConsoleThemeApplier themeApplier;
        private readonly IThemeLoader themeLoader;
        private readonly CommandConsoleSettings settings;

        public string Name => "Core";

        public CoreCommandGroup(CommandHistory history,
                                ICommandExecutor executor,
                                IConsoleThemeApplier themeApplier = null,
                                IThemeLoader themeLoader = null,
                                CommandConsoleSettings settings = null)
        {
            this.history = history ?? throw new ArgumentNullException(nameof(history));
            this.executor = executor ?? throw new ArgumentNullException(nameof(executor));
            this.themeApplier = themeApplier;
            this.themeLoader = themeLoader;
            this.settings = settings;
        }

        public void Register(ICommandRegistry registry)
        {
            registry.Register(new HelpCommand(registry));
            registry.Register(new ClearCommand());
            registry.Register(new HistoryCommand(history, settings != null ? settings.defaultHistoryLimit : 20));
            registry.Register(new RepeatCommand(executor, settings != null ? settings.maxRepeatCount : 1000));

            if (themeApplier != null && themeLoader != null)
                registry.Register(new ThemeCommand(themeApplier, themeLoader));
        }
    }
}
