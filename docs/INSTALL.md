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

The MSI installs app files under Program Files and creates a Start Menu shortcut. It does not include runtime API keys, create Git repositories, create background services, or create run folders during installation.

The current MSI is self-contained and includes the .NET Desktop Runtime files needed by the app. If Windows shows "You must install .NET Desktop Runtime to run this application", install the latest `SimpleJobRunnerSetup-x64.msi`; it upgrades earlier broken installs that were missing bundled runtime files.

After installing, run `codex login` if Codex CLI is not already authenticated. You can use ChatGPT login, including ChatGPT Pro/Plus. An OpenAI API key is optional unless you want in-app voice transcription or Codex API-key mode.

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

Typed jobs can run without a stored OpenAI API key when Codex CLI is authenticated through `codex login`.

After first launch, the app creates `%LOCALAPPDATA%\SimpleJobRunner` and the default output folder as needed.
