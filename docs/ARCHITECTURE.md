# Architecture

Simple Job Runner is split into a WPF app and a testable core library.

## Projects

- `SimpleJobRunner.App`: WPF UI, MVVM view models, file/folder pickers, shell launching, and NAudio recording.
- `SimpleJobRunner.Core`: run folder lifecycle, settings, Credential Locker access, transcription, Codex process execution, output collection, cleanup, and secret masking.
- `SimpleJobRunner.Tests`: xUnit tests for non-UI behavior.

## Job Flow

```text
Select files/folders
  -> copy to run\inbox
  -> record/transcribe or type prompt
  -> write run AGENTS.md
  -> classify task mode
  -> create run.json metadata
  -> build Codex prompt
  -> codex exec in run folder with read-only or workspace-write sandbox
  -> capture final assistant message to run\summary.md
  -> collect run\outputs when files exist
  -> update run lifecycle and diagnostics
  -> render RunResult text and optional files
```

The app never passes original input paths to Codex as writable targets. Codex sees the disposable run folder.

Text Query mode uses the least permissive `read-only` sandbox where possible. File Job, External Action, and Admin / Sensitive modes use `workspace-write` so requested files can be created in the disposable run folder. External and sensitive modes require confirmation before the Codex process starts.

## Key Services

- `RunFolderManager`: creates and cleans disposable run folders.
- `PromptBuilder`: builds the final prompt and rejects likely OpenAI API keys.
- `TaskModeClassifier`: auto-selects Text Query, File Job, External Action, or Admin / Sensitive mode from the visible prompt.
- `WindowsCredentialStore`: stores runtime OpenAI API keys in Windows Credential Locker.
- `OpenAiTranscriptionService`: calls `/v1/audio/transcriptions`.
- `CodexRunner`: runs `codex exec` through `ProcessStartInfo` without PowerShell.
- `OutputCollector`: validates and copies non-empty deliverables to the final output folder.
- `RunResultFactory`: reads `summary.md`, combines optional output files and warnings, and produces the structured result displayed by the UI.
- `RunMetadataStore`: writes machine-readable `run.json` lifecycle metadata.
- `RunDiagnostics`: writes and exports sanitized diagnostic information.
- `RunRetentionManager`: removes temp files after successful runs and purges old disposable run folders.
- `FailureExplanation`: turns exceptions into user-readable failure sections with technical details.

## Run Lifecycle

Runs are represented with `Created`, `Preparing`, `Transcribing`, `ReadyToRun`, `RunningCodex`, `CollectingOutputs`, `Completed`, `Failed`, and `Forgotten` statuses. The current implementation writes run metadata during app-driven execution; voice transcription still happens before a run folder exists.

## Diagnostics

Diagnostics are sanitized with `SecretMasker` before display or export. Diagnostic bundles include metadata, progress, summary, diagnostics, and environment details, but not input files or output files.
