# Troubleshooting

## Codex CLI Missing

Install Codex CLI using the official OpenAI Codex instructions and make sure `codex` is available on `PATH`. Restart the app after changing `PATH`.

## API Key Missing

An OpenAI API key is not required for typed jobs that use existing Codex CLI authentication. It is required only for **Record** voice transcription or for Codex API-key mode.

To enable those features, open Settings, enter the OpenAI API key, click **Set / Replace key**, then **Test key**.

## Transcription Fails

Check internet access, API key validity, microphone permissions, and the selected speech model.

## Codex Fails

Run this from a terminal:

```powershell
codex exec --help
```

The app expects `codex exec` to support `--cd`, `--skip-git-repo-check`, `--ephemeral`, `--sandbox`, `--json`, and `--output-last-message`.

If you are using existing Codex CLI authentication, run:

```powershell
codex login
```

Use ChatGPT login if you want Codex to use your ChatGPT plan entitlement.

The Text Result tab shows a failure explanation with:

- What happened
- Likely reason
- What you can try next
- Technical details

The Details and Diagnostics tabs show run metadata, sanitized command summary, Codex exit code when available, and the diagnostics path.

## No Files Created

That can be expected for Text Query mode. A run is successful when the Text Result tab contains a useful answer, even if the Files tab is empty.

For File Job mode, Codex should write requested deliverables to `./outputs`. Edit the prompt to name the expected file type and output, then rerun.

## External Or Sensitive Confirmation Appears

The app asks for confirmation when a prompt looks like it may create something outside the run folder, upload or send data, modify system configuration, delete files, change services, install packages, or require elevation. Cancel if the side effect is not intended, or change the mode before running.

## Forget This Job Did Not Delete Final Outputs

That is expected when **Preserve output files when forgetting a job** is enabled in Settings. Disable that setting only if you intentionally want **Forget this job** to delete final copied outputs too.
