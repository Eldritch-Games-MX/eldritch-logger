# Architecture

## Assemblies

| Assembly | Contents |
|---|---|
| `EldritchLogger` | The logging pipeline, sinks, settings and bootstrap. Ships the Roslyn analyzers (`Runtime/Analyzers`). |
| `EldritchLogger.Editor` | Log Viewer, Project Settings, inspectors, category code generation |
| `EldritchLogger.Console` | The in-game console. Depends on the logger, never the other way round. |
| `EldritchLogger.Console.Editor` | Console Commands window, build stripping, the disable define |

Tests live in `Tests~` and the analyzer source in `Analyzers~`, so neither is
imported into projects that use the package.

## The pipeline

Every entry takes the same path through `EldritchLogger`:

```
logger.Info("Player {Name} joined", name)
  → IsEnabled(level, category)          settings filter (level, category, runtime overrides)
                                        and the lowest sink minimum level; nothing is built if false
  → LogBuilder                          renders the template, records the filled holes
  → LogEntry                            immutable: level, category, message, properties, exception, context
  → timestamp from IClock
  → properties                          entry first, then open scopes, then enrichers (missing keys only)
  → ILogEntryMapper → LogEntryDto       strings only: invariant-culture values, filtered stack traces
  → ILogDispatcher                      each sink at or above its minimum level, isolated from the others
  → ILogSink.Emit                       Unity Console, files, HTTP, the in-game console...
```

Each stage is an interface handed to `EldritchLoggerBuilder`, so a test or a
game can replace any of them.

### Core types

- **`IEldritchLogger`**: the whole contract is `IsEnabled` and `Log(LogEntry)`.
  Everything else (`Info`, `AtWarning`, `BeginScope`...) is an extension
  method in `EldritchLoggerExtensions`. Implementing a logger means writing
  two members.
- **`LogEntry`** (domain) and **`LogEntryDto`** (what sinks see). The entry
  keeps typed values and the exception object. The DTO holds only strings, so
  every sink serializes the same text and none of them touches a live object
  on a background thread.
- **`LogCategory`**: a struct wrapping a case-insensitive name. Built-in
  categories are static fields; enums and strings convert implicitly.
- **`ILoggerFactory` / `ELoggerFactory`**: `ELoggerFactory` is the static
  entry point game code uses. It returns a `NullLogger` until a factory is
  set, which is why loggers are fetched in `Awake`.
- **`LoggerBootstrap`**: `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]`
  loads `Resources/LogSettings`, builds the logger with
  `EldritchLoggerBuilder.FromSettings`, installs it, and shuts it down on
  `Application.quitting`. `Installed` and `Uninstalling` let tooling (the Log
  Viewer's live capture) attach sinks without the runtime knowing about it.

## Logging never throws, never blocks

This is the guardrail everything else serves.

- `EldritchLogger.Log` wraps the pipeline in one try/catch. Enrichers and sinks
  are each isolated: one failing never stops the others.
- Template rendering and property formatting go through `LogValues.Format`,
  which never throws: a bad format string falls back to the default format, and
  a throwing `ToString()` becomes `<TypeName>`.
- `EldritchLogger.Flush` catches per sink, because it also runs from the
  unhandled-exception handler, where a second exception would be lost.
- Failures are reported through `SelfLog`, which has its own re-entry guard
  and a report cap. It must never log through the logger.
- Slow work happens off the logging thread. `BackgroundLogWriter` owns one
  thread per file, and `BatchingLogSink` owns one per HTTP sink. Their queues
  are bounded: when full, the *oldest* pending entry is dropped and counted.
  The game never waits on a disk or a network.

When changing the pipeline, keep both properties. A test that makes a sink,
enricher, formatter or value throw belongs in the same fixture as the code
under test.

## Disabled logging is free

- `IsEnabled` runs before any allocation. It combines the settings filter with
  `SinkCollection.LowestMinimumLevel`, so a level that no sink accepts is
  rejected as early as a filtered one.
- `At*()` returns a shared `NullLogBuilder` when disabled.
- The logger only copies an entry's property dictionary when a scope is open
  or an enricher is registered.
- The category-override lookup is skipped entirely while there are no
  overrides (a volatile flag).
- What the library cannot avoid, the call site's `params` array and boxing,
  is what analyzer rules ELG002 and ELG004 point at.

## Properties and precedence

Properties come from four places, applied in this order, each only adding keys
that are still missing:

1. The entry itself (`AddKeyValue`, template holes).
2. Open scopes, innermost first (`LogScope`, stored in an `AsyncLocal`).
3. Enrichers, in registration order.

So the entry wins over scopes, inner scopes over outer ones, and scopes over
enrichers. Scopes are applied by the logger, not by an enricher, so
`ClearEnrichers()` cannot turn them off by accident.

Filled template holes are listed in `LogEntry.RenderedProperties`, and the
mapper marks them `MetadataEntry.InMessage`. Text formatters skip those (the
value is already in the message). Structured sinks ignore the flag and write
everything.

## Threading

- Any thread may log. `SinkCollection` swaps an immutable snapshot on
  add/remove, so dispatch never locks.
- `Emit` runs on the logging thread. Sinks must return quickly and must not
  mutate the shared DTO.
- Sinks that touch Unity (the in-game console) queue formatted lines and drain
  them on the main thread.
- `LogDispatcher.IsDispatching` is thread-static and marks the echo of the
  Unity Console sink, so capturing Unity's log cannot loop.

## Capturing Unity's log

`UnityLogForwarder` listens to `Application.logMessageReceivedThreaded` and
`AppDomain.UnhandledException`, and turns Unity's messages into entries in the
`Unity` category marked with the reserved property `EldritchLogger.Source`.
Three rules keep it from feeding back on itself:

- It ignores messages written while the logger is dispatching, while `SelfLog`
  is reporting, or inside `UnityLogForwarder.SuppressCapture()`.
- The logger does not send captured entries to sinks marked `IShowsUnityLog`
  (the Unity Console and the in-game console), which already show them.
  `SinkCollection` keeps that shorter sink list ready, so this costs nothing
  per entry.

Both rules are enforced by `EldritchLogger` itself, around whatever
`ILogDispatcher` is configured, so a custom dispatcher cannot lose them.
- An uncaught exception reported by both sources within a short window is
  forwarded once.

On an uncaught exception it flushes the sinks on another thread, and waits at
most `CrashFlushTimeout`, so a hung sink can't hang a crashing game.

## Files

`FileLogSink` subclasses (JSON Lines, XML, text) only decide how an entry
becomes text. `BackgroundLogWriter` does the rest: one thread, ordered writes,
a bounded queue, a header and footer, and error handling per entry. A disk-full
error loses that entry, not the writer. `Flush` waits until every accepted
entry has been *written*, not just dequeued. After a timed-out `Dispose`, the
leftover entries are counted as dropped and no file is touched.

`LogFileLocator` decides file names and session retention. It matches the
extension exactly, because Windows patterns such as `*.txt` also match
`.txtx`.

## Settings and runtime control

`LogSettings` is the only configuration source. Its sink list is a
`[SerializeReference]` list of `LogSinkConfig`. Any serializable subclass
appears in **Add Sink ▾**, so new sink types need no editor code.
`OnAfterDeserialize` adds built-in categories missing from older assets.

`SettingsLogFilter` reads the asset live, and holds the runtime overrides
exposed as `ILogControl`. Overrides live in memory only. Writing them to the
asset would make a play session change the project.

## The console

```
input → ConsoleController → CommandExecutor
          → Lexer → CommandParser → ArgumentBinder (against the CommandDescriptor)
          → cheat policy → IConsoleCommand.Execute / IAsyncConsoleCommand via ICommandRunner
          → IConsoleOutput → ConsoleView (pooled lines, redrawn once per frame)
```

- **`CommandDescriptor`** is the single description of a command: name,
  aliases, parameters with typed `IArgumentType`s, flags and the cheat flag.
  Binding, validation, `help`, usage strings and autocomplete all read it, so
  they cannot disagree.
- **`CommandDiscovery`** finds `ICommandGroup`s, `[ConsoleCommand]` types,
  `[ConsoleMethod]` methods and `[ConsoleVariable]` members in non-test
  assemblies. It builds commands through `ConsoleServiceProvider` (constructor
  injection, largest satisfiable constructor) and records everything it skips
  in a `DiscoveryReport` instead of throwing. One broken type or member never
  stops discovery.
- **`ConsoleBootstrap`** composes it all in `Awake`. `ConfiguringServices` runs
  before discovery, so games can add services or replace the `ICheatPolicy`.
- **Availability** is checked twice: at runtime (`ConsoleAvailabilityPolicy`
  destroys the console root), and at build time (`ConsoleBuildProcessor` strips
  it from scenes). A release player built with the default settings has no
  console objects in its scenes at all.
- **`ConsoleLogSink`** is the only link between logger and console, and it is
  added by the console. The logger does not know the console exists.

## Deliberate decisions

- **No `Debug.Log` inside the package's runtime paths**, except the Unity
  Console sink and `SelfLog`'s default output. Analyzer ELG001 is the
  reminder.
- **No file rotation by size.** It was built and removed: part files
  complicated retention, the Log Viewer and every external reader. One file
  per session is the unit.
- **Values are formatted with the invariant culture**, in messages and
  properties alike, so a log written on a German machine parses the same as one
  written anywhere else.
