namespace SimpleJobRunner.Core;

public sealed class RunMetadata
{
    public string RunId { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public RunStatus Status { get; set; } = RunStatus.Created;
    public TaskMode Mode { get; set; } = TaskMode.TextQuery;
    public string Sandbox { get; set; } = CodexSandboxMode.ReadOnly.ToCliValue();
    public string AppVersion { get; set; } = string.Empty;
    public int InputFileCount { get; set; }
    public int OutputFileCount { get; set; }
    public bool HasTextResult { get; set; }
    public bool ExternalActionRequested { get; set; }
    public bool ExternalActionConfirmed { get; set; }
    public int? CodexExitCode { get; set; }
    public string? ErrorSummary { get; set; }
}
