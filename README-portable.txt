Simple Job Runner portable build

Run SimpleJobRunner.App.exe to start the app.

Prerequisites:
- Windows 11, or recent Windows 10 best effort.
- Codex CLI installed on PATH.
- Codex CLI authentication through codex login for typed jobs.
- Optional: an OpenAI API key for in-app voice transcription or Codex API-key mode.

The portable build stores user settings under %LOCALAPPDATA%\SimpleJobRunner
and stores the OpenAI API key in Windows Credential Locker under the current
Windows user. No runtime API key is included in this zip.
