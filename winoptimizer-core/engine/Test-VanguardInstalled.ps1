function Test-VanguardInstalled {
    <#
    .SYNOPSIS
        Detects if Riot Vanguard (VGC) anti-cheat is installed on the system.
    .DESCRIPTION
        Checks for the Vanguard service (vgc), installation directory, and running
        process to determine if Vanguard is present. Returns $true if any indicator is found.
    .OUTPUTS
        Boolean — $true if Vanguard is installed, $false otherwise.
    .EXAMPLE
        Test-VanguardInstalled
    .NOTES
        Core Engine Function — WinOptimizer Core
    #>
    [CmdletBinding()]
    param()

    # Check 1: vgc service exists
    $service = Get-Service -Name "vgc" -ErrorAction SilentlyContinue
    if ($service) { return $true }

    # Check 2: Vanguard directory exists
    $vanguardPaths = @(
        "$env:ProgramFiles\Riot Vanguard",
        "${env:ProgramFiles(x86)}\Riot Vanguard"
    )
    foreach ($path in $vanguardPaths) {
        if (Test-Path $path) { return $true }
    }

    # Check 3: Vanguard process running
    $process = Get-Process -Name "vgc" -ErrorAction SilentlyContinue
    if ($process) { return $true }

    return $false
}

function Test-VBSCompatibleTweak {
    <#
    .SYNOPSIS
        Tests if a tweak is compatible with VBS/HVCI/Vanguard.
    .DESCRIPTION
        Loads the tweak definition and returns its compatible_with_vanguard flag.
    .PARAMETER Id
        The tweak ID to check.
    .OUTPUTS
        PSCustomObject — With Id, Compatible, and Risk properties.
    .EXAMPLE
        Test-VBSCompatibleTweak -Id "disable-telemetry"
    .NOTES
        Core Engine Function — WinOptimizer Core
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$Id
    )

    . (Join-Path $PSScriptRoot "Get-AvailableTweaks.ps1")
    $tweak = Get-AvailableTweaks -Id $Id

    if (-not $tweak -or $tweak.Count -eq 0) {
        return [PSCustomObject]@{
            Id         = $Id
            Compatible = $null
            Risk       = $null
            Message    = "Tweak not found."
        }
    }
    $tweak = $tweak[0]

    return [PSCustomObject]@{
        Id         = $Id
        Compatible = $tweak.compatible_with_vanguard
        Risk       = $tweak.risk
    }
}
