# Eldritch Logger

Structured logging framework for Unity. Configurable log levels, categories, structured metadata, and multiple exporters (JSON, XML, Text, Unity Console), managed through a ScriptableObject.

## Features

- **Log levels:** `Debug` `Info` `Warning` `Error` `Critical`
- **Built-in categories:** `General` `Gameplay` `UI` `Audio` `Network` `AI` `Physics` `Animation` `Input`
- **Custom categories:** add named categories in the inspector, assign colors, log against them via string or any user-defined enum
- Color-coded output in the Unity Console
- ScriptableObject configuration (`LogSettings`)
- Fluent builder API — chainable, expressive, zero boilerplate
- Fire-and-forget API — `.Log()` returns `void`; file exporters run in the background
- Per-class named loggers — every entry carries `Logger = "ClassName"` for easy filtering
- SLF4J-style factory (`ELoggerFactory`) with a swappable `ILoggerFactory` back-end
- Constructor injection support for pure C# classes and DI containers (Zenject, VContainer)
- Component + GameObject context
- Event logging for C# delegates and UnityEvents
- Exception logging with type and message
- Exporters: JSON, XML, Text file, Unity Console
- Automatic cleanup of previous session logs
- **Runtime Console** — in-game command console that shows logger output, with extensible commands, autocompletion, history, and themes

## Installation

Add to `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.eldritchgames.eldritchlogger": "https://github.com/eldritchgames/eldritch-logger.git"
  }
}
```

## Setup

1. **Assets → Create → Eldritch Logger → Log Settings** — create a `LogSettings` asset.
2. Place it in a `Resources` folder: `Assets/Resources/LogSettings.asset`.
3. Configure in the inspector:
   - Minimum log level (entries below this level are silently filtered)
   - Enabled categories (unchecked categories are silently filtered)
   - **Custom Categories** — type a name and click **Add** to create a new category; set its color and toggle; click **✕** to remove
   - Export formats: JSON, XML, Text
   - Export directory and file name
   - Console options: color coding, stack trace suppression

> **Note:** The bootstrapper (`LoggerBootstrap`) auto-initializes before the first scene loads using `Resources.Load`. No code required.

## Usage

```csharp
using EldritchGames.EldritchLogger.Core;
```

### Obtaining a Logger

Each class declares its own named logger. The name stamps `Logger = "ClassName"` on every entry.

**MonoBehaviour — initialize in `Awake` (required):**
```csharp
public class PlayerController : MonoBehaviour
{
    private IEldritchLogger _logger;

    void Awake()
    {
        _logger = ELoggerFactory.GetLogger<PlayerController>();
    }
}
```

> Do **not** use a field initializer. Unity runs MonoBehaviour constructors during edit-mode deserialization, before `LoggerBootstrap` registers the factory. The logger would silently be a no-op.

**Pure C# class — constructor injection:**
```csharp
public class GameService
{
    private readonly IEldritchLogger _logger;

    public GameService(IEldritchLogger logger)
    {
        _logger = logger;
    }
}
```

**Dynamic / one-off:**
```csharp
var logger = ELoggerFactory.GetLogger("MySubsystem");
```

### Direct Logging

```csharp
_logger.Log(LogLevel.Info,    LogCategory.UI,      "Button clicked");
_logger.Log(LogLevel.Warning, LogCategory.Network, "Packet dropped");
_logger.Log(LogLevel.Error,   LogCategory.AI,      "Pathfinding failed");
```

### Custom Categories

Add categories without touching source. In the `LogSettings` inspector, type a name under **Custom Categories** and click **Add**. Assign a color and toggle it on/off like any built-in category.

Log against them by **string** or by **your own enum**:

```csharp
// String — quick and direct
_logger.Log(LogLevel.Info, "Economy", "Player purchased sword");

// Enum — compile-time safe, recommended for larger projects
public enum GameCategory { Economy, Quests, Systems }

_logger.Log(LogLevel.Debug,   GameCategory.Economy, "Gold overflow detected");
_logger.Log(LogLevel.Warning, GameCategory.Quests,  "Quest state corrupted");
```

The enum value's name must exactly match the category name registered in `LogSettings` (case-sensitive). Custom categories respect the enabled toggle and color settings just like built-in ones.

> **Collision guard:** the inspector prevents adding a custom category whose name matches a built-in `LogCategory` enum value.

### Fluent Builder

```csharp
_logger.AtInfo(LogCategory.Gameplay)
    .AddKeyValue("ItemId", 42)
    .AddKeyValue("PlayerId", player.Id)
    .WithComponent(this)
    .Log("Player picked up item");

_logger.AtError(LogCategory.AI)
    .AddKeyValue("State", "Pathfinding")
    .WithException(new InvalidOperationException("No path found"))
    .Log("AI navigation failed");

_logger.AtWarning(LogCategory.Network)
    .WithEvent(OnPlayerDeath, nameof(OnPlayerDeath))
    .Log("Player disconnected during death event");
```

### Fluent Builder Reference

| Method                   | Purpose                                                        |
|--------------------------|----------------------------------------------------------------|
| `.AddKeyValue(key, val)` | Attach structured metadata (e.g. `"Score": 9001`)             |
| `.WithException(ex)`     | Attach an exception (type + message)                          |
| `.WithEvent(evt, name)`  | Attach event context (C# delegate or UnityEvent)              |
| `.WithComponent(comp)`   | Attach `Component@GameObject` context                         |
| `.Category(category)`    | Override the log category                                     |
| `.Log("message")`        | Dispatch — returns `void`, file exporters run in background   |

### Exporters

| Exporter       | Format | Destination                              |
|----------------|--------|------------------------------------------|
| Unity Console  | Text   | Always active — Unity Console            |
| Text Exporter  | `.txt` | `LogSettings.exportDirectory/fileName`  |
| JSON Exporter  | `.json`| `LogSettings.exportDirectory/fileName`  |
| XML Exporter   | `.xml` | `LogSettings.exportDirectory/fileName`  |

Export directory defaults to `Application.persistentDataPath` unless overridden in `LogSettings`.

### DI Container Integration

`ELoggerFactory` holds a swappable `ILoggerFactory`. Bind a custom implementation once at startup to redirect all logging through your container:

```csharp
// Zenject example — call from an Installer
Container.Bind<ILoggerFactory>().To<EldritchLoggerFactory>().AsSingle();
ELoggerFactory.SetFactory(Container.Resolve<ILoggerFactory>());
```

```csharp
// Manual override (tests, custom bootstrap)
ELoggerFactory.SetFactory(new EldritchLoggerFactory(myRootLogger));
```

`ELoggerFactory.ClearFactory()` resets to the no-op `NullLogger`. Called automatically on application quit.

### Custom Sinks

Attach additional sinks at runtime through `ELoggerFactory.Sinks` (null until the logger is initialized):

```csharp
ELoggerFactory.Sinks?.AddSink(mySink);    // mySink : ILogSink
ELoggerFactory.Sinks?.RemoveSink(mySink);
```

## Runtime Console

An in-game command console lives in the `EldritchLogger.Console` assembly (`using EldritchGames.EldritchLogger.Console.*`). It requires the Input System and uGUI/TextMeshPro packages, which are installed as dependencies.

### Setup

1. **Assets → Create → Eldritch Logger → Console Settings** — create a `CommandConsoleSettings` asset.
2. Drag the `Packages/Eldritch Logger/Console/Prefabs/Eldritch Console` prefab into your scene.
3. On its `ConsoleBootstrap` component assign the settings asset and an `InputActionReference` for toggling the console. Optionally assign a `LogSettings` asset (defaults to `Resources/LogSettings`).

### Logger Integration

| Setting (`CommandConsoleSettings`) | Effect |
|---|---|
| `showEldritchLogs` | Registers a `ConsoleLogSink` so EldritchLogger entries appear in the console, formatted like the Unity Console output. |
| `showUnityLogs` | Also shows plain `Debug.Log` messages (`Application.logMessageReceived`). |

When both are enabled, the Unity Console echo of a logger entry is filtered out, so each entry appears only once.

### Built-in Commands

| Command | Description |
|---|---|
| `help [command]` | Lists all commands, or usage for one |
| `clear` | Clears the console output |
| `history [--limit N]` | Shows previously entered commands |
| `repeat <n> <command> [--silent] [--delay=N]` | Repeats a command |
| `theme <name>` | Applies a theme from `Resources/Themes` (`Dark`, `Light`, `Solarized Dark`, `Solarized Light`) |

### Custom Commands

Implement `IConsoleCommand` (positional args) or `IAdvancedConsoleCommand` (flags/options), and register them through an `ICommandGroup`. Groups are discovered automatically at startup; constructor parameters are resolved from `ServiceRegistry`.

```csharp
using EldritchGames.EldritchLogger.Console.Commands;
using EldritchGames.EldritchLogger.Console.Registry;

public class GameplayCommands : ICommandGroup
{
    public string Name => "Gameplay";
    public void Register(ICommandRegistry registry) => registry.Register(new GodModeCommand());
}
```

New themes: **Assets → Create → Eldritch Logger → Console Theme**, placed in a `Resources/Themes` folder.

### Migrating from `eldritch-console`

The standalone console package has been merged into this package.
- Remove the old `eldritch-console` folder / package from your project.
- Replace `EldritchGames.EldritchConsole` with `EldritchGames.EldritchLogger.Console` in `using` statements.
- `ConsoleTheme` now lives in `EldritchGames.EldritchLogger.Console.Settings`.
- Assembly references: `EldritchConsole.Runtime` → `EldritchLogger.Console`.
- Script and asset GUIDs are unchanged, so existing scenes, prefabs and theme assets keep their references.

## Troubleshooting

**Nothing prints in the Console**
- Check that `LogSettings.asset` exists at `Assets/Resources/LogSettings.asset`. The bootstrapper logs an error if it can't find it.
- Verify the minimum **log level** in `LogSettings` — entries below it are silently dropped.
- Verify the **enabled categories** — all categories must be checked for the corresponding logs to appear.
- For MonoBehaviours: make sure the logger is initialized in `Awake()`, not as a field initializer.

**Custom category logs not appearing**
- Confirm the category name is registered in `LogSettings` under **Custom Categories** and its toggle is enabled.
- When using an enum, verify `MyEnum.Value.ToString()` matches the registered name exactly — it is case-sensitive.
- Custom categories added at runtime are not persisted; they must be registered in the `LogSettings` asset.

**File exports not created**
- Enable at least one export format in `LogSettings`.
- Check `exportDirectory` — by default it writes to `Application.persistentDataPath`.

**DI container: logs appear as no-ops**
- Call `ELoggerFactory.SetFactory(...)` before any MonoBehaviour `Awake` runs.

## Samples

Import from **Package Manager → Eldritch Logger → Samples → Logger Sample Scene**.
