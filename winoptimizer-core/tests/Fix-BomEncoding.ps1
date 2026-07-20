$files = Get-ChildItem "D:\win\winoptimizer-core\engine\*.ps1"
foreach ($f in $files) {
    $content = [System.IO.File]::ReadAllText($f.FullName)
    $utf8Bom = New-Object System.Text.UTF8Encoding $true
    [System.IO.File]::WriteAllText($f.FullName, $content, $utf8Bom)
    Write-Output "Re-encoded with BOM: $($f.Name)"
}
