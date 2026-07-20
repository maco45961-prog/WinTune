function New-SystemBackup {
    <#
    .SYNOPSIS
        Creates a System Restore Point and exports registry/service state before applying a tweak.
    .DESCRIPTION
        Performs two actions: (1) Creates a Windows System Restore Point, and (2) exports
        the current state of registry keys and services that the tweak will modify, saving
        them to a backup file for reliable revert.
    .PARAMETER TweakId
        The tweak ID being applied. Used to name the backup and determine which keys to snapshot.
    .PARAMETER BackupDir
        Optional. Custom backup directory. Defaults to winoptimizer-core/backups/.
    .OUTPUTS
        PSCustomObject — Backup result with path and restore point status.
    .EXAMPLE
        New-SystemBackup -TweakId "disable-telemetry"
    .NOTES
        Core Engine Function — WinOptimizer Core
    #>
    [CmdletBinding(SupportsShouldProcess)]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', '', Justification = 'Variables are used later in the function body; PSScriptAnalyzer false positive')]
    param(
        [Parameter(Mandatory = $true)]
        [string]$TweakId,

        [Parameter(Mandatory = $false)]
        [string]$BackupDir
    )

    $ErrorActionPreference = "Stop"
    $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"

    if (-not $BackupDir) {
        $BackupDir = Join-Path (Join-Path $PSScriptRoot "..") "backups"
    }

    if (-not (Test-Path $BackupDir)) {
        New-Item -ItemType Directory -Path $BackupDir -Force | Out-Null
    }

    $backupFile = Join-Path $BackupDir "$TweakId-$timestamp.json"

    # 1. Create System Restore Point
    $restorePointCreated = $false
    try {
        Enable-ComputerRestore -Drive "C:\" -ErrorAction SilentlyContinue
        Checkpoint-Computer -Description "WinOptimizer backup before tweak: $TweakId" -RestorePointType MODIFY_SETTINGS
        $restorePointCreated = $true
    }
    catch {
        Write-Warning "Could not create System Restore Point: $_. Continuing with registry snapshot."
    }

    # 2. Export registry/service state snapshot
    . (Join-Path $PSScriptRoot "Get-AvailableTweaks.ps1")
    $tweak = Get-AvailableTweaks -Id $TweakId
    $snapshot = @{
        timestamp  = $timestamp
        tweak_id   = $TweakId
        restore_point_created = $restorePointCreated
        registry_snapshot = @()
        services_snapshot = @()
    }

    if ($tweak -and $tweak.Count -gt 0) {
        $applyCmd = $tweak[0].commands.apply

        # Extract registry paths from apply command
        $regPattern = 'HKLM:\\[^\s' + "'" + '"]+|HKCU:\\[^\s' + "'" + '"]+'
        $regPaths = [regex]::Matches($applyCmd, $regPattern) | ForEach-Object { $_.Value } | Sort-Object -Unique
        foreach ($regPath in $regPaths) {
            $regData = $null
            try {
                if (Test-Path $regPath) {
                    $regData = Get-ItemProperty -Path $regPath -ErrorAction SilentlyContinue
                }
            }
            catch { }
            $snapshot.registry_snapshot += @{
                path   = $regPath
                values = $regData
            }
        }

        # Extract service names from apply command
        $svcPattern = "'([A-Za-z0-9_]+)'\s*-[A-Za-z]*\s*(?:Name|StartupType)"
        $svcNames = [regex]::Matches($applyCmd, $svcPattern) | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique
        foreach ($svc in $svcNames) {
            $svcInfo = Get-Service -Name $svc -ErrorAction SilentlyContinue
            if ($svcInfo) {
                $filterStr = "Name='" + $svc + "'"
                $svcWmi = Get-WmiObject Win32_Service -Filter $filterStr -ErrorAction SilentlyContinue
                $snapshot.services_snapshot += @{
                    name          = $svc
                    status        = $svcInfo.Status.ToString()
                    startup_type  = $svcWmi.StartMode
                }
            }
        }
    }

    # Save snapshot
    $snapshot | ConvertTo-Json -Depth 5 | Set-Content -Path $backupFile -Encoding UTF8

    return [PSCustomObject]@{
        Status  = "success"
        BackupFile = $backupFile
        RestorePointCreated = $restorePointCreated
        Message = "Backup created. File: $backupFile"
    }
}

function Restore-SystemBackup {
    <#
    .SYNOPSIS
        Restores system state from a backup file created by New-SystemBackup.
    .DESCRIPTION
        Reads the backup snapshot and attempts to restore registry values and
        service states. Optionally triggers a full system restore.
    .PARAMETER BackupFile
        Path to the backup JSON file to restore from.
    .OUTPUTS
        PSCustomObject — Restore result with status and details.
    .EXAMPLE
        Restore-SystemBackup -BackupFile "C:\backups\disable-telemetry-20260720-143000.json"
    .NOTES
        Core Engine Function — WinOptimizer Core
    #>
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory = $true)]
        [string]$BackupFile
    )

    $ErrorActionPreference = "Stop"

    if (-not (Test-Path $BackupFile)) {
        return [PSCustomObject]@{
            Status  = "error"
            Message = "Backup file not found: $BackupFile"
        }
    }

    $snapshot = Get-Content -Path $BackupFile -Raw | ConvertFrom-Json

    if ($PSCmdlet.ShouldProcess("Backup $($snapshot.tweak_id)", "Restore system state")) {
        $restoredRegistry = 0
        $restoredServices = 0
        $errors = @()

        # Restore registry
        foreach ($entry in $snapshot.registry_snapshot) {
            try {
                if ($entry.values) {
                    $props = @{}
                    $entry.values.PSObject.Properties | Where-Object { $_.Name -notin @("PSPath","PSParentPath","PSChildName","PSDrive","PSProvider") } | ForEach-Object {
                        $props[$_.Name] = $_.Value
                    }
                    foreach ($key in $props.Keys) {
                        Set-ItemProperty -Path $entry.path -Name $key -Value $props[$key] -Type DWord -ErrorAction SilentlyContinue
                        $restoredRegistry++
                    }
                }
            }
            catch {
                $errors += "Registry restore failed for $($entry.path): $_"
            }
        }

        # Restore services
        foreach ($svc in $snapshot.services_snapshot) {
            try {
                $startupMap = @{
                    "Auto"     = "Automatic"
                    "Manual"   = "Manual"
                    "Disabled" = "Disabled"
                }
                $startType = if ($startupMap.ContainsKey($svc.startup_type)) { $startupMap[$svc.startup_type] } else { "Manual" }
                Set-Service -Name $svc.name -StartupType $startType -ErrorAction SilentlyContinue
                $restoredServices++
            }
            catch {
                $errors += "Service restore failed for $($svc.name): $_"
            }
        }

        . (Join-Path $PSScriptRoot "Write-TweakLog.ps1")
        $logStatus = if ($errors.Count -eq 0) { "success" } else { "partial" }
        Write-TweakLog -Action "restore" -TweakId $snapshot.tweak_id -Status $logStatus -Message "Restored $restoredRegistry registry entries, $restoredServices services. Errors: $($errors.Count)"

        return [PSCustomObject]@{
            Status  = if ($errors.Count -eq 0) { "success" } else { "partial" }
            TweakId = $snapshot.tweak_id
            RestoredRegistry = $restoredRegistry
            RestoredServices = $restoredServices
            Errors  = $errors
            Message = "Restore complete. Registry: $restoredRegistry, Services: $restoredServices, Errors: $($errors.Count)"
        }
    }
}
