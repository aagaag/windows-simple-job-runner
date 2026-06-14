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

## No Output Created

Simple Job Runner requires Codex to write deliverables to `./outputs`. Edit the prompt to name the expected file type and output, then rerun.

## Forget This Job Did Not Delete Final Outputs

That is expected. Forgetting a job deletes the disposable run folder, not the final copied outputs.
