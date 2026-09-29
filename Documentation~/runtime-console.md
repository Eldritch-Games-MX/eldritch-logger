# Runtime console

The in-game console lives in the `EldritchLogger.Console` assembly
(`EldritchGames.EldritchLogger.Console.*`). It shows logger output, runs typed
commands with autocomplete and history, and is stripped from release builds by
default.

## Setup

1. **Assets → Create → Eldritch Logger → Console Settings**: create a
   `CommandConsoleSettings` asset.
2. Drag the `Eldritch Console` prefab (`Packages/Eldritch Logger/Console/Prefabs`)
   into your scene.
3. On `ConsoleBootstrap`, assign the settings asset and an
   `InputActionReference` to toggle the console. Optionally assign an
   accept-suggestion action (Tab by default) and a `LogSettings` asset for
   formatting.

## Logger integration

| Setting (`CommandConsoleSettings`) | Effect |
|---|---|
| `showEldritchLogs` / `minimumLoggerLevel` | Show logger entries in the console |
| `showUnityLogs` | Also show plain `Debug.Log` messages |
| `mirrorCommandOutputToLogger` | Send command output to the logger too, for example to keep it in log files |
| `maxBufferedLines` / `poolSize` | Lines kept in memory / line objects on screen |

Each entry appears once, even when both logger and Unity logs are shown. The
view redraws at most once per frame, so bursts of logs don't stall the game.

## Built-in commands

| Command | Description |
|---|---|
| `help [command]` (`?`) | Lists all commands, or details for one |
| `clear` (`cls`) | Clears the console |
| `history [--limit=N]` | Shows previously entered commands |
| `repeat <count> [--silent] [--delay=ms] <command...>` | Repeats a command (runs as a coroutine) |
| `theme [name]` | Lists themes, or applies one from `Resources/Themes` |
| `cvars [filter]` | Lists console variables and their values |

Flags accept `--name=value` or `--name value`. Tab completes command names,
flags and argument values.

## Logger commands

These inspect and adjust the running logger. Overrides are **runtime-only**:
the `LogSettings` asset is never modified, even in the editor.

| Command | Description |
|---|---|
| `log.level [level\|reset]` | Shows the minimum level, overrides it, or returns to the settings value. Warns when no sink accepts that level |
| `log.category <name> [on\|off\|reset]` | Shows a category's state, or turns it on or off |
| `log.categories` | Lists categories and whether they are logged (overrides marked) |
| `log.sinks` | Lists running sinks, with dropped counts and file paths |
| `log.flush` | Writes out everything buffered by the sinks |
| `log.open` | Opens the log folder (the folders of running file sinks, or `Application.persistentDataPath`) |
| `log.reset` | Removes every override |
| `log.test [message...] [--level=Warning]` | Logs a test entry (Info by default) |

## Method commands

The quickest way to add a command is to mark a method. Its parameters become
typed arguments, with validation, `help` and autocomplete.

```csharp
using EldritchGames.EldritchLogger.Console.Commands.Reflection;

public static class DebugCommands
{
    [ConsoleMethod("give", "Adds an item to the player.")]
    static string Give(string item, int amount = 1) => Inventory.Add(item, amount);   // the return value is printed

    [ConsoleMethod("god", "Invulnerability.", IsCheat = true, Aliases = new[] { "iddqd" })]
    static void God(bool enabled) => Player.Invulnerable = enabled;

    [ConsoleMethod("spawnwave")]
    static IEnumerator SpawnWave(int count, IConsoleOutput output)                     // IEnumerator runs as a coroutine
    {
        for (int i = 0; i < count; i++) { Spawner.SpawnOne(); output.Info($"Spawned {i + 1}"); yield return new WaitForSeconds(0.5f); }
    }
}

public class Door : MonoBehaviour
{
    [ConsoleMethod("opendoors")]   // instance methods run on every active Door
    void Open() { ... }
}
```

- Supported parameter types: `int`, `float`, `double`, `bool` (true/false,
  on/off, yes/no, 1/0), `string` and enums.
- Optional parameters become optional arguments. A trailing `params` array
  takes any number of values.
- `CommandContext` and `IConsoleOutput` parameters are passed in by the console.
- The name defaults to the method name in lower case.
- Methods with unsupported parameters are skipped. The reason appears in the
  Console Commands window and as a `[Console]` warning.
- The attributes derive from `PreserveAttribute`, so IL2CPP stripping keeps the
  members.

## Console variables

```csharp
public static class GameVars
{
    [ConsoleVariable("timescale", "Game speed.", Min = 0, Max = 10)]
    static float TimeScale { get => Time.timeScale; set => Time.timeScale = value; }

    [ConsoleVariable("fps")]
    static int Fps => Mathf.RoundToInt(1f / Time.smoothDeltaTime);   // no setter: read-only

    [ConsoleVariable("difficulty", IsCheat = true)]
    static Difficulty Difficulty = Difficulty.Normal;                 // reading is free, changing needs cheats
}
```

`timescale` prints the value and `timescale 0.5` sets it. `cvars` lists every
variable. Static fields and properties of the same types as method parameters
are supported. `Min`/`Max` limit numeric values, and `ReadOnly = true` blocks
writes.

## Commands with full control

For custom argument types, flags or multi-frame commands, implement
`IConsoleCommand` (see `extending.md`).

## Themes

**Assets → Create → Eldritch Logger → Console Theme**, placed in
`Resources/Themes` (the folder is configurable). To theme extra UI elements,
add a `ThemeableGraphic` component or implement `IThemeable`.

## Release safety

Each `CommandConsoleSettings` asset has an **Availability**:

| Availability | Runs in |
|---|---|
| `DevelopmentBuilds` (default) | Editor and Development Builds |
| `EditorOnly` | Editor only |
| `Always` | Everywhere, including release builds (a build warning is logged) |
| `Never` | Nowhere |

Outside its availability, the console destroys itself at startup (its
`consoleRoot`, the prefab root by default). When building, it is also
**stripped from the scenes**, so release players don't contain it at all. To
disable the console everywhere, tick **Disable console** in **Project Settings
→ Eldritch Logger → Console**. This adds the `ELDRITCH_CONSOLE_DISABLED`
scripting define.

**Cheats:** mark commands with `[Cheat]`, `IsCheat = true` on the attributes, or
`new CommandDescriptor(..., isCheat: true)`. They only run when the console's
`ICheatPolicy` allows it. The default policy follows **Allow Cheats** on the
settings asset. `help` shows `[cheat]` next to them. For per-player rules (for
example, only the multiplayer host), register your own policy (see
`extending.md`).
