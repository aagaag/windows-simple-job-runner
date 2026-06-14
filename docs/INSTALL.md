# Install

## MSI

Build or download:

```text
SimpleJobRunnerSetup-x64.msi
```

Install with the normal Windows installer UI, or from an elevated terminal:

```powershell
msiexec /i SimpleJobRunnerSetup-x64.msi
```

To request a desktop shortcut:

```powershell
msiexec /i SimpleJobRunnerSetup-x64.msi INSTALLDESKTOPSHORTCUT=1
```

The MSI installs app files under Program Files and creates a Start Menu shortcut.

## Portable Zip

Extract:

```text
SimpleJobRunnerPortable-win-x64.zip
```

Run:

```text
SimpleJobRunner.App.exe
```

The portable build still stores user settings under `%LOCALAPPDATA%\SimpleJobRunner` and stores the OpenAI API key in Windows Credential Locker.
