# Lists NuGet-eligible projects under src/ (has <PackageId>, not <IsPackable>false</IsPackable>).
# Mirrors the discovery step in .github/workflows/Publish.yml.
$root = Split-Path $PSScriptRoot -Parent

Get-ChildItem -Path (Join-Path $root 'src') -Filter *.csproj -Recurse | ForEach-Object {
    $xml = Get-Content $_.FullName -Raw
    $id = [regex]::Match($xml, '<PackageId>(.*?)</PackageId>').Groups[1].Value
    if (-not $id) { return }
    if ($xml -match '(?i)<IsPackable>false') { return }
    [pscustomobject]@{
        PackageId = $id
        Version   = [regex]::Match($xml, '<Version>(.*?)</Version>').Groups[1].Value
        Project   = $_.FullName.Substring($root.Length + 1)
    }
} | Format-Table -AutoSize
