# Eldritch Logger

Structured logging framework for Unity, with an in-game command console. Configurable log levels, categories, structured metadata and pluggable sinks (Unity Console, JSON Lines, XML, text files, the in-game console, or your own), managed through a ScriptableObject.

## Features

- **Log levels:** `Debug` `Info` `Warning` `Error` `Critical`
- **Categories:** built-in `General` `Gameplay` `UI` `Audio` `Network` `AI` `Physics` `Animation` `Input`, plus any custom category you add in the inspector — all toggled and colored the same way
- Fluent builder API; disabled levels cost nothing (no allocations)
- Per-class named loggers — every entry carries `Logger = "ClassName"`
- SLF4J-style factory (`ELoggerFactory`) with a swappable `ILoggerFactory` back-end
- Enrichers — scene and build version are added automatically; add your own (player id, session id...)
- Sinks with their own minimum level; add custom sinks from the inspector or code
- File sinks write on a background thread, in order, through a bounded queue (logging never blocks the game)
- One file per session with automatic retention of the last N sessions
- Thread-safe: log from worker threads
- Unity context objects — click a log line to select the GameObject
- **Runtime Console** — in-game command console that shows logger output, with typed commands, autocompletion, history and themes

## Installation

Add to `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.eldritchgames.eldritchlogger": "https://github.com/Eldritch-Games-MX/eldritch-logger.git"
  }
}
```

Dependencies (installed automatically): Input System, uGUI (TextMeshPro), Newtonsoft JSON.

## Setup

1. **Assets → Create → Eldritch Logger → Log Settings** — create a `LogSettings` asset.
2. Place it in a `Resources` folder: `Assets/Resources/LogSettings.asset`.
3. Configure it in the inspector:
   - **Minimum Log Level** — entries below it are discarded.
   - **Categories** — toggle, color, and add/remove custom categories.
   - **Sinks** — where entries go. The Unity Console sink is there by default; use **Add Sink ▾** for JSON Lines, XML or text files (or your own sink types). Each sink has its own minimum level.
   - **Advanced** — timestamp format, message prefix, category colors, stack-trace filtering, auto-initialization.

`LoggerBootstrap` builds the logger from this asset before the first scene loads. No code required.

## Usage

```csharp
using EldritchGames.EldritchLogger.Core;
```

### Obtaining a Logger

**MonoBehaviour — initialize in `Awake`:**
```csharp
public class PlayerController : MonoBehaviour
{
    private IEldritchLogger _logger;

    void Awake() => _logger = ELoggerFactory.GetLogger<PlayerController>();
}
```

> Do **not** use a field initializer: Unity can construct MonoBehaviours during edit-mode deserialization, before the factory is registered, and the logger would be a no-op.

**Pure C# class — constructor injection:**
```csharp
public class GameService
{
    private readonly IEldritchLogger _logger;
    public GameService(IEldritchLogger logger) => _logger = logger;
}
```

### Direct Logging

```csharp
_logger.Log(LogLevel.Info,    LogCategory.UI,      "Button clicked");
_logger.Log(LogLevel.Warning, LogCategory.Network, "Packet dropped");
```

### Custom Categories

Add a category in the `LogSettings` inspector, then log against it by name or with your own enum:

```csharp
_logger.Log(LogLevel.Info, "Economy", "Player purchased sword");

public enum GameCategory { Economy, Quests }
_logger.Log(LogLevel.Warning, GameCategory.Quests, "Quest state corrupted");
```

Names are case-insensitive. Entries in categories that are not registered (or are disabled) are discarded.

### Fluent Builder

```csharp
_logger.AtInfo(LogCategory.Gameplay)
    .AddKeyValue("ItemId", 42)
    .WithComponent(this)
    .Log("Player picked up item");

_logger.AtError(LogCategory.AI)
    .WithException(ex)
    .Log("AI navigation failed");
```

| Method                   | Purpose                                                          |
|--------------------------|------------------------------------------------------------------|
| `.AddKeyValue(key, val)` | Structured metadata                                              |
| `.WithException(ex)`     | Exception (type, message, filtered stack trace)                  |
| `.WithEvent(evt, name)`  | C# delegate or UnityEvent that triggered the entry               |
| `.WithComponent(comp)`   | Component and GameObject names; the component becomes the context |
| `.WithContext(obj)`      | Unity object selected when clicking the entry in the Unity Console |
| `.Category(category)`    | Override the category                                            |
| `.Log("message")`        | Dispatch                                                         |

A builder is single-use. When the level/category is disabled, `AtInfo()` etc. return a shared no-op builder, so disabled logging allocates nothing. Use `_logger.IsEnabled(level, category)` to skip expensive message construction.

### Sinks

| Sink (inspector)  | Output                                                        |
|-------------------|---------------------------------------------------------------|
| Unity Console     | `Debug.Log` with rich-text category colors and context objects |
| JSON Lines File   | `.jsonl` — one JSON object per line, valid even after a crash |
| XML File          | `.xml` — `<Logs>` document, closed on shutdown                |
| Text File         | `.txt` — plain text, no color tags                            |

File sinks write to `Application.persistentDataPath` unless a directory is set. With **New File Per Session** each run gets `name_yyyyMMdd_HHmmss.ext` and only the newest **Max Session Files** are kept. If a file sink's queue fills up, the oldest pending entries are dropped and the drop is reported.

### Extending the Logger

**Custom sink:** implement `ILogSink`. `Emit` runs on the logging thread and must return quickly.

```csharp
public sealed class AnalyticsSink : ILogSink
{
    public string Name => "Analytics";
    public LogLevel MinimumLevel => LogLevel.Error;
    public void Emit(LogEntryDto entry) => Analytics.Queue(entry.Category, entry.Message);
}
```

To make it configurable in the inspector, add a config class — it appears in **Add Sink ▾** automatically:

```csharp
[Serializable]
public sealed class AnalyticsSinkConfig : LogSinkConfig
{
    public override string DisplayName => "Analytics";
    public override ILogSink CreateSink(SinkBuildContext context) => new AnalyticsSink();
}
```

Or attach a sink at runtime: `ELoggerFactory.Sinks?.AddSink(sink)`.

**Custom enricher:** implement `ILogEnricher` to add properties to every entry (must be thread-safe).

**Composing the logger in code:** turn off **Auto Initialize** in `LogSettings` and build it yourself:

```csharp
var logger = EldritchLoggerBuilder.FromSettings(settings)
    .AddEnricher(new PlayerIdEnricher())
    .AddSink(new AnalyticsSink())
    .Build();
LoggerBootstrap.Install(logger);
```

### DI Container Integration

```csharp
// Zenject example
Container.Bind<ILoggerFactory>().FromInstance(new EldritchLoggerFactory(rootLogger)).AsSingle();
ELoggerFactory.SetFactory(Container.Resolve<ILoggerFactory>());
```

`ELoggerFactory.ClearFactory()` resets to the no-op `NullLogger`.

## Runtime Console

The in-game console lives in the `EldritchLogger.Console` assembly (`EldritchGames.EldritchLogger.Console.*`).

### Setup

1. **Assets → Create → Eldritch Logger → Console Settings** — create a `CommandConsoleSettings` asset.
2. Drag the `Eldritch Console` prefab (`Packages/Eldritch Logger/Console/Prefabs`) into your scene.
3. On `ConsoleBootstrap`, assign the settings asset and an `InputActionReference` to toggle the console. Optionally assign an accept-suggestion action (defaults to Tab) and a `LogSettings` asset for formatting.

### Logger Integration

| Setting (`CommandConsoleSettings`) | Effect |
|---|---|
| `showEldritchLogs` / `minimumLoggerLevel` | Show EldritchLogger entries in the console |
| `showUnityLogs` | Also show plain `Debug.Log` messages |
| `mirrorCommandOutputToLogger` | Send command output to the logger too (e.g. to keep it in log files) |
| `maxBufferedLines` / `poolSize` | Lines kept in memory / line objects on screen |

Each logger entry appears once even when both logger and Unity logs are shown. The view redraws at most once per frame, so log bursts don't stall the game.

### Built-in Commands

| Command | Description |
|---|---|
| `help [command]` (`?`) | Lists all commands, or details for one |
| `clear` (`cls`) | Clears the console |
| `history [--limit=N]` | Shows previously entered commands |
| `repeat <count> [--silent] [--delay=ms] <command...>` | Repeats a command (runs as a coroutine) |
| `theme [name]` | Lists themes, or applies one from `Resources/Themes` |

Flags accept `--name=value` or `--name value`. Tab completes command names, flags and argument values.

### Custom Commands

Describe the command's arguments once; binding, validation, `help` and autocomplete all use the description. Write output to `context.Output`, not `Debug.Log`.

```csharp
using EldritchGames.EldritchLogger.Console.Arguments;
using EldritchGames.EldritchLogger.Console.Commands;
using EldritchGames.EldritchLogger.Console.Output;

[ConsoleCommand] // discovered automatically; constructor parameters are injected
public sealed class GiveCommand : IConsoleCommand
{
    private readonly Inventory inventory;

    public GiveCommand(Inventory inventory) => this.inventory = inventory;

    public CommandDescriptor Descriptor { get; } = new(
        "give", "Adds an item to the inventory.",
        parameters: new[]
        {
            ParameterSpec.Required("item", ArgumentTypes.Choice("item", () => ItemDatabase.Names)),
            ParameterSpec.Optional("amount", ArgumentTypes.PositiveInt)
        },
        flags: new[] { FlagSpec.Switch("silent") });

    public void Execute(CommandContext context)
    {
        var item = context.Arguments.Get<string>("item");
        int amount = context.Arguments.GetOrDefault("amount", 1);
        inventory.Add(item, amount);
        if (!context.Arguments.HasFlag("silent"))
            context.Output.Info($"Gave {amount} x {item}.");
    }
}
```

- Multi-frame commands implement `IAsyncConsoleCommand` and return an `IEnumerator`.
- Several related commands can be registered together by an `ICommandGroup`.
- Constructor dependencies come from the console's service provider (registry, executor, output, view, history, settings, theme services). A command or group whose dependencies are missing is skipped with a warning. Register your own services before the console starts:

```csharp
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
static void RegisterConsoleServices() =>
    ConsoleBootstrap.ConfiguringServices += services => services.Register(Inventory.Instance);
```

- New argument types: implement `IArgumentType` (`TryParse`, `Suggest`).

### Themes

**Assets → Create → Eldritch Logger → Console Theme**, placed in `Resources/Themes` (folder configurable). To theme extra UI elements, add a `ThemeableGraphic` component or implement `IThemeable`.

## Troubleshooting

**Nothing prints**
- Check that `LogSettings.asset` is at `Assets/Resources/LogSettings.asset` (an error is logged otherwise) and that **Auto Initialize** is on.
- Check the minimum level, the category toggles, and each sink's minimum level.
- Initialize MonoBehaviour loggers in `Awake()`, not in a field initializer.

**Custom category logs don't appear**
- The category must be registered and enabled in `LogSettings`.

**No log files**
- Add a file sink under **Sinks**. Files go to `Application.persistentDataPath` by default.
- Internal problems (a sink that throws, a full file queue) are reported in the Unity Console with an `[EldritchLogger]` prefix.

**Console command not found**
- Look for a `[Console] Skipped ...` warning: a constructor dependency is not registered.

## Samples

Import from **Package Manager → Eldritch Logger → Samples → Logger Sample Scene**.
