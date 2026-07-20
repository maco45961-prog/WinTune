function Undo-Tweak {
    <#
    .SYNOPSIS
        Reverts a single tweak by its ID.
    .DESCRIPTION
        Loads the tweak definition and executes its revert command. Validates that
        the tweak is reversible before attempting.
    .PARAMETER Id
        The tweak ID to revert (e.g., "disable-telemetry").
    .OUTPUTS
        PSCustomObject — Result with status, tweak id, and message.
    .EXAMPLE
        Undo-Tweak -Id "disable-telemetry"
    .NOTES
        Core Engine Function — WinOptimizer Core
    #>
[CmdletBinding(SupportsShouldProcess)]
[Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSAvoidUsingInvokeExpression', '', Justification = 'Dynamic execution of tweak commands stored as strings in JSON definitions')]
param(
        [Parameter(Mandatory = $true)]
        [string]$Id
    )

    $ErrorActionPreference = "Stop"

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

    if (-not $tweak.reversible) {
        return [PSCustomObject]@{
            Status  = "blocked"
            TweakId = $Id
            Message = "Tweak '$Id' is marked as irreversible."
        }
    }

    if ($PSCmdlet.ShouldProcess($tweak.name, "Revert tweak")) {
        try {
            Invoke-Expression $tweak.commands.revert

            . (Join-Path $PSScriptRoot "Update-SystemState.ps1")
            Update-SystemState -TweakId $Id -Action "revert"

            . (Join-Path $PSScriptRoot "Write-TweakLog.ps1")
            Write-TweakLog -Action "revert" -TweakId $Id -Status "success" -Message "Tweak '$($tweak.name)' reverted successfully."

            return [PSCustomObject]@{
                Status  = "success"
                TweakId = $Id
                Name    = $tweak.name
                Message = "Tweak reverted successfully."
            }
        }
        catch {
            . (Join-Path $PSScriptRoot "Write-TweakLog.ps1")
            Write-TweakLog -Action "revert" -TweakId $Id -Status "error" -Message "Failed to revert tweak: $_"

            return [PSCustomObject]@{
                Status  = "error"
                TweakId = $Id
                Message = "Failed to revert tweak: $_"
            }
        }
    }
}
