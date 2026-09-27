using EldritchGames.EldritchLogger.Console.Commands;
using EldritchGames.EldritchLogger.Console.Core;
using EldritchGames.EldritchLogger.Console.Logging;
using EldritchGames.EldritchLogger.Console.Parsing;
using EldritchGames.EldritchLogger.Console.Registry;
using EldritchGames.EldritchLogger.Console.Settings;
using EldritchGames.EldritchLogger.Console.UI;
using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Settings;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EldritchGames.EldritchLogger.Console.Loader
{
    public class ConsoleBootstrap : MonoBehaviour
    {
        [SerializeField] private CommandConsoleSettings settings;
        [SerializeField] private ConsoleThemeApplier themeApplier;
        [SerializeField] private ConsoleView view;
        [SerializeField] private InputActionReference toggleConsole;
        [Tooltip("Formatting settings for logger entries. Falls back to Resources/LogSettings when empty.")]
        [SerializeField] private LogSettings logSettings;

        private ConsoleController controller;
        private CommandHistory commandHistory;
        private ConsoleLogSink logSink;
        private ISinkRegistry sinkRegistry;

        private void Awake()
        {
            commandHistory = new CommandHistory(settings.historySize);

            ICommandRegistry registry = new CommandRegistry();
            ICommandParser parser = new CommandParser();
            ICommandExecutor executor = new CommandExecutor(registry, commandHistory);
            IThemeLoader themeLoader = new ResourcesThemeLoader();
            Lexer lexer = new();

            // Seed services
            ServiceRegistry.Register(registry);
            ServiceRegistry.Register(view);
            ServiceRegistry.Register(commandHistory);
            ServiceRegistry.Register(settings);
            ServiceRegistry.Register(parser);
            ServiceRegistry.Register(executor);
            ServiceRegistry.Register(lexer);
            ServiceRegistry.Register(themeApplier);
            ServiceRegistry.Register(themeLoader);

            // Register built-in groups
            CoreCommandGroup.RegisterCoreCommands(registry, commandHistory, parser, lexer, themeApplier, themeLoader, view, executor);

            // Discover plugin groups
            foreach (var group in CommandGroupLoader.DiscoverGroups(strict: true))
                group.Register(registry);

            controller = new ConsoleController(view, parser, executor, toggleConsole.action, registry, settings.showUnityLogs);

            if (settings.showEldritchLogs)
                AttachLoggerSink();

            if (settings.ignoreConsecutiveDuplicates)
                commandHistory.EnableDuplicateFiltering();
        }

        private void AttachLoggerSink()
        {
            sinkRegistry = ELoggerFactory.Sinks;
            if (logSettings == null)
                logSettings = Resources.Load<LogSettings>("LogSettings");

            if (sinkRegistry == null || logSettings == null)
            {
                Debug.LogWarning("ConsoleBootstrap: EldritchLogger is not initialized; the console will only show Unity logs.");
                sinkRegistry = null;
                return;
            }

            logSink = new ConsoleLogSink(logSettings);
            sinkRegistry.AddSink(logSink);
            controller.FilterLoggerEchoes = true;
        }

        private void Update()
        {
            logSink?.Flush(view);
        }

        private void OnDestroy()
        {
            if (logSink != null)
                sinkRegistry?.RemoveSink(logSink);

            controller?.Dispose();
        }
    }
}
