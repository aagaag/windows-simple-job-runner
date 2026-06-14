namespace SimpleJobRunner.Core;

public sealed record RunContext(
    string Slug,
    string RunRoot,
    string InboxPath,
    string OutputsPath,
    string TempPath,
    string PromptPath,
    string SummaryPath,
    string TranscriptPath,
    string EventsPath,
    string FinalOutputPath);
