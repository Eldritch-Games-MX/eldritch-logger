# Eldritch Logger

Eldritch Logger is a structured logging framework for Unity that brings clarity and control to your project’s logs.
It provides configurable log levels, categories, and color-coded output, all managed through ScriptableObject settings and custom inspectors.

## Features
- Structured log levels: Debug, Info, Warning, Error, Critical
- Category-based logging (UI, Gameplay, Audio, Network, etc.)
- Color-coded output for improved readability in the Console
- ScriptableObject configuration for flexible setup
- Custom inspector for easy category management
- Fluent builder API for expressive logging
- Sample scene for quick integration

## Installation
Add the package to your Unity project by editing `Packages/manifest.json`:

{
  "dependencies": {
    "com.eldritchgames.eldritchlogger": "https://github.com/eldritchgames/eldritch-logger.git"
  }
}

Unity will fetch the package directly from GitHub.

## Usage
1. Create a `LogSettings` asset via **Assets → Create → Eldritch Logger → Log Settings**.
2. Assign categories and minimum log level in the inspector.
3. Add the `LogInitializer` component to a GameObject in your scene and reference the `LogSettings` asset.
4. Use `EldritchLogger.Log(LogLevel.Info, LogCategory.UI, "Button clicked");` in your scripts.

## Samples
Import the sample scene from **Package Manager → Eldritch Logger → Samples → Logger Sample Scene** to see the logger in action.

## Tests
Unit tests are included under the `Tests` folder, using the Unity Test Framework.
