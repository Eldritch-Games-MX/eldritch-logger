# Extending

Everything here is done from your own assembly. None of it requires editing
this package.

## Write a sink

Implement `ILogSink`. `Emit` runs on the logging thread, which may not be the
main thread. Return quickly (queue slow I/O) and don't modify the entry: every
sink shares the same instance.

```csharp
public sealed class AnalyticsSink : ILogSink
{
    public string Name => "Analytics";
    public LogLevel MinimumLevel => LogLevel.Error;
    public void Emit(LogEntryDto entry) => Analytics.Queue(entry.Category, entry.Message);
}
```

Optional interfaces:

| Interface | When |
|---|---|
| `IFlushableSink` | The sink buffers entries. `Flush` runs on `log.flush`, on shutdown and on uncaught exceptions. |
| `IDisposable` | The sink owns a file, a thread or a connection. It is disposed when the logger shuts down. |
| `ISinkDiagnostics` | Shows a location and a dropped-entry count in `log.sinks` and the inspector. |
| `IShowsUnityLog` | The sink writes somewhere that already shows Unity's log (for example `Debug.Log*`). Captured Unity messages won't be sent to it, so they don't appear twice. |

A sink that writes to `Debug.*` from another thread, or later than `Emit`,
must wrap the write in `using (UnityLogForwarder.SuppressCapture()) { ... }`.
Otherwise the output is captured again as a new entry.

For a network or other slow destination, derive from `BatchingLogSink`. It
provides a background thread, batching by size and interval, a bounded queue
and `Flush`. Implement `SendBatch` and return `SendResult.Success`, `Retry`
(retried with backoff) or `Reject` (dropped and counted), plus `Location` for
diagnostics. `QueuedCount`, `SentCount`, `RetryCount` and `LastError` are
shown by the inspector and the Logger Control window; call
`ReportSendFailure(detail)` from `SendBatch` to give `LastError` a readable
reason.

For a file format, derive from `FileLogSink` and implement `Serialize` (plus
`Header` and `Footer` if the format needs them). Ordering, the background
thread and error handling come with it.

### Make it configurable in the inspector

Add a config class. It appears in **Add Sink ▾** automatically:

```csharp
[Serializable]
public sealed class AnalyticsSinkConfig : LogSinkConfig
{
    public override string DisplayName => "Analytics";
    public override ILogSink CreateSink(SinkBuildContext context) => new AnalyticsSink();
}
```

Override `Preview(LogEntryDto sample, LogSettings settings)` to show the sink's
output format in the inspector's **Output Preview**.

Or attach a sink at runtime: `ELoggerFactory.Sinks?.AddSink(sink)`.

## Write an enricher

Implement `ILogEnricher` to add properties to every entry. It runs on the
logging thread, so it must be thread-safe, and it should only add keys that
are missing, so an entry's own value wins:

```csharp
public sealed class PlayerIdEnricher : ILogEnricher
{
    public void Enrich(LogEntry entry, IDictionary<string, object> properties)
    {
        if (!properties.ContainsKey("PlayerId")) properties["PlayerId"] = Session.PlayerId;
    }
}
```

## Compose the logger in code

Turn off **Auto Initialize** in `LogSettings` and build the logger yourself:

```csharp
var logger = EldritchLoggerBuilder.FromSettings(settings)
    .AddEnricher(new PlayerIdEnricher())
    .AddSink(new AnalyticsSink())
    .Build();
LoggerBootstrap.Install(logger);
```

The builder also accepts your own filter, mapper, dispatcher and clock
(`WithFilter`, `WithMapper`, `WithDispatcher`, `WithClock`), and
`CaptureUnityLogs` sets Unity log capture without a settings asset. `ELoggerFactory.Control` is only available with
the default settings filter.

## Use a DI container

```csharp
// Zenject example
Container.Bind<ILoggerFactory>().FromInstance(new EldritchLoggerFactory(rootLogger)).AsSingle();
ELoggerFactory.SetFactory(Container.Resolve<ILoggerFactory>());
```

`ELoggerFactory.ClearFactory()` resets to the no-op `NullLogger`.

## Write a console command

For quick commands, use `[ConsoleMethod]` and `[ConsoleVariable]` (see
`runtime-console.md`). For full control, implement `IConsoleCommand` and
describe the arguments once. Binding, validation, `help` and autocomplete all
use the description. Write output to `context.Output`, not `Debug.Log`.

```csharp
using EldritchGames.EldritchLogger.Console.Arguments;
using EldritchGames.EldritchLogger.Console.Commands;
using EldritchGames.EldritchLogger.Console.Output;

[ConsoleCommand] // discovered automatically; constructor parameters are injected
public sealed class GiveCommand : IConsoleCommand
{
    private readonly Inventory inventory;

    public GiveCommand(Inventory inventory) => this.inventory = inventory;

    public CommandDescriptor Descriptor { get; } = new(
        "give", "Adds an item to the inventory.",
        parameters: new[]
        {
            ParameterSpec.Required("item", ArgumentTypes.Choice("item", () => ItemDatabase.Names)),
            ParameterSpec.Optional("amount", ArgumentTypes.PositiveInt)
        },
        flags: new[] { FlagSpec.Switch("silent") });

    public void Execute(CommandContext context)
    {
        var item = context.Arguments.Get<string>("item");
        int amount = context.Arguments.GetOrDefault("amount", 1);
        inventory.Add(item, amount);
        if (!context.Arguments.HasFlag("silent"))
            context.Output.Info($"Gave {amount} x {item}.");
    }
}
```

- Multi-frame commands implement `IAsyncConsoleCommand` and return an
  `IEnumerator`.
- Several related commands can be registered together by an `ICommandGroup`.
- For inline commands, `DelegateCommand` takes a descriptor and a lambda.

### Provide services to commands

Constructor dependencies come from the console's service provider: registry,
executor, output, view, history, settings, theme services and the cheat
policy. A command or group whose dependencies are missing is skipped, with a
warning and an entry in the Console Commands window. Register your own
services before the console starts:

```csharp
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
static void RegisterConsoleServices() =>
    ConsoleBootstrap.ConfiguringServices += services => services.Register(Inventory.Instance);
```

## Add an argument type

Implement `IArgumentType`: `TryParse` turns a token into a value (or an error
message), and `Suggest` feeds autocomplete. Use it in a `ParameterSpec` or
`FlagSpec`. `ArgumentTypes` has the built-in ones (`Int`, `PositiveInt`,
`NonNegativeInt`, `Float`, `Double`, `Bool`, `String`, `Choice`, `Enum<T>`).

## Replace the cheat policy

```csharp
public sealed class HostOnlyCheatPolicy : ICheatPolicy
{
    public bool CheatsAllowed => Network.IsHost;
}

ConsoleBootstrap.ConfiguringServices += services => services.Register<ICheatPolicy>(new HostOnlyCheatPolicy());
```

## Theme your own UI

Add a `ThemeableGraphic` component to a UI element, or implement `IThemeable`
on your own component. The console's theme applier styles it with the rest of
the console.

## Test what you wrote

Build a logger for the test with `new EldritchLoggerBuilder()`, add a
recording sink, and assert on the `LogEntryDto`s it receives. No settings asset
is needed. For commands, create a `CommandRegistry` and a `CommandExecutor`
with a recording `IConsoleOutput`, register the command, and run input strings
through `Execute`. The package's own tests in `Tests~` follow this pattern:
one fixture per class, named `<Class>Tests`.
