# User Guide

## Setup

Open Settings and configure:

- OpenAI API key, optional for typed jobs and required for voice transcription.
- Speech model.
- Codex authentication mode.
- Output folder.
- Retention options.

Use **Test key** to verify the OpenAI API key if you choose to store one.

## Codex Authentication

The recommended default is **Use existing Codex CLI authentication (ChatGPT login)**. In this mode Simple Job Runner relies on your existing `codex login` session and does not set `CODEX_API_KEY`.

Choose **Pass stored OpenAI API key to codex exec** only if you want Simple Job Runner to set `CODEX_API_KEY` for the single Codex child process. That mode uses OpenAI API billing rather than ChatGPT plan credits.

## Voice Input

Click **Record**, speak the task, then click **Stop**. The app uploads the audio file to OpenAI speech-to-text, deletes the audio file after transcription, and inserts the transcript into the prompt box. Review and edit the prompt before running.

The Record button is disabled until an OpenAI API key is saved in Settings. You can always type a prompt manually and run a job through existing Codex CLI authentication.

## File Jobs

Use **Add files**, **Add folder**, drag and drop, or command-line file paths. The app copies selected inputs to the run folder before invoking Codex.

## Outputs

Final outputs are copied to the configured output folder. The default is:

```text
%USERPROFILE%\Documents\Simple Job Outputs
```

Use **Forget this job** to delete the disposable run folder. Final copied outputs are kept.
