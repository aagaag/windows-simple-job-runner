param(
    [string]$Version = "0.3.0"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

& "$PSScriptRoot\build.ps1" -Configuration Release
& "$PSScriptRoot\test.ps1" -Configuration Release
& "$PSScriptRoot\package.ps1" -Configuration Release -Version $Version
