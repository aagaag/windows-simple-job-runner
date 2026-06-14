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
  -> build Codex prompt
  -> codex exec in run folder
  -> collect run\outputs
  -> copy final outputs to Documents\Simple Job Outputs
```

The app never passes original input paths to Codex as writable targets. Codex sees the disposable run folder.

## Key Services

- `RunFolderManager`: creates and cleans disposable run folders.
- `PromptBuilder`: builds the final prompt and rejects likely OpenAI API keys.
- `WindowsCredentialStore`: stores runtime OpenAI API keys in Windows Credential Locker.
- `OpenAiTranscriptionService`: calls `/v1/audio/transcriptions`.
- `CodexRunner`: runs `codex exec` through `ProcessStartInfo` without PowerShell.
- `OutputCollector`: validates and copies non-empty deliverables to the final output folder.
