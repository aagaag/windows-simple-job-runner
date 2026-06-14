namespace SimpleJobRunner.Core;

public sealed class CodexRunOptions
{
    public bool UseStoredApiKey { get; init; }
    public string? OpenAiApiKey { get; init; }
    public bool DebugLogging { get; init; }
    public CodexSandboxMode SandboxMode { get; init; } = CodexSandboxMode.WorkspaceWrite;
}
