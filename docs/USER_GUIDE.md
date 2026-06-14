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

## Prompt Editor

The prompt editor is intentionally compact. It shows only your dictated or typed instruction, not the hidden Simple Job runner instructions. Use **Expand editor** for long prompts and **Collapse prompt** to give the result area more room. The prompt collapses automatically when a run starts.

## Task Modes

Simple Job Runner auto-selects a mode from the prompt. Use the mode selector near **Run Simple Job** to override it:

- **Text Query**: direct answers, status checks, local queries, calculations, inventories, and lists. This mode prefers the `read-only` sandbox and can succeed without generated files.
- **File Job**: generated files, conversions, reports, spreadsheets, CSVs, Markdown, PDFs, and document jobs. This mode uses `workspace-write` and expects requested deliverables in `outputs`.
- **External Action**: GitHub repositories, uploads, publishing, sending data, or calls to external services. The app asks for confirmation before starting.
- **Admin / Sensitive**: deletes, system configuration, services, package installation, elevation, or other sensitive changes. The app asks for confirmation before starting.

## File Jobs

Use **Add files**, **Add folder**, drag and drop, or command-line file paths. The app copies selected inputs to the run folder before invoking Codex.

## Text Results Vs. File Results

Every run should produce a text result. Simple questions and status checks can complete successfully with no generated files.

The **Text Result** tab shows the final useful answer captured from `summary.md`. Use **Copy text**, **Save text as .txt**, **Save text as .md**, or **Clear result** from that tab.

The **Files** tab lists generated files only when they exist. File jobs should still include a short text result summarizing what was created.

## Output Files

Final outputs are copied to the configured output folder. The default is:

```text
%USERPROFILE%\Documents\Simple Job Outputs
```

Use **Forget this job** to delete the disposable run folder. Final copied outputs are kept.
