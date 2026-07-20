function Get-AvailableTweaks {
    <#
    .SYNOPSIS
        Returns the complete catalog of available tweaks from all categories.
    .DESCRIPTION
        Scans all tweak definition JSON files under the tweaks/ directory and returns
        an array of tweak objects. This is the primary discovery mechanism for any
        client application (UI, daemon, etc.) to enumerate what tweaks are available
        without hardcoding anything.
    .PARAMETER Category
        Optional. Filter tweaks by category. Valid values: privacy, services, network,
        gaming, appearance, startmenu-taskbar, explorer.
    .PARAMETER Id
        Optional. Return a single tweak by its exact ID.
    .OUTPUTS
        PSCustomObject[] — Array of tweak definition objects.
    .EXAMPLE
        Get-AvailableTweaks
        Returns all tweaks from all categories.
    .EXAMPLE
        Get-AvailableTweaks -Category "privacy"
        Returns only privacy tweaks.
    .EXAMPLE
        Get-AvailableTweaks -Id "disable-telemetry"
        Returns the specific tweak with id "disable-telemetry".
    .NOTES
        Core Engine Function — WinOptimizer Core
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseSingularNouns', '', Justification = 'Function returns multiple tweaks by design')]
    param(
        [Parameter(Mandatory = $false)]
        [ValidateSet("privacy", "services", "network", "gaming", "appearance", "startmenu-taskbar", "explorer")]
        [string]$Category,

        [Parameter(Mandatory = $false)]
        [string]$Id
    )

    $tweaksRoot = Join-Path (Join-Path $PSScriptRoot "..") "tweaks"

    if (-not (Test-Path $tweaksRoot)) {
        Write-Error "Tweaks directory not found at: $tweaksRoot"
        return @()
    }

    if ($Id) {
        $searchCategories = if ($Category) { @($Category) } else {
            Get-ChildItem -Path $tweaksRoot -Directory | Select-Object -ExpandProperty Name
        }

        foreach ($cat in $searchCategories) {
            $catPath = Join-Path $tweaksRoot $cat
            $file = Join-Path $catPath "$Id.json"
            if (Test-Path $file) {
                $tweak = Get-Content -Path $file -Raw | ConvertFrom-Json
                return @($tweak)
            }
        }
        Write-Warning "Tweak with ID '$Id' not found."
        return @()
    }

    $categories = if ($Category) {
        @($Category)
    } else {
        Get-ChildItem -Path $tweaksRoot -Directory | Select-Object -ExpandProperty Name
    }

    $allTweaks = @()

    foreach ($cat in $categories) {
        $catPath = Join-Path $tweaksRoot $cat
        if (-not (Test-Path $catPath)) {
            Write-Warning "Category directory not found: $cat"
            continue
        }

        $jsonFiles = Get-ChildItem -Path $catPath -Filter "*.json" -File
        foreach ($file in $jsonFiles) {
            try {
                $tweak = Get-Content -Path $file.FullName -Raw | ConvertFrom-Json
                $allTweaks += $tweak
            }
            catch {
                Write-Warning "Failed to parse tweak file: $($file.FullName) — $_"
            }
        }
    }

    return $allTweaks
}
