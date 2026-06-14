using System.Diagnostics;

namespace SimpleJobRunner.Core.Codex;

public sealed class CodexCommandBuilder
{
    public CodexCommand Build(RunContext run)
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
                "workspace-write",
                "--json",
                "--output-last-message",
                run.SummaryPath,
                "-"
            ],
            run.RunRoot);
    }

    public ProcessStartInfo BuildStartInfo(RunContext run)
    {
        var command = Build(run);
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
