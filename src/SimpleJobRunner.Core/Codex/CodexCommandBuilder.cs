using System.Diagnostics;

namespace SimpleJobRunner.Core.Codex;

public sealed class CodexCommandBuilder
{
    public CodexCommand Build(RunContext run, CodexSandboxMode sandboxMode = CodexSandboxMode.WorkspaceWrite)
    {
        ArgumentNullException.ThrowIfNull(run);

        return new CodexCommand(
            "codex",
            [
                "exec",
                "--cd",
                run.RunRoot,
                "--skip-git-repo-check",
                "--ephemeral",
                "--sandbox",
                sandboxMode.ToCliValue(),
                "--json",
                "--output-last-message",
                run.SummaryPath,
                "-"
            ],
            run.RunRoot);
    }

    public ProcessStartInfo BuildStartInfo(RunContext run, CodexSandboxMode sandboxMode = CodexSandboxMode.WorkspaceWrite)
    {
        var command = Build(run, sandboxMode);
        var startInfo = new ProcessStartInfo(command.FileName)
        {
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = command.WorkingDirectory
        };

        foreach (var argument in command.Arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        return startInfo;
    }
}
