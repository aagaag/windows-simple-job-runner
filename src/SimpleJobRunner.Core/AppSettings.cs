namespace SimpleJobRunner.Core;

public sealed class AppSettings
{
    public string OutputRoot { get; set; } = AppPaths.DefaultOutputRoot;
    public string SpeechModel { get; set; } = "gpt-4o-transcribe";
    public CodexAuthMode CodexAuthMode { get; set; } = CodexAuthMode.ExistingCliAuth;
    public int RunRetentionDays { get; set; } = 7;
    public int DiagnosticsRetentionDays { get; set; } = 1;
    public bool DebugLogging { get; set; }
    public bool RetainTranscripts { get; set; }
    public bool RetainPrompts { get; set; }
    public bool AutoDeleteOldRuns { get; set; } = true;
    public bool PreserveOutputsOnForget { get; set; } = true;
    public bool PrivacyMode { get; set; }

    public void Normalize()
    {
        if (string.IsNullOrWhiteSpace(OutputRoot))
        {
            OutputRoot = AppPaths.DefaultOutputRoot;
        }

        if (SpeechModel is not "gpt-4o-transcribe" and not "gpt-4o-mini-transcribe")
        {
            SpeechModel = "gpt-4o-transcribe";
        }

        if (RunRetentionDays < 0)
        {
            RunRetentionDays = 0;
        }

        if (DiagnosticsRetentionDays < 1)
        {
            DiagnosticsRetentionDays = 1;
        }
    }
}

public enum CodexAuthMode
{
    ExistingCliAuth = 0,
    UseStoredOpenAiApiKey = 1
}
