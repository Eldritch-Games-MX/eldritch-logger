using EldritchGames.EldritchLogger.Console.Commands;
using EldritchGames.EldritchLogger.Console.Parsing;
using EldritchGames.EldritchLogger.Console.Registry;
using EldritchGames.EldritchLogger.Console.Loader;

namespace EldritchGames.EldritchLogger.Console.Core
{
    public static class CoreCommandGroup
    {
        public static void RegisterCoreCommands(ICommandRegistry registry,
                                                CommandHistory history,
                                                ICommandParser parser,
                                                Lexer lexer,
                                                IConsoleThemeApplier themeApplier,
                                                IThemeLoader themeLoader,
                                                IConsoleView view,
                                                ICommandExecutor executor)
        {
            registry.Register(new HelpCommand(registry));
            registry.Register(new RepeatCommand(executor, parser, lexer));
            registry.Register(new ThemeCommand(themeApplier, themeLoader));
            registry.Register(new HistoryCommand(history));
            registry.Register(new ClearCommand(view));
        }
    }
}
