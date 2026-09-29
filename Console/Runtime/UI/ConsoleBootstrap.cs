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
        [Tooltip("Object removed when the console is not available (see CommandConsoleSettings.availability). Defaults to this GameObject.")]
        [SerializeField] private GameObject consoleRoot;

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

        /// <summary>
        /// Service types the bootstrap registers before discovery. Command dependencies outside this
        /// list must be registered through <see cref="ConfiguringServices"/>.
        /// </summary>
        public static readonly System.Type[] DefaultServiceTypes =
        {
            typeof(ICommandRegistry), typeof(ICommandExecutor), typeof(IConsoleOutput), typeof(IConsoleView),
            typeof(IConsoleOutputView), typeof(CommandHistory), typeof(CommandConsoleSettings),
            typeof(IConsoleThemeApplier), typeof(IThemeLoader), typeof(ICheatPolicy)
        };

        public ICommandRegistry Registry { get; private set; }
        public ICommandExecutor Executor { get; private set; }
        public IConsoleOutput Output { get; private set; }

        /// <summary>Result of command discovery: registrations, skipped types and conflicts.</summary>
        public DiscoveryReport Discovery { get; private set; }

        public CommandConsoleSettings Settings => settings;

        /// <summary>The object removed when the console is unavailable.</summary>
        public GameObject ConsoleRoot => consoleRoot != null ? consoleRoot : gameObject;

        /// <summary>All services available to commands (after <see cref="ConfiguringServices"/>).</summary>
        public ConsoleServiceProvider Services => services;

        private void Awake()
        {
            if (!ConsoleAvailabilityPolicy.IsAvailableHere(settings))
            {
                Destroy(ConsoleRoot);
                enabled = false;
                return;
            }

            if (settings == null)
                settings = ScriptableObject.CreateInstance<CommandConsoleSettings>();

            var history = new CommandHistory(settings.historySize, settings.ignoreConsecutiveDuplicates);
            var mirror = settings.mirrorCommandOutputToLogger ? ELoggerFactory.GetLogger("Console") : null;
            IThemeLoader themeLoader = new ResourcesThemeLoader(settings.themesResourcePath);

            Registry = new CommandRegistry();
            Output = new ConsoleOutput(view, mirror);
            var executor = new CommandExecutor(Registry, new Lexer(), new CommandParser(), new ArgumentBinder(),
                                               history, Output, new CoroutineCommandRunner(this));
            Executor = executor;

            services = new ConsoleServiceProvider()
                .Register(Registry)
                .Register(Executor)
                .Register(Output)
                .Register<IConsoleView>(view)
                .Register<IConsoleOutputView>(view)
                .Register(history)
                .Register(settings)
                .Register<IConsoleThemeApplier>(themeApplier)
                .Register(themeLoader)
                .Register<ICheatPolicy>(new SettingsCheatPolicy(settings));

            try
            {
                ConfiguringServices?.Invoke(services);
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
            }

            executor.CheatPolicy = services.Get<ICheatPolicy>();
            Discovery = new CommandDiscovery(services, message => Debug.LogWarning($"[Console] {message}")).RegisterAll(Registry);

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
