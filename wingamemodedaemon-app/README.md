# WinGameModeDaemon

Background system tray daemon that automatically applies and reverts gaming profiles when games are detected.

## What it does

1. Monitors Windows process creation/termination events (WMI, no polling)
2. When a configured game executable is detected, calls `Apply-Profile` from winoptimizer-core
3. When the game closes, reverts only the tweaks the daemon applied (not user's manual tweaks)
4. Shows system tray icon with status (inactive / gaming active / reverting / disabled)
5. Sends Windows toast notifications on profile changes

## Requirements

- Windows 10/11
- .NET 8.0 Runtime (Desktop)
- winoptimizer-core in a sibling directory or set via `WINOPTIMIZER_CORE_PATH`
- Admin privileges (required for core engine functions)

## Build

```bash
dotnet build
dotnet publish -c Release
```

## Usage

```bash
# Run directly
dotnet run

# Or publish and run the exe
dotnet publish -c Release -r win-x64 --self-contained
.\bin\Release\net8.0-windows\win-x64\publish\WinGameModeDaemon.exe
```

## Configuration

Edit `config/watched-games.json` or right-click the tray icon > "Configure watched games...":

```json
{
  "executables": [
    "VALORANT.exe",
    "VALORANT-Win64-Shipping.exe"
  ],
  "profileToApply": "gaming-competitivo-lowend",
  "enabled": true,
  "notificationsEnabled": true,
  "startWithWindows": false
}
```

The "Auto-detect installed games" button scans common Steam/Epic/Riot install paths.

## Tray Icon Menu

| Color | State |
|-------|-------|
| Gray  | Inactive (no game running) |
| Green | Gaming profile active |
| Yellow | Reverting profile |
| Red | Daemon disabled |

- **Disable/Enable** — toggles the daemon (reverts active profile when disabling)
- **Configure watched games** — opens the config editor
- **Open config file** — opens watched-games.json in default editor
- **Open log** — opens the most recent log file
- **Exit** — reverts any active profile and shuts down

## Design Principles

- **Zero optimization logic** — this daemon only calls winoptimizer-core functions
- **WMI events, not polling** — process detection uses `Win32_ProcessStartTrace`/`Win32_ProcessStopTrace`
- **Selective revert** — only reverts tweaks it applied, never touches user's manual changes
- **Vanguard safety** — checks `Test-VanguardInstalled` before applying profiles
- **Graceful shutdown** — reverts profile on game close, Windows shutdown, logoff, Ctrl+C, or daemon exit

## Project Structure

```
/wingamemodedaemon-app
  /detection
    ProcessWatcher.cs       -> WMI event-based process start/stop detection
  /integration
    CoreClient.cs           -> Apply-Profile / Undo-Tweak / Vanguard checks via PowerShell
  /tray
    TrayIconManager.cs      -> System tray icon, context menu, dark theme renderer
  /config
    ConfigManager.cs        -> Load/save watched-games.json
    WatchedGamesConfig.cs   -> Config data model
    watched-games.json      -> Default watched games config
  /i18n
    Localizer.cs            -> Spanish/English string provider
  GameModeDaemon.cs         -> Central orchestrator (ties everything together)
  ConfigForm.cs             -> Dark-themed config editor form
  Program.cs                -> Entry point with single-instance mutex
  WinGameModeDaemon.csproj  -> .NET 8.0 WinForms project
```

## License

MIT
