using System.Diagnostics;
using System.Text;

namespace SimpleJobRunner.Core.Codex;

public sealed class CodexAvailabilityService : ICodexAvailabilityService
{
    private static readonly string[] RequiredExecFlags =
    [
        "--cd",
        "--skip-git-repo-check",
        "--ephemeral",
        "--sandbox",
        "--json",
        "--output-last-message"
    ];

    public async Task<CodexAvailability> CheckAsync(CancellationToken ct)
    {
        try
        {
            var version = await RunAndCaptureAsync("codex", ["--version"], ct);
            var help = await RunAndCaptureAsync("codex", ["exec", "--help"], ct);
            var missing = RequiredExecFlags
                .Where(flag => !help.Contains(flag, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            return new CodexAvailability(true, version.Trim(), missing, missing.Length == 0 ? null : "Codex CLI is missing required exec flags.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new CodexAvailability(false, null, RequiredExecFlags, ex.Message);
        }
    }

    private static async Task<string> RunAndCaptureAsync(string fileName, IReadOnlyList<string> arguments, CancellationToken ct)
    {
        var startInfo = new ProcessStartInfo(fileName)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException($"Could not start {fileName}.");
        var stdoutTask = process.StandardOutput.ReadToEndAsync(ct);
        var stderrTask = process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);

        var stdout = await stdoutTask;
        var stderr = await stderrTask;
        if (process.ExitCode != 0)
        {
            var detail = string.IsNullOrWhiteSpace(stderr) ? stdout : stderr;
            throw new InvalidOperationException($"{fileName} exited with code {process.ExitCode}: {detail.Trim()}");
        }

        return stdout;
    }
}
