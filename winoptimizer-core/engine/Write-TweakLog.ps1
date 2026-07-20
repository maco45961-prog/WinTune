function Write-TweakLog {
    <#
    .SYNOPSIS
        Writes a log entry for tweak operations in both human-readable and JSON Lines format.
    .DESCRIPTION
        Appends to two log files: a human-readable .log file and a structured .jsonl file.
        Both are stored in the logs/ directory.
    .PARAMETER Action
        The action performed: apply, revert, restore, backup, profile_apply.
    .PARAMETER TweakId
        The tweak ID involved.
    .PARAMETER Status
        The result: success, error, blocked, partial.
    .PARAMETER Message
        Human-readable message describing the action.
    .PARAMETER ProfileName
        Optional. Profile name if the action was part of a profile application.
    .OUTPUTS
        None. Writes to log files.
    .EXAMPLE
        Write-TweakLog -Action "apply" -TweakId "disable-telemetry" -Status "success" -Message "Applied successfully"
    .NOTES
        Core Engine Function — WinOptimizer Core
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$Action,

        [Parameter(Mandatory = $true)]
        [string]$TweakId,

        [Parameter(Mandatory = $true)]
        [ValidateSet("success", "error", "blocked", "partial")]
        [string]$Status,

        [Parameter(Mandatory = $true)]
        [string]$Message,

        [Parameter(Mandatory = $false)]
        [string]$ProfileName
    )

    $logDir = Join-Path (Join-Path $PSScriptRoot "..") "logs"
    if (-not (Test-Path $logDir)) {
        New-Item -ItemType Directory -Path $logDir -Force | Out-Null
    }

    $date = Get-Date -Format "yyyyMMdd"
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $timestampISO = (Get-Date).ToString("o")

    # Human-readable log
    $logFile = Join-Path $logDir "winoptimizer-$date.log"
    $entry = "[$timestamp] [$($Action.ToUpper())] [$($Status.ToUpper())] Tweak=$TweakId"
    if ($ProfileName) { $entry += " Profile=$ProfileName" }
    $entry += " — $Message"
    Add-Content -Path $logFile -Value $entry -Encoding UTF8

    # Structured JSON Lines log
    $jsonlFile = Join-Path $logDir "winoptimizer-$date.jsonl"
    $jsonEntry = [PSCustomObject]@{
        timestamp = $timestampISO
        action = $Action
        tweak_id = $TweakId
        status = $Status
        message = $Message
        profile = $ProfileName
        hostname = $env:COMPUTERNAME
        user = $env:USERNAME
    }
    $jsonLine = $jsonEntry | ConvertTo-Json -Compress
    Add-Content -Path $jsonlFile -Value $jsonLine -Encoding UTF8
}
