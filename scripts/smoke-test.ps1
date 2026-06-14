$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repoRoot = Split-Path -Parent $PSScriptRoot
$releaseDir = Join-Path $repoRoot "artifacts\release"
$required = @(
    "SimpleJobRunnerSetup-x64.msi",
    "SimpleJobRunnerPortable-win-x64.zip",
    "SHA256SUMS.txt"
)

foreach ($name in $required) {
    $path = Join-Path $releaseDir $name
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Missing package artifact: $path"
    }

    if ((Get-Item -LiteralPath $path).Length -eq 0) {
        throw "Package artifact is empty: $path"
    }
}

Write-Host "Package artifacts exist and are non-empty."
