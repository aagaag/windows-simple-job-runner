# Repository instructions for Codex

This repository builds Simple Job Runner, a Windows desktop utility for ephemeral one-off file jobs.

## Priorities

- Keep the app Windows-native and easy to install.
- Keep runtime jobs ephemeral.
- Keep secrets out of source code, logs, tests, prompts, transcripts, and installers.
- Prefer clear, boring implementation over clever abstractions.
- Keep WPF UI separate from core logic.
- Add tests for non-UI logic.

## Security rules

- Never commit API keys, tokens, passwords, certificates, or private keys.
- Never write secrets into example files.
- Use placeholders in docs.
- Runtime API keys belong in Windows Credential Locker.
- GitHub repository secrets are for CI/CD only.
- `CODEX_API_KEY` may be set only on the `codex exec` child process.

## Build rules

- Use PowerShell scripts in `scripts/` for repeatable commands.
- Keep docs updated when behavior changes.
- Run tests before finalizing changes.
- Do not add new dependencies without documenting why.

## User-experience rules

- The app must not create a Codex app project or sidebar entry.
- The app must not create per-job GitHub repositories.
- The app must not create Git repositories in run folders.
- The user must be able to edit transcribed text before running a job.
- Final outputs should be easy to find.
