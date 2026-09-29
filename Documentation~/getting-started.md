# Getting started

## 1 — Install

Add the package to `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.eldritchgames.eldritchlogger": "https://github.com/Eldritch-Games-MX/eldritch-logger.git"
  }
}
```

Input System, uGUI (TextMeshPro) and Newtonsoft JSON are installed with it.

## 2 — Create the settings asset

Open **Edit → Project Settings → Eldritch Logger** and click **Create
Assets/Resources/LogSettings.asset**. (You can also use **Assets → Create →
Eldritch Logger → Log Settings** inside a `Resources` folder.)

Configure it on the same page or in the inspector:

- **Minimum Log Level**: entries below it are discarded.
- **Categories**: toggle, color, and add or remove custom categories.
- **Sinks**: where entries go. The Unity Console sink is there by default. Use
  **Add Sink ▾** for JSON Lines, XML, text files, HTTP, or your own sink types.
  Each sink has its own minimum level.
- **Capture Unity Logs**: which of Unity's own messages are forwarded into
  the logger (errors and uncaught exceptions by default).
- **Advanced**: timestamp format, message prefix, category colors,
  stack-trace filtering, auto-initialization.

`LoggerBootstrap` builds the logger from this asset before the first scene
loads. No code is required.

## 3 — Get a logger

```csharp
using EldritchGames.EldritchLogger.Core;

public class PlayerController : MonoBehaviour
{
    private IEldritchLogger _logger;

    void Awake() => _logger = ELoggerFactory.GetLogger<PlayerController>();
}
```

Get the logger in `Awake`, not in a field initializer. Unity can construct
MonoBehaviours during edit-mode deserialization, before the logger is
installed, and a field initializer would capture a no-op logger. Analyzer rule
`ELG003` flags this.

Plain C# classes take the logger through their constructor:

```csharp
public class GameService
{
    private readonly IEldritchLogger _logger;
    public GameService(IEldritchLogger logger) => _logger = logger;
}
```

Every entry from a named logger carries `Logger = "PlayerController"`.

## 4 — Log

```csharp
_logger.Info("Player {Name} joined", player.Name);                  // message template
_logger.Error(ex, "Saving slot {Slot} failed", slot);

_logger.AtWarning(LogCategory.Network)                               // fluent builder
    .AddKeyValue("Peer", peer)
    .Log("Packet dropped");

using (_logger.BeginScope("MatchId", match.Id))                      // every entry inside carries MatchId
{
    _logger.Info("Round {Round} started", round);
}
```

See `logging.md` for everything the API can do.

## 5 — Write to a file

In `LogSettings`, click **Add Sink ▾ → JSON Lines File**. Enter Play Mode, log
something, then open **Tools → Eldritch Logger → Log Viewer**. The **Live** tab
shows the session, and the **File** tab opens the `.jsonl` file. The sink's
**Open Folder** button shows where files go (`Application.persistentDataPath`
by default).

## 6 — Add the in-game console (optional)

1. **Assets → Create → Eldritch Logger → Console Settings**.
2. Drag the `Eldritch Console` prefab (`Packages/Eldritch Logger/Console/Prefabs`)
   into the scene.
3. On its `ConsoleBootstrap`, assign the settings asset and an
   `InputActionReference` to toggle the console.

Press the toggle in Play Mode and type `help`. Logger entries appear in the
console, and `log.level debug` turns on debug logging for the session. See
`runtime-console.md` to add your own commands.

## 7 — Try the sample

**Package Manager → Eldritch Logger → Samples → Logger Sample Scene** shows
presets, categories, templates, scopes and the console working together.
