# Secrets model

Simple Job Runner has two different kinds of secrets: runtime user secrets and CI/CD secrets.

## Runtime user secrets

The user's OpenAI API key is optional for typed jobs. It is required for in-app voice transcription and for Codex API-key mode. When configured, it is stored on each Windows computer in Windows Credential Locker under the current Windows user. It is not stored in this repository, app settings, prompt files, transcript files, logs, installers, or GitHub Actions.

## Codex CLI execution

Simple Job Runner can either rely on existing Codex CLI authentication or pass the stored API key to a single `codex exec` child process using the `CODEX_API_KEY` environment variable. Existing CLI authentication can use `codex login`, including ChatGPT login. When API-key mode is used, the variable is set only for the child process and is not persisted globally.

## GitHub repository secrets

GitHub repository secrets are only for CI/CD. They may include code-signing certificate material or an opt-in test API key. They must not include the production runtime API key that users enter into the app.

## What must never happen

- No API key in source control.
- No API key in installer packages.
- No API key in `.env` files.
- No API key in logs.
- No API key in prompt files.
- No API key in transcripts.
- No API key in crash reports.
- No full API key displayed after saving.
