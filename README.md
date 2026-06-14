# Simple Job Runner for Windows

## What It Is

Simple Job Runner is a small Windows utility for disposable Codex-powered local tasks. It is a focused workflow layer around Codex for quick text results, file generation, local file inspection, and one-off administrative tasks without creating Codex project/sidebar clutter.

It can stage selected files into a disposable workspace, run `codex exec`, capture the final answer as a Text Result, and copy generated files to a user-visible output folder.

## What It Is Not

Simple Job Runner is not an alternative to Codex, not a security boundary, not a compliance system, and not a substitute for reviewing commands or outputs before using them.

It does not create Codex app projects, persistent chat history, per-job Git repositories, or per-job GitHub repositories. It does not edit original input files and does not bundle runtime API keys.

## Who It Is For

It is for Windows users who want a repeatable front end for one-off Codex tasks: quick text answers, reports, file conversions, summaries, local inventories, and carefully confirmed external or administrative actions.

## Quick Start

1. Install Codex CLI and authenticate it with `codex login`.
2. Install Simple Job Runner from the MSI or extract the portable zip.
3. Launch the app and review Settings.
4. Use **Safe test** to load a harmless Text Query.
5. Click **Run Simple Job**.
6. Read the answer in **Text Result**.

## Requirements

- Windows 11 first; recent Windows 10 best effort.
- Codex CLI installed and available on `PATH`.
- Codex CLI authentication. `codex login` can use ChatGPT login, including ChatGPT Pro/Plus.
- Optional: an OpenAI API key for in-app speech-to-text or Codex API-key mode.

## Installation

Package outputs are written to:

```text
artifacts/release/SimpleJobRunnerSetup-x64.msi
artifacts/release/SimpleJobRunnerPortable-win-x64.zip
artifacts/release/SHA256SUMS.txt
```

The MSI creates a Start Menu entry and can optionally create a desktop shortcut:

```powershell
msiexec /i SimpleJobRunnerSetup-x64.msi INSTALLDESKTOPSHORTCUT=1
```

The portable zip runs from the extracted folder.

## First-Run Setup

On launch, the app checks whether Codex CLI is available and supports the required non-interactive flags. If setup is incomplete, it opens Settings and explains what is missing.

Settings lets you:

- choose existing Codex CLI authentication or API-key mode;
- set, test, or remove the optional OpenAI transcription key;
- choose the output folder;
- choose retention and privacy defaults;
- purge old disposable run folders.

## How To Run A Text Query

Use **Text Query** for direct answers, status checks, local queries, calculations, inventories, and lists. This mode prefers the `read-only` Codex sandbox and can succeed with no generated files.

Example:

```text
How much free space is available on my local disks? Return a concise table.
```

## How To Run A File Job

Use **File Job** for generated files, conversions, reports, spreadsheets, CSVs, Markdown, PDFs, and document jobs. This mode uses `workspace-write` and expects requested deliverables in `run\outputs`.

Example:

```text
Read the files in inbox and make an Excel spreadsheet in outputs with date, amount, counterparty, and notes.
```

## How Outputs Work

Every run should produce a text result. The **Text Result** tab is the primary output and supports copy, save as `.txt`, save as `.md`, and clear.

The **Files** tab lists generated files only when they exist. Text-only runs can complete successfully with an empty Files tab.

Generated files are copied from the disposable run folder to:

```text
%USERPROFILE%\Documents\Simple Job Outputs
```

unless you choose another output folder.

## How Cleanup Works

Each job gets a run folder under:

```text
%LOCALAPPDATA%\SimpleJobRunner\runs
```

The run folder contains `inbox`, `outputs`, `temp`, `run.json`, `summary.md`, and diagnostics when available. Successful runs delete temp files and, by default, do not retain prompt or transcript files. Final output files are kept until you delete them.

**Forget this job** deletes the disposable run folder. Output files are preserved by default unless you disable that setting.

## How Secrets Are Stored

The optional OpenAI API key is stored per Windows user in Windows Credential Locker under:

```text
Resource: SimpleJobRunner.OpenAI
User name: openai-api-key
```

The full key is not displayed after saving. The app redacts likely OpenAI keys, GitHub tokens, bearer tokens, passwords, and sensitive variable assignments from diagnostics and logs.

## Permission Modes

- **Text Query**: direct text result, prefers `read-only`.
- **File Job**: generated files in the disposable workspace, uses `workspace-write`.
- **External Action**: network or persistent external side effects, requires confirmation.
- **Admin / Sensitive**: deletes, service changes, package installs, system configuration, or elevation, requires confirmation.

The app does not use full system access by default.

## External Actions And Confirmations

Prompts that appear to create or modify external resources show a confirmation before Codex runs.

Example:

```text
Create a private GitHub repository called test-project, but ask for confirmation before creating it.
```

## Admin/Sensitive Tasks

Administrative prompts require explicit confirmation and should be reviewed carefully.

Example:

```text
List the incremental backups from the last three weeks in this selected backup folder.
```

Listing is normally a Text Query. Deleting, installing, changing services, modifying system configuration, or touching files outside the disposable workspace is treated as sensitive.

## Troubleshooting

If Codex CLI is missing, install and authenticate Codex CLI, then restart the app or reopen Settings. If voice transcription is disabled, save an OpenAI API key in Settings or type prompts manually.

The **Details** tab shows run ID, run folder, mode, sandbox, counts, Codex exit code, elapsed time, and command summary. The **Diagnostics** tab shows sanitized diagnostic text and can export a diagnostics bundle.

## Privacy And Sensitive Data

Do not use this app with patient-identifiable information, medical records, client-confidential files, legal matters, financial records, trade secrets, or regulated data unless you have confirmed that your use is permitted by your organization, account terms, data-retention settings, jurisdiction, and professional obligations.

Codex CLI may send prompts and relevant file context to OpenAI to perform the task. Review results before relying on them.

## For Technical Users: How It Calls Codex

Simple Job Runner invokes Codex with a command shaped like:

```powershell
codex exec `
  --cd "<runDir>" `
  --skip-git-repo-check `
  --ephemeral `
  --sandbox "<read-only or workspace-write>" `
  --json `
  --output-last-message "<runDir>\summary.md" `
  -
```

It passes prompts through stdin, keeps jobs outside Git repositories, captures `summary.md` as the Text Result, and writes `run.json` metadata for lifecycle status.

## Uninstalling

Use Windows Apps settings or:

```powershell
msiexec /x SimpleJobRunnerSetup-x64.msi
```

Uninstalling removes app files. It does not delete each user's `%LOCALAPPDATA%\SimpleJobRunner` state or final output folders.
