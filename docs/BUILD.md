# Build

## Requirements

- Windows with PowerShell.
- .NET SDK capable of building `net8.0` and `net8.0-windows`.
- Internet access for NuGet restore.

## Commands

```powershell
./scripts/build.ps1
./scripts/test.ps1
```

The build script restores packages and builds the WPF app, core library, and tests in Release mode.

## Packaging

```powershell
./scripts/package.ps1 -Version 0.3.2
```

The package script:

- publishes the WPF app self-contained for `win-x64`;
- verifies that the self-contained Windows desktop runtime files are present;
- copies `README-portable.txt` into the publish folder;
- creates `SimpleJobRunnerPortable-win-x64.zip`;
- generates a WiX file list for the publish folder;
- builds `SimpleJobRunnerSetup-x64.msi`;
- writes `SHA256SUMS.txt`.

Generated package artifacts are under `artifacts/release`.
