# Simple Job Runner

Simple Job Runner is a Windows desktop utility for one-off Codex file jobs. It lets you select local files, dictate a task, transcribe the task with OpenAI speech-to-text, run Codex CLI in an ephemeral local workspace, and collect generated files without creating Codex app sidebar clutter or per-job GitHub repositories.

## What It Does

- Stages selected files and folders into a disposable run folder.
- Records push-to-stop microphone audio and transcribes it with OpenAI speech-to-text.
- Shows the transcript in an editable prompt box before any Codex run starts.
- Runs `codex exec` non-interactively with `--skip-git-repo-check`, `--ephemeral`, `--sandbox workspace-write`, `--json`, and `--output-last-message`.
- Copies final generated files to `Documents\Simple Job Outputs` by default.
- Lets the user forget a job by deleting the disposable run folder while keeping final outputs.

## What It Does Not Do

- It does not create Codex app projects, persistent chat history, per-job Git repositories, or per-job GitHub repositories.
- It does not edit original input files.
- It does not store runtime API keys in files, logs, installers, or source control.
- It does not bundle Codex CLI.

## Prerequisites

- Windows 11 first, recent Windows 10 best effort.
- Codex CLI installed and available on `PATH`.
- Codex CLI authentication. `codex login` can use ChatGPT login, including ChatGPT Pro/Plus.
- Optional: an OpenAI API key for in-app speech-to-text.

## Build

```powershell
./scripts/build.ps1
./scripts/test.ps1
./scripts/package.ps1
```

Package outputs are written to:

```text
artifacts/release/SimpleJobRunnerSetup-x64.msi
artifacts/release/SimpleJobRunnerPortable-win-x64.zip
artifacts/release/SHA256SUMS.txt
```

## First Launch

On first launch the app checks Codex CLI and opens Settings only if required setup is incomplete. Typed jobs can run without an OpenAI API key when Codex CLI is already authenticated with `codex login`.

The OpenAI API key is optional unless you want the **Record** button or you explicitly choose Codex API-key mode. If saved, it is stored in Windows Credential Locker under:

```text
Resource: SimpleJobRunner.OpenAI
User name: openai-api-key
```

## Running A Job

1. Add files or a folder, or drag files onto the app.
2. Type a prompt directly, or record a voice prompt if an OpenAI API key is configured.
3. Edit the prompt.
4. Click **Run Simple Job**.
5. Open generated files from the Outputs panel or from the output folder.

## Ephemerality

Each job gets a fresh run folder under:

```text
%LOCALAPPDATA%\SimpleJobRunner\runs
```

Inputs are copied into `inbox`, temporary work goes in `temp`, Codex writes deliverables to `outputs`, and final user-visible files are copied to:

```text
%USERPROFILE%\Documents\Simple Job Outputs
```

## Security And Privacy

Audio is sent to OpenAI only when you use **Record**. Codex CLI may send prompts and relevant file context to OpenAI to complete the job. Treat input files as confidential and review `docs/PRIVACY.md` and `docs/SECRETS.md` before use.
