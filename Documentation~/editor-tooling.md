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
- **Follow:** keeps reading the open file as it grows, like `tail -f`, for
  example while a player build writes it. If the file is overwritten by a new
  session, the view starts over.

Filtering:

- By level (with counts), category, logger name and free text. The text search
  also covers properties and exceptions.
- **Origin:** entries logged by your code, entries captured from Unity's own
  log (marked `[Unity]`), or both.
- **By property:** select an entry and click **Filter** next to one of its
  properties (for example `MatchId = 42`) to show only entries with that value.
  This turns a scope into "everything from this match". Filters stack.
- Active filters appear as chips under the toolbar. Click one to remove it, or
  **Clear filters** to remove them all.

**Group** collapses entries by message template, most frequent first, with a
count, the most severe level and when the template was last seen.
`Lost {Packets} packets from {Peer}` becomes one row however the values differ,
which is the quickest way to find noisy log lines. Double-click a group (or
**Show these entries**) to list its entries.

The details pane shows the entry's properties and its exception or stack
trace. Stack trace lines with a source location are links: click one to open
the script at that line. Traces from player builds are matched to the project
by their `Assets/` path. Double-click an entry, or use **Select context**, to
select its context object in the scene.

## Logger Control

**Tools → Eldritch Logger → Logger Control** is the editor version of the
console's `log.*` commands, for scenes without an in-game console. In Play Mode:

- The minimum level, with an override badge and **Reset**. A warning appears
  when no sink accepts the chosen level.
- Every category with an on/off toggle, override badges and **Reset All
  Overrides**.
- The running sinks with their minimum level, dropped entries, file location
  (**Reveal**), and for the HTTP sink queued, sent and retried counts and the
  last error. **Flush** writes out everything buffered.
- A **Test Entry** box that logs a message at any level.

Overrides last for the session. The `LogSettings` asset is never changed.

## Console Commands window

**Tools → Eldritch Logger → Console Commands**:

- **Play Mode:**
  - The live registry: usage, aliases, cheat flag, and the group or type that
    registered each command.
  - A box to run commands, and the last commands entered (**Edit** copies one
    into the box, **Run** runs it again).
  - **Console Variables:** the live value of every `[ConsoleVariable]`, editable
    in place (toggles for `bool`, dropdowns for enums, text fields for
    everything else). Changes go through the console, so ranges, read-only
    variables and cheats are respected.
  - Commands that were skipped, with the reason, and name/alias conflicts.
- **Edit Mode:**
  - Every discovered command group and `[ConsoleCommand]` type, flagging
    constructor dependencies that the console does not provide by default.
    Those types are skipped unless the dependency is registered through
    `ConsoleBootstrap.ConfiguringServices`.
  - Every `[ConsoleMethod]` and `[ConsoleVariable]` member with its usage, or
    the reason it would be skipped (an unsupported parameter type, an instance
    method outside a MonoBehaviour...). Problems are listed first, so you can
    fix them without entering Play Mode.

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

**Find Unused Categories** in the `LogSettings` inspector searches the scripts
under `Assets/` for each custom category, as a string (`"Economy"`, any case)
or as its generated field or an enum member (`LogCategories.Economy`). The ones
nobody mentions are marked **unused?**. It is a text search, so a category
name built at runtime isn't found; check before removing one.

## Sink inspector helpers

- **Output Preview:** every sink in the `LogSettings` inspector can show how a
  sample entry is written in its format: plain text, JSON Lines, XML, the HTTP
  payload (JSON, JSON Lines or CLEF), or the colored Unity Console line. The
  sample is logged through a real logger with a template, a scope property and
  an exception, so it shows exactly what reaches the file or server. Custom
  sink configs can override `LogSinkConfig.Preview` to show theirs.
- **Send Test Entry** (HTTP sink): posts the sample once with the current URL,
  format and headers and shows the result (`200 OK`, `401 Unauthorized`, a
  timeout...), so a wrong URL or API key shows up before Play Mode.
- File sinks have an **Open Folder** button.
- In Play Mode, the inspector lists the running sinks with their dropped-entry
  counts, the HTTP sink's queued, sent and retried counts and last error, and a
  **Reveal** button for log files.

## Code analyzers

The package ships Roslyn analyzers that run on every assembly referencing the
logger:

| ID | Warns about | Quick fix |
|---|---|---|
| `ELG001` | `Debug.Log*` in game code. Use an `IEldritchLogger` so entries are filtered and reach every sink. Editor assemblies (`*.Editor`, `Assembly-CSharp-Editor`) are exempt. | |
| `ELG002` | A Debug-level message built with interpolation, concatenation or `string.Format`, or a Debug template call with arguments (`logger.Debug("Hp {Hp}", hp)`), outside an `if (logger.IsEnabled(...))` check. The string, or the argument array and boxed values, are created even when Debug is off. | Wraps the call in `if (logger.IsEnabled(LogLevel.Debug, category))`, using the call's own category |
| `ELG003` | A MonoBehaviour field initialized with `ELoggerFactory.GetLogger`. Initialize it in `Awake()` instead. | Moves the initialization into `Awake()`, creating the method if needed |
| `ELG004` | A message template built with interpolation or concatenation (`logger.Info($"Player {name}")`). Put values in holes instead, so they are captured as properties. | Rewrites it as a template with arguments: `$"Player {player.Name} took {damage:0.0}"` becomes `"Player {Name} took {Damage:0.0}", player.Name, damage` |
| `ELG005` | A `yield return` while a log scope is open (`using (logger.BeginScope(...))` or `using var`). The scope would leak to unrelated logs while the coroutine is suspended. | |
| `ELG006` | A template whose holes and arguments don't match: a hole without an argument (shown as written), an extra argument (ignored; a trailing exception is fine), or a repeated hole name. | |

Quick fixes appear as light-bulb actions in Rider and Visual Studio. They live
in `EldritchLogger.Analyzers.CodeFixes.dll`, next to the analyzer. Unity's
compiler loads it but only runs the analyzers.

Suppress a rule locally with `#pragma warning disable ELG001`. To change
severities, add `Assets/Default.ruleset` (all assemblies) or
`Assets/<AssemblyName>.ruleset` (one assembly).

The analyzer and code fix sources are in `Analyzers~/`. After changing them,
rebuild with `dotnet build Analyzers~/EldritchLogger.Analyzers -c Release` and
`dotnet build Analyzers~/EldritchLogger.Analyzers.CodeFixes -c Release`, which
copy the DLLs to `Runtime/Analyzers/`. Run the rule and fix tests with
`dotnet run --project Analyzers~/EldritchLogger.Analyzers.Tests`.
