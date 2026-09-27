using EldritchGames.EldritchLogger.Console.Arguments;
using EldritchGames.EldritchLogger.Console.Autocompletion;
using EldritchGames.EldritchLogger.Console.Commands;
using EldritchGames.EldritchLogger.Console.Execution;
using EldritchGames.EldritchLogger.Console.Logging;
using EldritchGames.EldritchLogger.Console.Output;
using EldritchGames.EldritchLogger.Console.Parsing;
using EldritchGames.EldritchLogger.Console.Registry;
using EldritchGames.EldritchLogger.Console.Services;
using EldritchGames.EldritchLogger.Console.Settings;
using EldritchGames.EldritchLogger.Console.Themes;
using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Formatting;
using EldritchGames.EldritchLogger.Settings;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EldritchGames.EldritchLogger.Console.UI
{
    /// <summary>
    /// Composition root of the in-game console: builds the services, discovers commands,
    /// wires the controller and attaches the console to the EldritchLogger.
    /// </summary>
    public class ConsoleBootstrap : MonoBehaviour
    {
        [SerializeField] private CommandConsoleSettings settings;
        [SerializeField] private ConsoleThemeApplier themeApplier;
        [SerializeField] private ConsoleView view;
        [SerializeField] private InputActionReference toggleConsole;
        [Tooltip("Accepts the autocomplete suggestion. Defaults to the Tab key when empty.")]
        [SerializeField] private InputActionReference acceptSuggestion;
        [Tooltip("Formatting settings for logger entries. Falls back to Resources/LogSettings when empty.")]
        [SerializeField] private LogSettings logSettings;

        private ConsoleServiceProvider services;
        private ConsoleController controller;
        private ConsoleLogSink logSink;
        private ISinkRegistry sinkRegistry;
        private InputAction defaultAcceptAction;

        /// <summary>
        /// Raised before commands are discovered, so game code can register services that
        /// command groups and commands receive through their constructors.
        /// Subscribe before the console's <c>Awake</c> (e.g. from a <c>[RuntimeInitializeOnLoadMethod]</c>).
        /// </summary>
        public static event System.Action<ConsoleServiceProvider> ConfiguringServices;

        public ICommandRegistry Registry { get; private set; }
        public ICommandExecutor Executor { get; private set; }
        public IConsoleOutput Output { get; private set; }

        private void Awake()
        {
            if (settings == null)
                settings = ScriptableObject.CreateInstance<CommandConsoleSettings>();

            var history = new CommandHistory(settings.historySize, settings.ignoreConsecutiveDuplicates);
            var mirror = settings.mirrorCommandOutputToLogger ? ELoggerFactory.GetLogger("Console") : null;
            IThemeLoader themeLoader = new ResourcesThemeLoader(settings.themesResourcePath);

            Registry = new CommandRegistry();
            Output = new ConsoleOutput(view, mirror);
            Executor = new CommandExecutor(Registry, new Lexer(), new CommandParser(), new ArgumentBinder(),
                                           history, Output, new CoroutineCommandRunner(this));

            services = new ConsoleServiceProvider()
                .Register(Registry)
                .Register(Executor)
                .Register(Output)
                .Register<IConsoleView>(view)
                .Register<IConsoleOutputView>(view)
                .Register(history)
                .Register(settings)
                .Register<IConsoleThemeApplier>(themeApplier)
                .Register(themeLoader);

            try
            {
                ConfiguringServices?.Invoke(services);
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
            }

            new CommandDiscovery(services, message => Debug.LogWarning($"[Console] {message}")).RegisterAll(Registry);

            var acceptAction = acceptSuggestion != null ? acceptSuggestion.action : CreateDefaultAcceptAction();
            controller = new ConsoleController(view, Executor, new AutocompleteProvider(Registry),
                                               toggleConsole != null ? toggleConsole.action : null,
                                               acceptAction, settings.showUnityLogs);

            if (settings.showEldritchLogs)
                AttachLoggerSink();
        }

        private InputAction CreateDefaultAcceptAction()
        {
            defaultAcceptAction = new InputAction("AcceptSuggestion", binding: "<Keyboard>/tab");
            return defaultAcceptAction;
        }

        private void AttachLoggerSink()
        {
            sinkRegistry = ELoggerFactory.Sinks;
            if (logSettings == null)
                logSettings = Resources.Load<LogSettings>(LoggerBootstrap.SettingsResourcePath);

            if (sinkRegistry == null)
            {
                Debug.LogWarning("[Console] EldritchLogger is not initialized; the console will only show Unity logs.");
                return;
            }

            logSink = new ConsoleLogSink(new TextLogFormatter(logSettings, richText: true),
                                         settings.minimumLoggerLevel, settings.loggerQueueCapacity);
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
            defaultAcceptAction?.Dispose();
            services?.Clear();
        }
    }
}
