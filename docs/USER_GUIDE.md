# User Guide

## Setup

Open Settings and configure:

- OpenAI API key.
- Speech model.
- Codex authentication mode.
- Output folder.
- Retention options.

Use **Test key** to verify the OpenAI API key.

## Voice Input

Click **Record**, speak the task, then click **Stop**. The app uploads the audio file to OpenAI speech-to-text, deletes the audio file after transcription, and inserts the transcript into the prompt box. Review and edit the prompt before running.

## File Jobs

Use **Add files**, **Add folder**, drag and drop, or command-line file paths. The app copies selected inputs to the run folder before invoking Codex.

## Outputs

Final outputs are copied to the configured output folder. The default is:

```text
%USERPROFILE%\Documents\Simple Job Outputs
```

Use **Forget this job** to delete the disposable run folder. Final copied outputs are kept.
