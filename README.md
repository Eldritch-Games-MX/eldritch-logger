# Eldritch Logger

Eldritch Logger is a structured logging framework for Unity that brings clarity and control to your project’s logs.
It provides configurable log levels, categories, structured metadata, and multiple exporters (JSON, XML, Text, Unity Console), all managed through ScriptableObject settings.

## Features
- Structured log levels: Debug, Info, Warning, Error, Critical
- Category-based logging (UI, Gameplay, Audio, Network, etc.)
- Color-coded output in the Unity Console
- ScriptableObject configuration (LogSettings) for flexible setup
- Fluent builder API for expressive logging
- Component + GameObject context (Component@GameObject)
- Unified event logging (WithEvent) for both C# delegates and UnityEvents
- Exception logging with type and message
- Exporters: JSON, XML, Text file, Unity Console
- Configurable export directory and file name
- Automatic cleanup of previous session logs

## Installation
Add the package to your Unity project by editing `Packages/manifest.json`:

{
  "dependencies": {
    "com.eldritchgames.eldritchlogger": "https://github.com/eldritchgames/eldritch-logger.git"
  }
}

Unity will fetch the package directly from GitHub.

## Setup
1. Create a LogSettings asset via Assets → Create → Eldritch Logger → Log Settings.
2. Place the asset in a Resources folder (e.g., Assets/Resources/LogSettings.asset).
3. Configure in the inspector:
	- Minimum log level
	- Enabled categories
	- Export formats (JSON, XML, Text)
	- Export directory and file name
	- Console options (color coding, stack trace suppression)

At runtime, a boostrapper automatically initializes the logger before the first scene loads.
It also disposes exporters cleanly when the application quits.

## Usage

Add this using directive to any script that calls the logger:
```csharp
using EldritchGames.EldritchLogger.Core;
```

### Direct Logging
```csharp
ELogger.Log(LogLevel.Info, LogCategory.UI, "Button clicked");
ELogger.Log(LogLevel.Error, LogCategory.Network, "Connection lost");
```

### Fluent Builder
```csharp
ELogger.AtInfo(LogCategory.Gameplay)
    .AddKeyValue("ItemId", 42)
    .Log("Player picked up potion");

ELogger.AtError(LogCategory.AI)
    .WithException(new InvalidOperationException("Boom!"))
    .Log("AI failed to pathfind");

ELogger.AtWarning(LogCategory.Network)
    .WithEvent(OnPlayerDeath, nameof(OnPlayerDeath))
    .Log("Player lost connection");

ELogger.AtInfo(LogCategory.Gameplay)
    .WithComponent(this)
    .Log("Contextual log with MonoBehaviour");
```

### Advanced: Direct Instance Access
`ELogger` is a static facade over `EldritchLogger.Instance`. For dependency injection or test scenarios where you need the instance directly:
```csharp
// Inject via constructor or field
IEldritchLogger logger = EldritchLogger.Instance;
```

###  Eldritch Logger - Fluent Builder Quick Reference

| Method                  | Purpose                                                                 |
|--------------------------|-------------------------------------------------------------------------|
| `.AddKeyValue(key, val)` | Attach structured metadata (e.g., `"Score": 9001`)                      |
| `.WithException(ex)`     | Attach an exception (type + message)                                    |
| `.WithEvent(evt, name)`  | Attach event context (C# delegate or UnityEvent)                        |
| `.WithComponent(comp)`   | Attach component + GameObject context (`Component@GameObject`)          |
| `.Category(category)`    | Override the category for the log entry                                 |
| `.Log("message")`        | Final call to dispatch the log entry                                    |

### Exporters
JSON, XML, Text → written to files in the configured export directory.

Unity Console Exporter → always active, writes formatted logs to the Unity Console.

## Samples
Import the sample scene from **Package Manager → Eldritch Logger → Samples → Logger Sample Scene** to see the logger in action.
