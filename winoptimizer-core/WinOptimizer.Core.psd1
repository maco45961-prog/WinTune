@{
    RootModule        = 'WinOptimizer.Core.psm1'
    ModuleVersion     = '1.0.0'
    GUID              = 'a1b2c3d4-e5f6-7890-abcd-ef1234567890'
    Author            = 'WinOptimizer Contributors'
    CompanyName       = 'WinOptimizer'
    Copyright         = '(c) 2026 WinOptimizer Contributors. MIT License.'
    Description       = 'Core Engine for the WinOptimizer ecosystem. Provides system optimization, backup/restore, and tweak management without UI.'
    PowerShellVersion = '5.1'
    FunctionsToExport = @(
        'Get-AvailableTweaks'
        'Invoke-Tweak'
        'Undo-Tweak'
        'Apply-Profile'
        'New-SystemBackup'
        'Restore-SystemBackup'
        'Test-VanguardInstalled'
        'Test-VBSCompatibleTweak'
        'Update-SystemState'
        'Write-TweakLog'
    )
    CmdletsToExport   = @()
    VariablesToExport  = @()
    AliasesToExport    = @()
    PrivateData = @{
        PSData = @{
            Tags       = @('Windows', 'Optimization', 'Tweaks', 'System', 'Performance')
            LicenseUri = 'https://github.com/winoptimizer/core/blob/main/LICENSE'
            ProjectUri = 'https://github.com/winoptimizer/core'
        }
    }
}
