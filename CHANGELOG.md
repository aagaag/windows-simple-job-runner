# Changelog

## 0.3.1 - Installer Runtime Fix

- Published a higher-version MSI so Windows upgrades earlier broken 0.3.0 installs.
- Added package validation that fails if the self-contained Windows desktop runtime files are missing from the publish folder.
- Clarified installation troubleshooting for the .NET Desktop Runtime dialog.

## 0.3.0 - Skeptical Friend Hardening

- Added clearer app positioning and safety language.
- Added run lifecycle metadata in `run.json`.
- Improved diagnostics and error visibility with a Diagnostics tab and sanitized diagnostics export.
- Expanded secret redaction for OpenAI keys, GitHub tokens, bearer tokens, passwords, and sensitive assignments.
- Added retention and cleanup controls for prompts, transcripts, diagnostics, old runs, and forgotten jobs.
- Added mode-based confirmation copy for external and admin/sensitive actions.
- Added a safe first-run test prompt.
- Improved audit trail and command visibility in run details.
- Improved Windows installer/shareability documentation.

## 0.2.0

- Added a compact prompt editor with expand/collapse controls and automatic collapse when a run starts.
- Added first-class Text Result, Files, Progress, and Details tabs.
- Captured Codex final answers into `run\summary.md` with `--output-last-message` and rendered that text directly in the app.
- Added Text Query, File Job, External Action, and Admin / Sensitive modes with confirmation for external or sensitive actions.
- Allowed successful text-only runs with no generated output files.
- Added structured run result details with result type, commands, warnings, assumptions, and files not processed.
- Kept existing Windows MSI and portable zip packaging behavior.

## 0.1.0

- Made the OpenAI API key optional for typed jobs that use existing Codex CLI authentication.
- Added UI tooltips explaining voice transcription, Codex authentication modes, retention, outputs, and secret storage.
- Initial Windows WPF application scaffold.
- Added core run-folder, prompt, credential, transcription, Codex runner, output, cleanup, and settings services.
- Added unit tests for non-UI behavior.
- Added self-contained win-x64 publish, portable zip, WiX MSI packaging, and release artifacts with SHA256 hashes.
- Added GitHub Actions CI and release workflows.
