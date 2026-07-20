function Apply-Profile {
    <#
    .SYNOPSIS
        Applies a profile of multiple tweaks by profile name.
    .DESCRIPTION
        Loads a profile JSON file, validates all tweak IDs exist, creates a backup,
        and applies each tweak sequentially. Updates system-state.json with the profile name.
    .PARAMETER Name
        The profile name (filename without .json).
    .PARAMETER Force
        Bypass Vanguard compatibility checks for all tweaks in the profile.
    .OUTPUTS
        PSCustomObject[] — Array of results for each tweak in the profile.
    .EXAMPLE
        Apply-Profile -Name "debloat-estandar"
    .EXAMPLE
        Apply-Profile -Name "gaming-competitivo-lowend" -Force
    .NOTES
        Core Engine Function — WinOptimizer Core
    #>
    [CmdletBinding(SupportsShouldProcess)]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '', Justification = 'Apply is an approved verb since PowerShell 3.0')]
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name,

        [Parameter(Mandatory = $false)]
        [switch]$Force
    )

    $ErrorActionPreference = "Stop"
    $profilesRoot = Join-Path (Join-Path $PSScriptRoot "..") "profiles"
    $profileFile = Join-Path $profilesRoot "$Name.json"

    if (-not (Test-Path $profileFile)) {
        return [PSCustomObject]@{
            Status  = "error"
            Profile = $Name
            Message = "Profile '$Name' not found at: $profileFile"
        }
    }

    $profileDef = Get-Content -Path $profileFile -Raw | ConvertFrom-Json

    if ($PSCmdlet.ShouldProcess($profileDef.name, "Apply profile with $($profileDef.tweak_ids.Count) tweaks")) {
        # Pre-backup
        . (Join-Path $PSScriptRoot "New-SystemBackup.ps1")
        New-SystemBackup -TweakId "profile-$Name" | Out-Null

        . (Join-Path $PSScriptRoot "Invoke-Tweak.ps1")
        . (Join-Path $PSScriptRoot "Write-TweakLog.ps1")

        $results = @()
        $appliedCount = 0
        $errorCount = 0

        foreach ($tweakId in $profileDef.tweak_ids) {
            $invokeParams = @{ Id = $tweakId }
            if ($Force) { $invokeParams.Force = $true }

            $result = Invoke-Tweak @invokeParams
            $results += $result

            if ($result.Status -eq "success") {
                $appliedCount++
                Write-TweakLog -Action "profile_apply" -TweakId $tweakId -Status "success" -Message "Applied as part of profile '$Name'" -ProfileName $Name
            }
            else {
                $errorCount++
                Write-TweakLog -Action "profile_apply" -TweakId $tweakId -Status $result.Status -Message "$($result.Message)" -ProfileName $Name
            }
        }

        # Update system-state.json with profile name
        $stateFile = Join-Path (Join-Path (Join-Path $PSScriptRoot "..") "shared") "system-state.json"
        if (Test-Path $stateFile) {
            $state = Get-Content -Path $stateFile -Raw | ConvertFrom-Json
            $state.profile_applied = $Name
            $state | ConvertTo-Json -Depth 10 | Set-Content -Path $stateFile -Encoding UTF8
        }

        return [PSCustomObject]@{
            Status  = if ($errorCount -eq 0) { "success" } else { "partial" }
            Profile = $Name
            ProfileDescription = $profileDef.description
            Applied = $appliedCount
            Errors  = $errorCount
            Total   = $profileDef.tweak_ids.Count
            Results = $results
            Message = "Profile '$Name': $appliedCount/$($profileDef.tweak_ids.Count) tweaks applied. $errorCount errors."
        }
    }
}
