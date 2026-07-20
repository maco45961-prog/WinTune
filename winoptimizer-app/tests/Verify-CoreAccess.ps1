Import-Module 'D:\win\winoptimizer-core\WinOptimizer.Core.psd1' -Force
$funcs = Get-Command -Module WinOptimizer.Core
Write-Output '=== Public Functions ==='
$funcs | ForEach-Object { $_.Name } | Sort-Object
Write-Output ''
Write-Output '=== Test: Get-AvailableTweaks ==='
$tweaks = Get-AvailableTweaks
Write-Output "Total tweaks: $($tweaks.Count)"
$tweaks | ForEach-Object { "  $($_.id) [ $($_.category) ] risk=$($_.risk) vanguard=$($_.compatible_with_vanguard)" }
Write-Output ''
Write-Output '=== Profiles ==='
Get-ChildItem 'D:\win\winoptimizer-core\profiles\*.json' | ForEach-Object {
    $p = Get-Content $_.FullName -Raw | ConvertFrom-Json
    $desc = if ($p.description.Length -gt 60) { $p.description.Substring(0,60) + "..." } else { $p.description }
    "  $($p.name): $($p.tweak_ids.Count) tweaks - $desc"
}
Write-Output ''
Write-Output '=== Test: Test-VanguardInstalled ==='
$vg = Test-VanguardInstalled
Write-Output "  Vanguard installed: $vg"
