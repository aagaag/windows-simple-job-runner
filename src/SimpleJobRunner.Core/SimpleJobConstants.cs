namespace SimpleJobRunner.Core;

public static class SimpleJobConstants
{
    public const string OpenAiCredentialResource = "SimpleJobRunner.OpenAI";
    public const string OpenAiCredentialUserName = "openai-api-key";

    public const string TranscriptionPromptHint =
        "The speaker may use English, German, French, Italian, Dutch, legal/business terminology, and technical terms such as Codex, GitHub, SharePoint, Icinga, Excel, AHV, ISAB, Scribble. Preserve the language spoken. Do not translate unless explicitly requested.";

    public const string RunInstructions = """
        # Simple Job run instructions

        This is a disposable one-off Simple Job task, not a software project.

        - Read source files from ./inbox.
        - Write final deliverables to ./outputs.
        - Use ./temp only for temporary scripts and intermediate files.
        - Do not modify files in ./inbox.
        - Do not delete user-provided files.
        - Do not initialize Git.
        - Do not create a local repository.
        - Do not create a GitHub repository.
        - Do not create branches, commits, pull requests, issues, README files, package files, CI workflows, app scaffolds, notebooks, source trees, or durable project structure unless the user explicitly asks.
        - Prefer built-in tools and already-installed software.
        - Do not install dependencies unless the user explicitly approves.
        - Never write secrets, API keys, tokens, passwords, or credentials into files.
        - Treat input files as confidential.
        - Do not upload files to third-party services unless explicitly approved by the user.
        - If the user asks for a simple answer, status check, local query, calculation, command result, inventory, or list, return the answer directly as the text result.
        - Do not create an output file unless the user asks for one.
        - If the user asks for a generated file, create it in ./outputs and summarize it in the text result.
        - If the job performs an external side effect, report exactly what was changed or created.
        - Before finishing a file-producing job, confirm that requested output files exist, are non-empty, and are in ./outputs.
        - End with a useful final text answer. Include final output paths when files were created, number of input files processed when relevant, failures, and assumptions.
        """;
}
