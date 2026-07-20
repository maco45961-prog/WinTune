function Update-SystemState {
    <#
    .SYNOPSIS
        Updates the shared system-state.json file with tweak status changes.
    .DESCRIPTION
        Reads the current system-state.json, updates the active_tweaks list, and
        writes it back. Also captures a basic system snapshot if this is the first entry.
    .PARAMETER TweakId
        The tweak ID being applied or reverted.
    .PARAMETER Action
        Either "apply" or "revert".
    .PARAMETER StateFile
        Optional. Path to system-state.json. Defaults to shared/system-state.json.
    .OUTPUTS
        None. Modifies system-state.json in place.
    .EXAMPLE
        Update-SystemState -TweakId "disable-telemetry" -Action "apply"
    .NOTES
        Core Engine Function — WinOptimizer Core
    #>
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory = $true)]
        [string]$TweakId,

        [Parameter(Mandatory = $true)]
        [ValidateSet("apply", "revert")]
        [string]$Action,

        [Parameter(Mandatory = $false)]
        [string]$StateFile
    )

    if (-not $StateFile) {
        $StateFile = Join-Path (Join-Path (Join-Path $PSScriptRoot "..") "shared") "system-state.json"
    }

    # Load or initialize state
    if (Test-Path $StateFile) {
        $state = Get-Content -Path $StateFile -Raw | ConvertFrom-Json
    }
    else {
        $state = [PSCustomObject]@{
            version = "1.0.0"
            last_updated = $null
            active_tweaks = @()
            tweak_history = @()
            profile_applied = $null
            system_snapshot = [PSCustomObject]@{
                hostname = $env:COMPUTERNAME
                os_version = [System.Environment]::OSVersion.VersionString
                total_ram_gb = [math]::Round((Get-CimInstance Win32_ComputerSystem).TotalPhysicalMemory / 1GB, 2)
                cpu_name = (Get-CimInstance Win32_Processor).Name
                vanguard_installed = $false
                vbs_enabled = $false
            }
        }
    }

    $now = (Get-Date).ToString("o")
    $state.last_updated = $now

    # Capture system snapshot if first time
    if (-not $state.system_snapshot.total_ram_gb -or $state.system_snapshot.total_ram_gb -eq 0) {
        $state.system_snapshot.total_ram_gb = [math]::Round((Get-CimInstance Win32_ComputerSystem).TotalPhysicalMemory / 1GB, 2)
        $state.system_snapshot.cpu_name = (Get-CimInstance Win32_Processor).Name
        $state.system_snapshot.os_version = [System.Environment]::OSVersion.VersionString
    }

    . (Join-Path $PSScriptRoot "Test-VanguardInstalled.ps1")
    $state.system_snapshot.vanguard_installed = Test-VanguardInstalled

    try {
        $vbs = (Get-CimInstance -ClassName Win32_DeviceGuard -Namespace root\Microsoft\Windows\DeviceGuard -ErrorAction SilentlyContinue).VirtualizationBasedSecurityStatus
        $state.system_snapshot.vbs_enabled = ($vbs -eq 2)
    }
    catch {
        $state.system_snapshot.vbs_enabled = $false
    }

    # Update active_tweaks
    $activeList = @($state.active_tweaks)

    if ($Action -eq "apply") {
        if ($TweakId -notin $activeList) {
            $activeList += $TweakId
        }
    }
    elseif ($Action -eq "revert") {
        $activeList = $activeList | Where-Object { $_ -ne $TweakId }
    }

    $state.active_tweaks = $activeList

    # Add to history
    $historyEntry = [PSCustomObject]@{
        tweak_id = $TweakId
        action = $Action
        timestamp = $now
    }
    $historyArray = @($state.tweak_history)
    $historyArray += $historyEntry
    # Keep last 500 history entries
    if ($historyArray.Count -gt 500) {
        $historyArray = $historyArray[-500..-1]
    }
    $state.tweak_history = $historyArray

    # Save
    $state | ConvertTo-Json -Depth 10 | Set-Content -Path $StateFile -Encoding UTF8
}
