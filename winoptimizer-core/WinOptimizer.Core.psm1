# WinOptimizer Core Engine Module
# Dot-sources all engine functions and makes them available in the session.

$enginePath = Join-Path $PSScriptRoot "engine"

$scripts = @(
    "Get-AvailableTweaks.ps1"
    "Test-VanguardInstalled.ps1"
    "Write-TweakLog.ps1"
    "New-SystemBackup.ps1"
    "Update-SystemState.ps1"
    "Invoke-Tweak.ps1"
    "Undo-Tweak.ps1"
    "Apply-Profile.ps1"
)

foreach ($script in $scripts) {
    $scriptPath = Join-Path $enginePath $script
    if (Test-Path $scriptPath) {
        . $scriptPath
    }
    else {
        Write-Warning "WinOptimizer.Core: Engine script not found: $scriptPath"
    }
}
