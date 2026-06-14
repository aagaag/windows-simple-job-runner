# Deployment

## Build The MSI Locally

```powershell
./scripts/package.ps1 -Version 0.1.0
```

Outputs:

```text
artifacts/release/SimpleJobRunnerSetup-x64.msi
artifacts/release/SimpleJobRunnerPortable-win-x64.zip
artifacts/release/SHA256SUMS.txt
```

## Install On One Machine

```powershell
msiexec /i artifacts/release/SimpleJobRunnerSetup-x64.msi
```

Launch Simple Job Runner from the Start Menu. Each Windows user configures their own OpenAI API key on first launch.

## Deploy To Several Machines

Distribute the MSI with your normal Windows software deployment tool. The installer contains no runtime OpenAI API key and does not create run folders during installation.

For a desktop shortcut, deploy with:

```powershell
msiexec /i SimpleJobRunnerSetup-x64.msi INSTALLDESKTOPSHORTCUT=1 /qn
```

## Update

Build a newer version and deploy the new MSI. WiX `MajorUpgrade` handles replacement of older installed versions.

## Uninstall

Use Windows Apps settings or:

```powershell
msiexec /x SimpleJobRunnerSetup-x64.msi
```

Uninstalling removes app files. It does not delete each user's `%LOCALAPPDATA%\SimpleJobRunner` state or final output folders.
