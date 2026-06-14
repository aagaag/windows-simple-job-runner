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
    string FinalOutputPath)
{
    public string MetadataPath => Path.Combine(RunRoot, "run.json");
    public string DiagnosticsPath => Path.Combine(RunRoot, "diagnostics.log");
}
