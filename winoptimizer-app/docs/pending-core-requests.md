# Pending Core Engine Requests

Features needed from `winoptimizer-core` that are not yet implemented. The app uses stubs for now.

## 1. `Get-Profiles` — dedicated function

**Status:** Stub (reading JSON files directly in CoreClient)

The core module exports `Apply-Profile` but not `Get-Profiles`. The app currently reads profile JSON files directly from the `profiles/` directory. A proper `Get-Profiles` function in the core would be cleaner.

**Requested signature:**
```powershell
Get-Profiles [-Name <string>] [-Category <string>]
```

## 2. `Get-AvailableTweaks` — 3-arg Join-Path fix

**Status:** Fixed in core (PS 5.1 compatibility)

The core used `Join-Path $PSScriptRoot ".." "tweaks"` which fails on PowerShell 5.1. Fixed to nested `Join-Path` calls. This should be the standard pattern in all core scripts.

## 3. Per-tweak undo from profile context

**Status:** Working via `Undo-Tweak`

When a profile is applied, the app logs each tweak individually so it can undo them one by one. This works but the core could expose a `Undo-Profile` function that takes a profile name and reverts all tweaks in it, checking which are actually active.

**Requested signature:**
```powershell
Undo-Profile -Name <string>
```

## 4. Session-aware state tracking

**Status:** App-side only

The app tracks its own session log in memory. The core's `system-state.json` tracks global state but not per-session state. If the app crashes, session data is lost. Consider adding session persistence to the core.

## 5. `Export-SessionLog` / `Import-SessionLog`

**Status:** Not implemented

For the Benchmark app to compare before/after, it would be useful to export the session log in a structured format. Currently the app writes to `logs/winoptimizer-YYYYMMDD.jsonl` which other apps can read, but a dedicated function would be cleaner.

## 6. Dry-run mode for `Invoke-Tweak`

**Status:** Not implemented

The app would benefit from a `-WhatIf` / dry-run mode that shows what would change without actually applying. PowerShell's `SupportsShouldProcess` already supports `-WhatIf`, but it needs to be wired through properly in the core.
