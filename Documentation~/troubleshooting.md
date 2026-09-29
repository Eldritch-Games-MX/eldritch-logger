# Troubleshooting

## Nothing prints

- Check that `LogSettings.asset` is at `Assets/Resources/LogSettings.asset` (an
  error is logged otherwise) and that **Auto Initialize** is on.
- Check the minimum level, the category toggles, and each sink's minimum
  level. An entry needs to pass the settings *and* at least one sink.
  `log.level` in the console warns when no sink accepts the level you set.
- Get MonoBehaviour loggers in `Awake()`, not in a field initializer (analyzer
  `ELG003`).

## Custom category logs don't appear

- The category must be registered and enabled in `LogSettings`. Names are
  case-insensitive.

## No log files

- Add a file sink under **Sinks**. Files go to `Application.persistentDataPath`
  by default. The sink's **Open Folder** button, or `log.open` in the console,
  goes there.
- Internal problems (a sink that throws, a full file queue, a locked file) are
  reported in the Unity Console with an `[EldritchLogger]` prefix.

## A property appears in the message but not after it

- That's expected: filled template holes are shown once, in the message. JSON,
  XML and HTTP output keep them as properties.

## Every entry logged during a coroutine carries the wrong scope

- A scope was left open across a `yield`. Close it before yielding (analyzer
  `ELG005`).

## Console command not found

- Open **Tools → Eldritch Logger → Console Commands**. It lists skipped types
  and members with the reason (a missing dependency, an unsupported parameter
  type), and name conflicts.

## The console does not appear

- Check the settings asset's **Availability** and the **Disable console**
  toggle in **Project Settings → Eldritch Logger → Console**. Release builds
  strip the console unless availability is `Always`.

## "'x' is a cheat and cheats are disabled"

- Enable **Allow Cheats** on the console settings, or check your custom
  `ICheatPolicy`.
