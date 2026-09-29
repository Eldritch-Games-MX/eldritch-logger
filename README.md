# Eldritch Logger

Structured logging for Unity, with an in-game command console. Game code logs
through one small interface; a `LogSettings` asset decides which levels and
categories are written and where they go: the Unity Console, JSON Lines, XML
or text files, a log server over HTTP, the in-game console, or your own sink.

Logging never throws and never blocks the game. Disabled levels cost nothing,
file and network writes happen on background threads, and a failing sink is
reported and skipped. Entries are structured: template holes, scopes and
enrichers become properties that files, Seq and the Log Viewer can filter by.

See `Documentation~/index.md` for the full guide, `Documentation~/getting-started.md`
to set it up, `Documentation~/logging.md` for the logging API,
`Documentation~/runtime-console.md` for the in-game console,
`Documentation~/editor-tooling.md` for the editor windows and analyzers,
`Documentation~/architecture.md` for how the pieces fit together, and
`Documentation~/extending.md` to add sinks, enrichers and commands from your own
code.

## Features

- **Levels** `Debug` `Info` `Warning` `Error` `Critical`, and **categories**:
  built-in ones plus any you add in the inspector
- **Message templates**: `logger.Info("Player {Name} joined", name)` records
  `Name` as a property
- **Scopes**: `using (logger.BeginScope("MatchId", id))` tags every entry in
  the block, across `await`
- Fluent builder with structured properties, exceptions and clickable context
  objects
- Per-class named loggers through `ELoggerFactory`, with a swappable back-end
- Sinks with their own minimum level: Unity Console, JSON Lines, XML, text,
  HTTP (JSON, JSON Lines or CLEF for Seq), or your own
- One file per session with automatic retention; bounded background queues
- Captures Unity's own errors and uncaught exceptions into your sinks
- Runtime overrides for level and categories that never touch the asset
- **Runtime console**: typed commands, autocomplete, history and themes;
  commands from plain methods (`[ConsoleMethod]`) and console variables
  (`[ConsoleVariable]`); `log.*` commands; cheat gating; stripped from release
  builds by default
- **Editor tooling**: Log Viewer, Console Commands window, Project Settings,
  category code generation, and Roslyn analyzers ELG001–ELG005

## Installation

Add to `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.eldritchgames.eldritchlogger": "https://github.com/Eldritch-Games-MX/eldritch-logger.git"
  }
}
```

Input System, uGUI (TextMeshPro) and Newtonsoft JSON are installed with it.

## Quick start

1. Open **Edit → Project Settings → Eldritch Logger** and click **Create
   Assets/Resources/LogSettings.asset**.
2. Set the minimum level and categories, and add sinks with **Add Sink ▾**.
   The logger builds itself from this asset before the first scene loads.
3. Get a logger in `Awake` and log:

```csharp
using EldritchGames.EldritchLogger.Core;

public class PlayerController : MonoBehaviour
{
    private IEldritchLogger _logger;

    void Awake() => _logger = ELoggerFactory.GetLogger<PlayerController>();

    void Start() => _logger.Info("Player {Name} spawned at {Position}", name, transform.position);
}
```

4. Optional: drag the `Eldritch Console` prefab into the scene, assign a
   Console Settings asset and a toggle action, and type `help` in Play Mode.

## Examples

### Templates, the builder and exceptions

```csharp
_logger.Info("Player {Name} took {Damage:0.0} damage", player.Name, damage);
_logger.Error(ex, "Saving slot {Slot} failed", slot);

_logger.AtWarning(LogCategory.Network)
    .AddKeyValue("Peer", peer)
    .WithComponent(this)          // click the entry in the Unity Console to select this object
    .Log("Lost {Packets} packets", lost);
```

### Custom categories

```csharp
_logger.Log(LogLevel.Info, "Economy", "Player purchased sword");      // added in the LogSettings inspector
_logger.AtInfo(LogCategories.LootDrops).Log("Chest opened");          // generated class: typos don't compile
```

### Scopes

```csharp
using (_logger.BeginScope(("MatchId", match.Id), ("Map", match.Map)))
{
    _logger.Info("Round {Round} started", round);   // carries MatchId and Map
    await SpawnWaveAsync();                          // and so does everything logged in here
}
```

### Guarding expensive debug logging

```csharp
if (_logger.IsEnabled(LogLevel.Debug, LogCategory.AI))
    _logger.AtDebug(LogCategory.AI).Log("Path {Path}", string.Join(" → ", path));
```

### Runtime control

```csharp
ELoggerFactory.Control.MinimumLevelOverride = LogLevel.Debug;   // this session only
ELoggerFactory.Control.SetCategoryOverride(LogCategory.AI, false);
```

Or from the in-game console: `log.level debug`, `log.category AI off`, `log.reset`.

### A console command from a method

```csharp
public static class DebugCommands
{
    [ConsoleMethod("give", "Adds an item to the player.")]
    static string Give(string item, int amount = 1) => Inventory.Add(item, amount);

    [ConsoleVariable("timescale", "Game speed.", Min = 0, Max = 10)]
    static float TimeScale { get => Time.timeScale; set => Time.timeScale = value; }
}
```

### A custom sink

```csharp
public sealed class AnalyticsSink : ILogSink
{
    public string Name => "Analytics";
    public LogLevel MinimumLevel => LogLevel.Error;
    public void Emit(LogEntryDto entry) => Analytics.Queue(entry.Category, entry.Message);
}

[Serializable]
public sealed class AnalyticsSinkConfig : LogSinkConfig   // appears in Add Sink ▾
{
    public override string DisplayName => "Analytics";
    public override ILogSink CreateSink(SinkBuildContext context) => new AnalyticsSink();
}
```

## Samples

Import from **Package Manager → Eldritch Logger → Samples → Logger Sample Scene**.
