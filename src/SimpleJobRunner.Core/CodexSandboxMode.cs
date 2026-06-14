namespace SimpleJobRunner.Core;

public enum CodexSandboxMode
{
    ReadOnly = 0,
    WorkspaceWrite = 1
}

public static class CodexSandboxModeExtensions
{
    public static string ToCliValue(this CodexSandboxMode mode)
    {
        return mode switch
        {
            CodexSandboxMode.ReadOnly => "read-only",
            CodexSandboxMode.WorkspaceWrite => "workspace-write",
            _ => "workspace-write"
        };
    }
}
