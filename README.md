# Usage4Claude-Windows

A Windows system tray application for monitoring Claude AI usage quotas.

## Features

- **System Tray Icon** - Shows usage percentage with color-coded progress indicator
- **Popup Window** - Click the tray icon to see detailed usage breakdown
- **Multiple Accounts** - Manage and switch between multiple Claude accounts
- **Browser Login** - Login via WebView2 for easy session key extraction
- **Smart Refresh** - Adaptive polling that adjusts based on usage patterns
- **Toast Notifications** - Get alerted when approaching usage limits
- **Auto-Start** - Launch automatically at Windows login
- **Settings** - Customizable display, refresh, and notification options

## Requirements

- Windows 10 (2004) or later
- .NET 8.0 Runtime (for framework-dependent build)
- WebView2 Runtime (for browser login feature)

## Installation

### Framework-Dependent (smaller download, requires .NET 8 Runtime)
1. Download `Usage4Claude-vX.X.X-win-x64.zip` from [Releases](https://github.com/raravel/Usage4Claude-Windows/releases)
2. Extract and run `Usage4Claude.exe`

### Self-Contained (larger download, no runtime needed)
1. Download `Usage4Claude-vX.X.X-win-x64-self-contained.zip` from [Releases](https://github.com/raravel/Usage4Claude-Windows/releases)
2. Extract and run `Usage4Claude.exe`

## Building from Source

```bash
dotnet restore src/Usage4Claude/Usage4Claude.csproj
dotnet build src/Usage4Claude/Usage4Claude.csproj
```

### Publishing

```bash
# Framework-dependent single file
dotnet publish src/Usage4Claude/Usage4Claude.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true

# Self-contained single file
dotnet publish src/Usage4Claude/Usage4Claude.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true
```

## License

Copyright 2025 f-is-h. All rights reserved.
