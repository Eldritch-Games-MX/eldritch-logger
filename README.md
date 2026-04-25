# Eldritch Logger

Structured logging framework for Unity. Configurable log levels, categories, structured metadata, and multiple exporters (JSON, XML, Text, Unity Console), managed through a ScriptableObject.

## Features

- **Log levels:** `Debug` `Info` `Warning` `Error` `Critical`
- **Categories:** `General` `Gameplay` `UI` `Audio` `Network` `AI` `Physics` `Animation` `Input`
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

## Troubleshooting

**Nothing prints in the Console**
- Check that `LogSettings.asset` exists at `Assets/Resources/LogSettings.asset`. The bootstrapper logs an error if it can't find it.
- Verify the minimum **log level** in `LogSettings` — entries below it are silently dropped.
- Verify the **enabled categories** — all categories must be checked for the corresponding logs to appear.
- For MonoBehaviours: make sure the logger is initialized in `Awake()`, not as a field initializer.

**File exports not created**
- Enable at least one export format in `LogSettings`.
- Check `exportDirectory` — by default it writes to `Application.persistentDataPath`.

**DI container: logs appear as no-ops**
- Call `ELoggerFactory.SetFactory(...)` before any MonoBehaviour `Awake` runs.

## Samples

Import from **Package Manager → Eldritch Logger → Samples → Logger Sample Scene**.
