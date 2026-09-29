# Eldritch Logger

A structured logging framework for Unity, with an in-game command console. Game
code logs through one small interface; where the entries go (the Unity Console,
files, a log server, the in-game console) is decided by a settings asset, not by
the code that logs.

## Design principles

1. **Logging never breaks the game.** A log call never throws, never blocks on
   I/O and never stalls a frame. A sink that throws, a disk that fills up or a
   value whose `ToString()` fails is reported through `SelfLog` and skipped;
   the caller carries on.
2. **Disabled logging is free.** `IsEnabled` is checked before anything is
   built. A disabled builder chain returns a shared no-op builder, and a
   disabled template is never rendered. Analyzer rules point out the
   allocations that only an `IsEnabled` check can remove.
3. **Structured, not just text.** Template holes, scopes, enrichers and
   `AddKeyValue` all become properties of the entry, so files, log servers and
   the Log Viewer can filter and group by value instead of by parsing text.
4. **One pipeline, every destination.** Every entry takes the same path:
   filter, properties, mapping, then every sink that wants it. A sink only
   decides how to write an entry, never whether the rest of the pipeline runs.
5. **Configuration is an asset.** Levels, categories, colors and sinks live in
   a `LogSettings` ScriptableObject. The logger builds itself from it before
   the first scene loads. Runtime overrides (`log.level`, `log.category`) never
   write back to the asset.
6. **Seams over switches.** Sinks, enrichers, filters, mappers, dispatchers,
   console commands, argument types and cheat policies are all interfaces. New
   behaviour is added from your own assembly, not by editing the package.
7. **Development tools stay out of release builds.** The console is stripped
   from release players unless you say otherwise, and cheat commands only run
   when a policy allows them.

## What the package covers

| Concern | Types | Notes |
|---|---|---|
| Logging API | `IEldritchLogger`, `EldritchLoggerExtensions`, `ILogBuilder` | Direct calls, a fluent builder and message templates |
| Obtaining loggers | `ELoggerFactory`, `ILoggerFactory`, `EldritchLoggerFactory` | One named logger per class; swappable back-end |
| Levels and categories | `LogLevel`, `LogCategory`, `LogSettings` | Built-in and custom categories, toggled and colored in the inspector |
| Properties | `MessageTemplate`, `LogScope`, `ILogEnricher` | Template holes, ambient scopes, automatic scene and build version |
| Destinations | `ILogSink`, `LogSinkConfig` | Unity Console, JSON Lines, XML, text, HTTP, the in-game console, your own |
| Unity's own logs | `UnityLogForwarder` | Engine errors and uncaught exceptions reach your sinks too |
| Runtime control | `ILogControl` | Level and category overrides that never touch the asset |
| In-game console | `ConsoleBootstrap`, `IConsoleCommand`, `[ConsoleMethod]`, `[ConsoleVariable]` | Typed commands, autocomplete, history, themes, cheats, release stripping |
| Editor tooling | Log Viewer, Logger Control, Console Commands window, sink previews, code generation | Plus Roslyn analyzers ELG001–ELG006 with quick fixes |

## Non-goals

- **Log storage and search.** The package writes files and posts to servers
  (Seq via CLEF, or any HTTP endpoint). Indexing and dashboards are the
  server's job. The Log Viewer is for one session or one file, not for a fleet.
- **Crash reporting.** Uncaught exceptions are captured and flushed, but
  symbolication, minidumps and crash grouping belong to a crash reporter.
- **Analytics.** An analytics sink is a few lines (see `extending.md`), but
  sampling, batching into events and player consent are the analytics
  package's concern.
- **Log rotation by size.** Each file sink writes one file per session and
  keeps the newest N sessions. Splitting files into parts was tried and
  removed: it complicated every reader for little gain.

## Where to go next

- `getting-started.md`: install, create the settings asset, log your first entries.
- `logging.md`: levels, categories, the builder, templates, scopes, Unity's logs, sinks and runtime control.
- `runtime-console.md`: the in-game console, built-in and logger commands, method commands, console variables, release safety.
- `editor-tooling.md`: Log Viewer, Logger Control, Console Commands window, Project Settings, sink previews, category code generation, analyzers.
- `architecture.md`: how the pieces fit together, and the guardrails worth keeping.
- `extending.md`: custom sinks, enrichers, commands, argument types and cheat policies.
- `troubleshooting.md`: when nothing prints, or the console does not appear.
