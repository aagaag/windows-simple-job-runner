# Privacy

- Audio is sent to OpenAI for transcription only when the user clicks **Record** and an OpenAI API key is configured.
- Codex CLI may send prompts and relevant file context to OpenAI to perform the task.
- Selected inputs are copied into local disposable run folders.
- The final text result is captured locally in the disposable run folder as `summary.md` and shown in the app.
- Run metadata is captured locally as `run.json`.
- Diagnostics can be written locally and exported without input files or output files by default.
- Final outputs are copied to the configured output folder.
- Run folders can be deleted with **Forget this job**.
- The app sends no telemetry unless explicitly added later.

Treat all selected input files as confidential. Do not use this app on data that should not be processed by the configured OpenAI/Codex account. Typed jobs can use existing Codex CLI authentication without a stored OpenAI API key.

Do not process patient-identifiable, client-confidential, medical, legal, financial, trade-secret, or regulated data unless your account, organization, jurisdiction, and policies permit it.
