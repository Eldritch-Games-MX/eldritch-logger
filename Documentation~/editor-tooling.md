# Editor tooling

All windows are under **Tools → Eldritch Logger**.

## Log Viewer

**Tools → Eldritch Logger → Log Viewer** browses logger entries:

- **Live:** entries from the current Play Mode session. They are captured from
  the moment the logger starts, even while the window is closed. Turn capture
  off with the **Capture** toggle.
- **File:** opens a `.jsonl` file written by the JSON Lines sink. **Open Log
  Folder** jumps to the default log directory. Lines cut off by a crash are
  skipped and counted.

Filter by level (with counts), category, logger name and free text, which also
searches properties and exceptions. Select an entry to see its properties and
exception. Double-click an entry to select its context object in the scene.

## Console Commands window

**Tools → Eldritch Logger → Console Commands**:

- **Play Mode:** the live registry (usage, aliases, cheat flag, and the group
  or type that registered each command), commands that were skipped with the
  reason, name and alias conflicts, and a box to run commands.
- **Edit Mode:** every discovered command group and `[ConsoleCommand]` type,
  flagging constructor dependencies the console does not provide by default.
  Those types would be skipped unless the dependency is registered through
  `ConsoleBootstrap.ConfiguringServices`.

## Project Settings

**Edit → Project Settings → Eldritch Logger** shows which `LogSettings` asset
the bootstrap will load (and offers to create one), the full settings
inspector, and code generation options. The **Console** sub-page covers
release safety and every console settings asset.

## Category code generation

Generate a static class with one `LogCategory` field per configured category,
so a typo becomes a compile error:

```csharp
_logger.AtInfo(LogCategories.LootDrops).Log("Chest opened");
```

Set the output file, namespace and class name in Project Settings, then click
**Generate Now** (or **Generate Class** in the `LogSettings` inspector). Enable
**Regenerate automatically** to update the class whenever categories are added
or removed. The file is only rewritten when its content changes, and only under
`Assets/`. The options are stored in
`ProjectSettings/EldritchLoggerSettings.asset`; commit it.

## Sink inspector helpers

File sinks have an **Open Folder** button. In Play Mode, the `LogSettings`
inspector lists the running sinks with their dropped-entry counts, and a
**Reveal** button for log files.

## Code analyzers

The package ships Roslyn analyzers that run on every assembly referencing the
logger:

| ID | Warns about |
|---|---|
| `ELG001` | `Debug.Log*` in game code. Use an `IEldritchLogger` so entries are filtered and reach every sink. Editor assemblies (`*.Editor`, `Assembly-CSharp-Editor`) are exempt. |
| `ELG002` | A Debug-level message built with interpolation, concatenation or `string.Format`, or a Debug template call with arguments (`logger.Debug("Hp {Hp}", hp)`), outside an `if (logger.IsEnabled(...))` check. The string, or the argument array and boxed values, are created even when Debug is off. |
| `ELG003` | A MonoBehaviour field initialized with `ELoggerFactory.GetLogger`. Initialize it in `Awake()` instead. |
| `ELG004` | A message template built with interpolation or concatenation (`logger.Info($"Player {name}")`). Put values in holes instead, so they are captured as properties. |
| `ELG005` | A `yield return` while a log scope is open (`using (logger.BeginScope(...))` or `using var`). The scope would leak to unrelated logs while the coroutine is suspended. |

Suppress a rule locally with `#pragma warning disable ELG001`. To change
severities, add `Assets/Default.ruleset` (all assemblies) or
`Assets/<AssemblyName>.ruleset` (one assembly).

The analyzer source is in `Analyzers~/`. After changing it, rebuild with
`dotnet build Analyzers~/EldritchLogger.Analyzers -c Release`, which copies the
DLL to `Runtime/Analyzers/`. Run the rule tests with
`dotnet run --project Analyzers~/EldritchLogger.Analyzers.Tests`.
