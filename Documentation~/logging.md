# Logging

All logging types are in `EldritchGames.EldritchLogger.Core` unless noted.

## Levels

`Debug` `Info` `Warning` `Error` `Critical`. An entry is written when its level
is at or above the settings' **Minimum Log Level** *and* at or above the
minimum level of at least one sink. `logger.IsEnabled(level, category)` answers
both questions at once.

## Categories

Built in: `General` `Gameplay` `UI` `Audio` `Network` `AI` `Physics`
`Animation` `Input` `Unity` (Unity's own captured messages). Add your own in
the `LogSettings` inspector, then log against them by name or with an enum:

```csharp
_logger.Log(LogLevel.Info, "Economy", "Player purchased sword");

public enum GameCategory { Economy, Quests }
_logger.Log(LogLevel.Warning, GameCategory.Quests, "Quest state corrupted");
```

Names are case-insensitive. Entries in categories that are not registered, or
are disabled, are discarded. To make category typos compile errors, generate a
`LogCategories` class (see `editor-tooling.md`).

## Direct calls

```csharp
_logger.Log(LogLevel.Info,    LogCategory.UI,      "Button clicked");
_logger.Log(LogLevel.Warning, LogCategory.Network, "Packet dropped");
```

## Fluent builder

```csharp
_logger.AtInfo(LogCategory.Gameplay)
    .AddKeyValue("ItemId", 42)
    .WithComponent(this)
    .Log("Player picked up item");

_logger.AtError(LogCategory.AI)
    .WithException(ex)
    .Log("AI navigation failed");
```

| Method | Purpose |
|---|---|
| `.AddKeyValue(key, value)` | A structured property |
| `.WithException(ex)` | Exception type, message and filtered stack trace |
| `.WithEvent(evt, name)` | The C# delegate or UnityEvent that triggered the entry |
| `.WithComponent(component)` | Component and GameObject names; the component becomes the context |
| `.WithContext(obj)` | The Unity object selected when the entry is clicked in the Unity Console |
| `.Category(category)` | Overrides the category |
| `.Log(message)` / `.Log(template, args)` | Writes the entry |

A builder is single-use. When the level or category is disabled, `AtInfo()`
and the others return a shared no-op builder, so a disabled chain allocates
nothing.

## Message templates

Put values in `{Holes}` and pass them as arguments instead of interpolating:

```csharp
_logger.Info("Player {Name} took {Damage:0.0} damage", player.Name, damage);   // General category
_logger.AtWarning(LogCategory.Network).Log("Lost {Packets} packets from {Peer}", n, peer);
_logger.Error(ex, "Saving slot {Slot} failed", slot);
```

- Each hole becomes a property (`Name`, `Damage`...) holding the original
  value. The template itself is stored as `MessageTemplate`, so a log server
  can group entries by template.
- Holes are filled in order. A hole without an argument stays as written
  (`{Player}`), and a property of the same name from `AddKeyValue` or a scope
  is still shown. Extra arguments are ignored. Analyzer rule `ELG006` flags
  both mistakes in constant templates.
- Formats (`{Damage:0.0}`) use the invariant culture. `{{` and `}}` are literal
  braces.
- Rendering never throws. An invalid format (`{Hp:D}` given a float) is
  ignored, and a value whose `ToString()` throws is shown as `<TypeName>`.
- A trailing exception with no hole left for it becomes the entry's exception:
  `logger.Error("Save {Slot} failed", slot, ex)` works like
  `logger.Error(ex, "Save {Slot} failed", slot)`.
- Text outputs (the Unity Console, text files, the in-game console) show the
  rendered message without repeating the filled hole properties. Structured
  outputs (JSON, XML, HTTP) keep every property.
- Nothing is rendered when the level is disabled. The argument array and boxed
  numbers are still created at the call site, so keep an `IsEnabled` check
  around Debug logging in hot paths. Analyzer rule `ELG002` flags unguarded
  Debug template calls.
- Don't build the template itself with `$"..."` or `+`: the values would be
  baked into the text and lost as properties. Analyzer rule `ELG004` flags it.

The shorthands `Debug`, `Info`, `Warning`, `Error` and `Critical` log to the
default category. Use `At*(category).Log(template, args)` for another one.

## Scopes

Properties added to every entry logged inside a block, by any logger:

```csharp
using (_logger.BeginScope(("MatchId", match.Id), ("Map", match.Map)))
{
    _logger.Info("Round {Round} started", round);   // carries MatchId and Map
    await SpawnWaveAsync();                          // scopes flow across await
}
```

- Inner scopes win over outer ones, and properties set on the entry itself win
  over scopes.
- Scopes flow across `await` and `Task.Run` (they live in an `AsyncLocal`).
- `LogScope.Push(...)` opens a scope without a logger reference.
- Keys cannot be null.

> **Don't keep a scope open across a coroutine `yield`.** Unity resumes
> coroutines without restoring the execution context. A scope opened before a
> `yield` stays active for *everything* logged on the main thread until the
> coroutine resumes and closes it. Open and close the scope between yields.
> Analyzer rule `ELG005` flags this.

## Enrichers

Enrichers add properties to every entry. `SceneEnricher` (`Scene`) and
`BuildVersionEnricher` (`BuildVersion`) are on by default. They never
overwrite a property the entry already has. See `extending.md` to write your
own.

## Unity's own logs

**Capture Unity Logs** in `LogSettings` forwards Unity's messages into the
logger, under the `Unity` category, so they reach file and remote sinks:

| Option | What is forwarded |
|---|---|
| `Off` | Nothing |
| `ErrorsAndExceptions` (default) | Engine errors, asserts, `Debug.LogError`, and uncaught exceptions with their stack trace |
| `WarningsAndAbove` | The above plus warnings |
| `All` | Everything, including `Debug.Log` |

- Forwarded entries are not sent back to the Unity Console or the in-game
  console, which already show them. The logger's own output is never
  re-captured.
- An uncaught exception is forwarded once, even though it arrives both as a
  .NET unhandled-exception event and as a Unity log entry.
- On an uncaught exception, buffered sinks are flushed (waiting at most one
  second), so the entries leading up to a crash are on disk.
- Every preset keeps the `Unity` category enabled, and settings assets created
  before it existed get it automatically when they load. If you disable it,
  captured messages are discarded and the inspector warns about it.

## Sinks

| Sink | Output |
|---|---|
| Unity Console | `Debug.Log*` with rich-text category colors and clickable context objects |
| JSON Lines File | `.jsonl`: one JSON object per line, readable even after a crash |
| XML File | `.xml`: a `<Logs>` document, closed on shutdown |
| Text File | `.txt`: plain text, no color tags |
| HTTP | Batched POSTs to a server: JSON array, JSON Lines, or CLEF for Seq |
| In-game console | Added automatically by `ConsoleBootstrap` |

**File sinks** write on a background thread, in order, through a bounded queue.
If the queue fills up, the oldest pending entries are dropped and the drop is
reported. Files go to `Application.persistentDataPath` unless a directory is
set. With **New File Per Session**, each run writes `name_yyyyMMdd_HHmmss.ext`
and only the newest **Max Session Files** are kept. Without it, one file is
overwritten each run.

**HTTP sink:** set the URL, payload format and headers (for example
`X-Seq-ApiKey`). For Seq, use `https://your-seq/api/events/raw?clef` with the
CLEF format. Templates are sent as `@mt`, so Seq groups events by template.

- Entries are sent from a background thread in batches, every **Batch Size**
  entries or every **Flush Interval**.
- Temporary failures (timeouts, 5xx, 408, 429) are retried with exponential
  backoff. Other 4xx responses are dropped and reported.
- Header values are stored in the settings asset and ship with builds. Use an
  ingest-only key.
- The HTTP sink is not available on WebGL.

Sinks can also be attached at runtime: `ELoggerFactory.Sinks?.AddSink(sink)`.

## Runtime control

`ELoggerFactory.Control` changes filtering for the current session without
modifying the settings asset:

```csharp
var control = ELoggerFactory.Control;           // null when no logger is installed, or it uses a custom filter
control.MinimumLevelOverride = LogLevel.Debug;  // null returns to the settings value
control.SetCategoryOverride(LogCategory.AI, false);
control.ClearOverrides();
control.Flush();
```

The console's `log.*` commands and the editor's **Logger Control** window are
built on this API (see `runtime-console.md` and `editor-tooling.md`).

## Diagnostics

Problems inside the logger (a sink that throws, a file that can't be written, a
full queue) are reported through `SelfLog`, which writes `[EldritchLogger]`
warnings to the Unity Console by default. Reports are capped at
`SelfLog.MaxReports` per session, so a broken sink can't flood the console.
Point `SelfLog.Output` somewhere else to capture them.
