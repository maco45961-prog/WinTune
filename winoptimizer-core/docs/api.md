# WinOptimizer Core — API Reference

All functions are PowerShell advanced functions in `engine/`. Source them with dot-sourcing or import via a module manifest.

## Discovery

### `Get-AvailableTweaks`

Returns the complete catalog of available tweaks.

```powershell
. .\engine\Get-AvailableTweaks.ps1
Get-AvailableTweaks                           # All tweaks
Get-AvailableTweaks -Category "privacy"      # Filter by category
Get-AvailableTweaks -Id "disable-telemetry"  # Single tweak by ID
```

**Parameters:**
| Parameter | Type | Required | Description |
|---|---|---|---|
| `-Category` | String | No | Filter: privacy, services, network, gaming, appearance, startmenu-taskbar, explorer |
| `-Id` | String | No | Exact tweak ID |

**Returns:** `PSCustomObject[]`

---

## Apply / Revert

### `Invoke-Tweak`

Applies a single tweak by ID.

```powershell
. .\engine\Invoke-Tweak.ps1
Invoke-Tweak -Id "disable-telemetry"
Invoke-Tweak -Id "disable-nagle-algorithm" -Force  # Bypass Vanguard check
```

**Parameters:**
| Parameter | Type | Required | Description |
|---|---|---|---|
| `-Id` | String | Yes | Tweak ID |
| `-Force` | Switch | No | Bypass Vanguard compatibility check |
| `-SkipBackup` | Switch | No | Skip auto-backup (not recommended) |

**Returns:** `PSCustomObject` — `{ Status, TweakId, Name, Message, RequiresRestart }`

### `Undo-Tweak`

Reverts a single tweak by ID.

```powershell
. .\engine\Undo-Tweak.ps1
Undo-Tweak -Id "disable-telemetry"
```

**Parameters:**
| Parameter | Type | Required | Description |
|---|---|---|---|
| `-Id` | String | Yes | Tweak ID |

**Returns:** `PSCustomObject` — `{ Status, TweakId, Name, Message }`

---

## Profiles

### `Apply-Profile`

Applies a profile (collection of tweaks).

```powershell
. .\engine\Apply-Profile.ps1
Apply-Profile -Name "debloat-estandar"
Apply-Profile -Name "gaming-competitivo-lowend" -Force
```

**Parameters:**
| Parameter | Type | Required | Description |
|---|---|---|---|
| `-Name` | String | Yes | Profile name (filename without .json) |
| `-Force` | Switch | No | Bypass Vanguard checks for all tweaks |

**Returns:** `PSCustomObject` — `{ Status, Profile, Applied, Errors, Total, Results, Message }`

### `Get-Profiles`

Returns all available profiles. (Implementation: reads `profiles/*.json`)

```powershell
Get-ChildItem .\profiles\*.json | ForEach-Object { Get-Content $_ -Raw | ConvertFrom-Json }
```

---

## Backup / Restore

### `New-SystemBackup`

Creates a System Restore Point and exports registry/service state snapshot.

```powershell
. .\engine\New-SystemBackup.ps1
New-SystemBackup -TweakId "disable-telemetry"
New-SystemBackup -TweakId "my-tweak" -BackupDir "D:\custom-backups"
```

**Parameters:**
| Parameter | Type | Required | Description |
|---|---|---|---|
| `-TweakId` | String | Yes | Tweak ID (used for naming) |
| `-BackupDir` | String | No | Custom backup directory |

**Returns:** `PSCustomObject` — `{ Status, BackupFile, RestorePointCreated, Message }`

### `Restore-SystemBackup`

Restores system state from a backup JSON file.

```powershell
. .\engine\New-SystemBackup.ps1
Restore-SystemBackup -BackupFile "C:\backups\disable-telemetry-20260720-143000.json"
```

**Parameters:**
| Parameter | Type | Required | Description |
|---|---|---|---|
| `-BackupFile` | String | Yes | Path to backup JSON |

**Returns:** `PSCustomObject` — `{ Status, TweakId, RestoredRegistry, RestoredServices, Errors, Message }`

---

## Security / Compatibility

### `Test-VanguardInstalled`

Detects if Riot Vanguard (VGC) anti-cheat is installed.

```powershell
. .\engine\Test-VanguardInstalled.ps1
Test-VanguardInstalled  # Returns $true or $false
```

### `Test-VBSCompatibleTweak`

Tests if a specific tweak is compatible with Vanguard/VBS.

```powershell
. .\engine\Test-VanguardInstalled.ps1
Test-VBSCompatibleTweak -Id "disable-telemetry"
# Returns: { Id, Compatible, Risk }
```

---

## Shared State

### `Update-SystemState`

Updates `shared/system-state.json` with tweak status changes. Called automatically by `Invoke-Tweak` and `Undo-Tweak`.

```powershell
. .\engine\Update-SystemState.ps1
Update-SystemState -TweakId "disable-telemetry" -Action "apply"
```

### `Write-TweakLog`

Writes structured logs. Called automatically by engine functions.

```powershell
. .\engine\Write-TweakLog.ps1
Write-TweakLog -Action "apply" -TweakId "disable-telemetry" -Status "success" -Message "Applied"
```

---

## Client Integration Guide

Any client app (Optimizer UI, Benchmark, NetTools, Game Mode Daemon) should:

1. **Dot-source** the engine scripts: `. .\engine\*.ps1`
2. **Discover tweaks** with `Get-AvailableTweaks` — never hardcode tweak lists.
3. **Apply/revert** with `Invoke-Tweak` / `Undo-Tweak` — backup is automatic.
4. **Read state** from `shared/system-state.json` — this is the shared contract.
5. **Check logs** from `logs/winoptimizer-YYYYMMDD.jsonl` — structured JSON Lines.
6. **Respect** `compatible_with_vanguard` and `risk` flags in tweak definitions.
