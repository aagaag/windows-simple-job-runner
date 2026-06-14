# Security

Report security issues privately to the repository owner.

Simple Job Runner is designed to keep runtime secrets out of the repository and installers:

- Runtime OpenAI API keys are stored per Windows user in Windows Credential Locker.
- The app never stores the OpenAI API key in settings, prompts, transcripts, logs, installers, or GitHub history.
- `CODEX_API_KEY` is set only on a single `codex exec` child process when the user opts into that mode.
- Default tests do not call the OpenAI API.
- GitHub repository secrets are reserved for CI/CD concerns such as optional signing material.

Do not attach real API keys, input files, transcripts, or generated outputs to public issues.
