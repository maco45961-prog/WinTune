Import-Module PSScriptAnalyzer -Force
$results = Invoke-ScriptAnalyzer -Path "D:\win\winoptimizer-core\engine" -Recurse -Severity Error,Warning
if ($results) {
    $results | Format-Table -AutoSize
    Write-Output "Total issues: $($results.Count)"
} else {
    Write-Output "No PSScriptAnalyzer errors or warnings found!"
}
