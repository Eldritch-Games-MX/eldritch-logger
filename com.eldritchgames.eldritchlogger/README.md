# Eldritch Logger

Eldritch Logger is a structured logging framework for Unity.
It adds configurable log levels, categories, and color-coded output to make debugging clearer and more efficient.

## Installation
This package can be added to your project by editing `Packages/manifest.json`:

{
  "dependencies": {
    "com.eldritchgames.eldritchlogger": "https://github.com/eldritchgames/eldritch-logger.git"
  }
}

## Usage
1. Create a LogSettings asset via Assets → Create → Eldritch Logger → Log Settings.
2. Configure categories and minimum log level in the inspector.
3. Add the LogInitializer component to a GameObject and assign the LogSettings asset.
4. Log messages with:
   EldritchLogger.Log(LogLevel.Info, LogCategory.UI, "Button clicked");

## Samples
A sample scene is available under Package Manager → Eldritch Logger → Samples → Logger Sample Scene.