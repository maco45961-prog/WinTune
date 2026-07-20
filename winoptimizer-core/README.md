# WinOptimizer Core

Core Engine for the WinOptimizer ecosystem. A PowerShell library that performs system optimization and exposes a clean API for client applications.

**No UI. No telemetry. No paid dependencies. MIT licensed.**

## What This Is

A headless engine that any Windows optimization app can consume:
- **Optimizer UI** — reads tweaks via `Get-AvailableTweaks`, applies via `Invoke-Tweak`
- **Benchmark Tool** — reads `system-state.json` for before/after comparison
- **Net Tools** — applies network tweaks for latency testing
- **Game Mode Daemon** — reads active tweaks, monitors Vanguard compatibility
- **Setup Wizard** — applies profiles via `Apply-Profile`

## Quick Start

```powershell
# Import all engine functions
. .\engine\Get-AvailableTweaks.ps1
. .\engine\Invoke-Tweak.ps1
. .\engine\Undo-Tweak.ps1
. .\engine\Apply-Profile.ps1

# List all tweaks
Get-AvailableTweaks

# Apply a single tweak
Invoke-Tweak -Id "disable-telemetry"

# Apply a profile
Apply-Profile -Name "gaming-competitivo-lowend"

# Revert
Undo-Tweak -Id "disable-telemetry"
```

## Structure

```
winoptimizer-core/
├── engine/                    # Core functions (source these)
│   ├── Get-AvailableTweaks.ps1
│   ├── Invoke-Tweak.ps1
│   ├── Undo-Tweak.ps1
│   ├── Apply-Profile.ps1
│   ├── New-SystemBackup.ps1
│   ├── Test-VanguardInstalled.ps1
│   ├── Update-SystemState.ps1
│   └── Write-TweakLog.ps1
├── tweaks/                    # Tweak definitions (auto-discovered)
│   ├── privacy/
│   ├── services/
│   ├── network/
│   ├── gaming/
│   ├── appearance/
│   ├── startmenu-taskbar/
│   └── explorer/
├── profiles/                  # Profile definitions
├── shared/
│   ├── schemas/               # JSON schemas
│   └── system-state.json      # Shared state file
├── logs/                      # Auto-generated logs
├── backups/                   # Auto-generated backups
├── docs/                      # Documentation
├── tests/                     # Pester + PSScriptAnalyzer tests
└── README.md
```

## Adding a Tweak

1. Create a JSON file in `tweaks/<category>/` following `shared/schemas/tweak.schema.json`
2. Done — `Get-AvailableTweaks` will discover it automatically

## Testing

```powershell
# Run all tests
Invoke-Pester .\tests\ -Output Detailed

# Run PSScriptAnalyzer only
Invoke-ScriptAnalyzer -Path .\engine\ -Recurse -Severity Error,Warning
```

## Shared State

`shared/system-state.json` is the contract between all ecosystem apps. See `shared/schemas/system-state.schema.json` for the full schema.

## License

MIT
