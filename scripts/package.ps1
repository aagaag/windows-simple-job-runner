param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$Version = "0.2.0"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

function Invoke-Native {
    $command = $args[0]
    $commandArgs = @()
    if ($args.Count -gt 1) {
        $commandArgs = $args[1..($args.Count - 1)]
    }
    & $command @commandArgs
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed with exit code ${LASTEXITCODE}: $args"
    }
}

function Get-StableWixId {
    param(
        [string]$Prefix,
        [string]$Value
    )

    $sha1 = [System.Security.Cryptography.SHA1]::Create()
    try {
        $bytes = [System.Text.Encoding]::UTF8.GetBytes($Value.ToLowerInvariant())
        $hash = [System.BitConverter]::ToString($sha1.ComputeHash($bytes)).Replace("-", "").Substring(0, 16)
        return "$Prefix$hash"
    }
    finally {
        $sha1.Dispose()
    }
}

function Escape-Xml {
    param([string]$Value)
    return [System.Security.SecurityElement]::Escape($Value)
}

function Get-RelativePath {
    param(
        [string]$BasePath,
        [string]$TargetPath
    )

    $base = (Resolve-Path -LiteralPath $BasePath).Path.TrimEnd('\') + '\'
    $target = (Resolve-Path -LiteralPath $TargetPath).Path
    $baseUri = New-Object System.Uri($base)
    $targetUri = New-Object System.Uri($target)
    return [System.Uri]::UnescapeDataString($baseUri.MakeRelativeUri($targetUri).ToString()).Replace('/', '\')
}

function New-GeneratedWixFiles {
    param(
        [string]$SourceDirectory,
        [string]$OutputPath
    )

    $directoryIds = @{}
    $directoryIds["."] = "INSTALLFOLDER"

    $directories = Get-ChildItem -LiteralPath $SourceDirectory -Directory -Recurse | Sort-Object FullName
    foreach ($directory in $directories) {
        $relative = Get-RelativePath -BasePath $SourceDirectory -TargetPath $directory.FullName
        $directoryIds[$relative] = Get-StableWixId -Prefix "dir_" -Value $relative
    }

    $directoryLines = New-Object System.Collections.Generic.List[string]
    foreach ($directory in $directories) {
        $relative = Get-RelativePath -BasePath $SourceDirectory -TargetPath $directory.FullName
        $parentRelative = [System.IO.Path]::GetDirectoryName($relative)
        if ([string]::IsNullOrWhiteSpace($parentRelative)) {
            $parentRelative = "."
        }

        $parentId = $directoryIds[$parentRelative]
        $directoryId = $directoryIds[$relative]
        $name = Escape-Xml ([System.IO.Path]::GetFileName($directory.FullName))
        $directoryLines.Add("    <DirectoryRef Id=`"$parentId`"><Directory Id=`"$directoryId`" Name=`"$name`" /></DirectoryRef>")
    }

    $componentLines = New-Object System.Collections.Generic.List[string]
    $componentRefLines = New-Object System.Collections.Generic.List[string]
    $files = Get-ChildItem -LiteralPath $SourceDirectory -File -Recurse |
        Where-Object { $_.Extension -ne ".pdb" } |
        Sort-Object FullName

    foreach ($file in $files) {
        $relative = Get-RelativePath -BasePath $SourceDirectory -TargetPath $file.FullName
        $relativeDirectory = [System.IO.Path]::GetDirectoryName($relative)
        if ([string]::IsNullOrWhiteSpace($relativeDirectory)) {
            $relativeDirectory = "."
        }

        $directoryId = $directoryIds[$relativeDirectory]
        $componentId = Get-StableWixId -Prefix "cmp_" -Value $relative
        $fileId = Get-StableWixId -Prefix "fil_" -Value $relative
        $source = Escape-Xml $file.FullName

        $componentLines.Add("    <DirectoryRef Id=`"$directoryId`">")
        $componentLines.Add("      <Component Id=`"$componentId`" Guid=`"*`">")
        $componentLines.Add("        <File Id=`"$fileId`" Source=`"$source`" KeyPath=`"yes`" />")
        $componentLines.Add("      </Component>")
        $componentLines.Add("    </DirectoryRef>")
        $componentRefLines.Add("      <ComponentRef Id=`"$componentId`" />")
    }

    $lines = New-Object System.Collections.Generic.List[string]
    $lines.Add('<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">')
    $lines.Add("  <Fragment>")
    foreach ($line in $directoryLines) {
        $lines.Add($line)
    }
    $lines.Add("  </Fragment>")
    $lines.Add("  <Fragment>")
    foreach ($line in $componentLines) {
        $lines.Add($line)
    }
    $lines.Add("  </Fragment>")
    $lines.Add("  <Fragment>")
    $lines.Add('    <ComponentGroup Id="PublishedFiles">')
    foreach ($line in $componentRefLines) {
        $lines.Add($line)
    }
    $lines.Add("    </ComponentGroup>")
    $lines.Add("  </Fragment>")
    $lines.Add("</Wix>")

    Set-Content -LiteralPath $OutputPath -Value $lines -Encoding utf8
}

$artifactsRoot = Join-Path $repoRoot "artifacts"
$publishRoot = Join-Path $artifactsRoot "publish"
$publishDir = Join-Path $publishRoot "SimpleJobRunner"
$releaseDir = Join-Path $artifactsRoot "release"
$zipPath = Join-Path $releaseDir "SimpleJobRunnerPortable-win-x64.zip"
$hashPath = Join-Path $releaseDir "SHA256SUMS.txt"

if (Test-Path $publishDir) {
    Remove-Item -LiteralPath $publishDir -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $publishDir, $releaseDir | Out-Null

Invoke-Native dotnet restore .\SimpleJobRunner.sln
Invoke-Native dotnet publish .\src\SimpleJobRunner.App\SimpleJobRunner.App.csproj `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -o $publishDir `
    -p:Version=$Version `
    -p:AssemblyVersion=$Version.0 `
    -p:FileVersion=$Version.0

Copy-Item -LiteralPath .\README-portable.txt -Destination (Join-Path $publishDir "README-portable.txt") -Force

if (Test-Path $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

Compress-Archive -Path (Join-Path $publishDir "*") -DestinationPath $zipPath -Force

New-GeneratedWixFiles `
    -SourceDirectory $publishDir `
    -OutputPath (Join-Path $repoRoot "installer\SimpleJobRunner.Installer\GeneratedFiles.wxs")

Invoke-Native dotnet build .\installer\SimpleJobRunner.Installer\SimpleJobRunner.Installer.wixproj `
    -c $Configuration `
    -p:PublishDir=$publishDir `
    -p:ProductVersion=$Version

$msiSource = Join-Path $repoRoot "installer\SimpleJobRunner.Installer\bin\x64\$Configuration\SimpleJobRunnerSetup-x64.msi"
$msiTarget = Join-Path $releaseDir "SimpleJobRunnerSetup-x64.msi"
Copy-Item -LiteralPath $msiSource -Destination $msiTarget -Force

Get-ChildItem -LiteralPath $releaseDir -File |
    Where-Object { $_.Name -ne "SHA256SUMS.txt" } |
    Sort-Object Name |
    ForEach-Object {
        $hash = Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256
        "$($hash.Hash.ToLowerInvariant())  $($_.Name)"
    } | Set-Content -LiteralPath $hashPath -Encoding ascii

Write-Host "Created:"
Write-Host "  $msiTarget"
Write-Host "  $zipPath"
Write-Host "  $hashPath"
