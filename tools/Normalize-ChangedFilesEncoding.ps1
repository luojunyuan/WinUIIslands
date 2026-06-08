param(
    [switch]$ExcludeUntracked
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = (& git rev-parse --show-toplevel).Trim()
if ([string]::IsNullOrWhiteSpace($repoRoot)) {
    throw 'This script must be run inside a git repository.'
}

function Get-ChangedPath {
    $paths = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)

    $tracked = & git -C $repoRoot diff --name-only --diff-filter=ACMRT HEAD --
    foreach ($path in $tracked) {
        if (![string]::IsNullOrWhiteSpace($path)) {
            [void]$paths.Add($path)
        }
    }

    if (!$ExcludeUntracked) {
        $untracked = & git -C $repoRoot ls-files --others --exclude-standard
        foreach ($path in $untracked) {
            if (![string]::IsNullOrWhiteSpace($path)) {
                [void]$paths.Add($path)
            }
        }
    }

    $paths
}

function Test-BinaryContent {
    param([byte[]]$Bytes)

    foreach ($byte in $Bytes) {
        if ($byte -eq 0) {
            return $true
        }
    }

    return $false
}

$utf8NoBom = [System.Text.UTF8Encoding]::new($false, $false)
$utf8Bom = [System.Text.UTF8Encoding]::new($true)
$normalizedCount = 0
$skippedCount = 0

foreach ($relativePath in Get-ChangedPath) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (!(Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        continue
    }

    $bytes = [System.IO.File]::ReadAllBytes($fullPath)
    if (Test-BinaryContent -Bytes $bytes) {
        Write-Host "skip binary: $relativePath"
        $skippedCount++
        continue
    }

    $text = $utf8NoBom.GetString($bytes)
    if ($text.Length -gt 0 -and $text[0] -eq [char]0xFEFF) {
        $text = $text.Substring(1)
    }

    $text = [regex]::Replace($text, "`r`n|`r|`n", "`n")
    $text = $text.Replace("`n", "`r`n")

    [System.IO.File]::WriteAllText($fullPath, $text, $utf8Bom)
    Write-Host "normalized: $relativePath"
    $normalizedCount++
}

Write-Host "done: normalized=$normalizedCount skipped=$skippedCount"
