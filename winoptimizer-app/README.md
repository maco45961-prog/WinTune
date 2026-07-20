# WinOptimizer App

Desktop UI for the WinOptimizer ecosystem. Built with WPF (.NET 8) and dark mode by default.

## Requirements

- Windows 10/11
- .NET 8 SDK (for building)
- PowerShell 5.1+ (runtime)
- `winoptimizer-core` accessible at `../winoptimizer-core` or `D:\win\winoptimizer-core`

## Build & Run

```bash
# From the winoptimizer-app directory
dotnet build
dotnet run
```

## Architecture

```
winoptimizer-app/
├── WinOptimizer.App.csproj     ← .NET 8 WPF project
├── App.xaml / App.xaml.cs      ← Entry point, dark mode theme
├── ui/
│   ├── MainWindow.xaml         ← Sidebar navigation + content area
│   ├── views/
│   │   ├── HomeView            ← Profile selection cards
│   │   ├── CustomView          ← Full tweak catalog with search
│   │   ├── ConfirmDialog       ← Apply confirmation modal
│   │   └── LogView             ← Session log + undo
│   └── i18n/
│       ├── es/Resources.xaml   ← Spanish strings
│       └── en/Resources.xaml   ← English strings
├── integration/
│   └── CoreClient.cs           ← Thin passthrough to core PowerShell
└── docs/
    └── pending-core-requests.md
```

## Data Flow

```
UI (WPF) → CoreClient.cs → PowerShell Core Engine → Registry/Services
                ↓
        system-state.json (shared state)
        logs/*.jsonl (session history)
```

The UI never touches the registry or services directly. All mutations go through the core engine via `CoreClient`.

## Features

- **Profile selection**: 4 cards (Debloat Std/Agresivo, Gaming Low-End, Custom)
- **Tweak catalog**: Full catalog with search, risk badges, category grouping
- **Confirmation dialog**: Shows what will change before applying
- **Session log**: Timestamped log of all actions with per-tweak undo
- **Vanguard detection**: Warning banner when Vanguard is installed
- **Dark mode**: Default theme, OLED-friendly
- **i18n**: Spanish (default) and English

## Core Engine Functions Used

| Function | Purpose |
|---|---|
| `Get-AvailableTweaks` | Discover all tweaks |
| `Invoke-Tweak` | Apply a single tweak |
| `Undo-Tweak` | Revert a single tweak |
| `Apply-Profile` | Apply a profile |
| `New-SystemBackup` | Create restore point + snapshot |
| `Test-VanguardInstalled` | Detect Vanguard anti-cheat |

## License

MIT
