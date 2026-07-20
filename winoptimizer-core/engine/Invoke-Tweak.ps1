function Invoke-Tweak {
    <#
    .SYNOPSIS
        Applies a single tweak by its ID.
    .DESCRIPTION
        Loads the tweak definition, validates Vanguard compatibility if needed,
        creates a backup entry, applies the tweak commands, and updates system-state.json.
    .PARAMETER Id
        The tweak ID to apply (e.g., "disable-telemetry").
    .PARAMETER Force
        Bypass Vanguard compatibility check. Only use when user explicitly confirms.
    .PARAMETER SkipBackup
        Skip automatic backup before applying. Not recommended.
    .OUTPUTS
        PSCustomObject — Result with status, tweak id, and message.
    .EXAMPLE
        Invoke-Tweak -Id "disable-telemetry"
    .EXAMPLE
        Invoke-Tweak -Id "disable-nagle-algorithm" -Force
    .NOTES
        Core Engine Function — WinOptimizer Core
    #>
[CmdletBinding(SupportsShouldProcess)]
[Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSAvoidUsingInvokeExpression', '', Justification = 'Dynamic execution of tweak commands stored as strings in JSON definitions')]
param(
        [Parameter(Mandatory = $true)]
        [string]$Id,

        [Parameter(Mandatory = $false)]
        [switch]$Force,

        [Parameter(Mandatory = $false)]
        [switch]$SkipBackup
    )

    $ErrorActionPreference = "Stop"

    # Load tweak definition
    . (Join-Path $PSScriptRoot "Get-AvailableTweaks.ps1")
    $tweak = Get-AvailableTweaks -Id $Id

    if (-not $tweak -or $tweak.Count -eq 0) {
        return [PSCustomObject]@{
            Status  = "error"
            TweakId = $Id
            Message = "Tweak '$Id' not found."
        }
    }
    $tweak = $tweak[0]

    # Vanguard compatibility check
    if (-not $tweak.compatible_with_vanguard -and -not $Force) {
        . (Join-Path $PSScriptRoot "Test-VanguardInstalled.ps1")
        $vanguardPresent = Test-VanguardInstalled

        if ($vanguardPresent) {
            return [PSCustomObject]@{
                Status  = "blocked"
                TweakId = $Id
                Message = "Tweak '$Id' is not compatible with Vanguard anti-cheat. Use -Force to override."
                Risk    = $tweak.risk
            }
        }
    }

    # Pre-tweak backup (registry snapshot)
    if (-not $SkipBackup) {
        . (Join-Path $PSScriptRoot "New-SystemBackup.ps1")
        New-SystemBackup -TweakId $Id | Out-Null
    }

    # Apply
    if ($PSCmdlet.ShouldProcess($tweak.name, "Apply tweak")) {
        try {
            Invoke-Expression $tweak.commands.apply

            # Update system-state.json
            . (Join-Path $PSScriptRoot "Update-SystemState.ps1")
            Update-SystemState -TweakId $Id -Action "apply"

            # Logging
            . (Join-Path $PSScriptRoot "Write-TweakLog.ps1")
            Write-TweakLog -Action "apply" -TweakId $Id -Status "success" -Message "Tweak '$($tweak.name)' applied successfully."

            $restartNote = ""
            if ($tweak.requires_restart) {
                $restartNote = " A restart is required for changes to take full effect."
            }

            return [PSCustomObject]@{
                Status  = "success"
                TweakId = $Id
                Name    = $tweak.name
                Message = "Tweak applied successfully.$restartNote"
                RequiresRestart = $tweak.requires_restart
            }
        }
        catch {
            . (Join-Path $PSScriptRoot "Write-TweakLog.ps1")
            Write-TweakLog -Action "apply" -TweakId $Id -Status "error" -Message "Failed to apply tweak: $_"

            return [PSCustomObject]@{
                Status  = "error"
                TweakId = $Id
                Message = "Failed to apply tweak: $_"
            }
        }
    }
}
