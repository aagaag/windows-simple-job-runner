namespace SimpleJobRunner.Core;

public sealed class RunResultFactory
{
    public async Task<RunResult> CreateAsync(
        RunContext run,
        TaskMode taskMode,
        IReadOnlyList<OutputFile> outputFiles,
        IReadOnlyList<string> commandsRun,
        IReadOnlyList<string> warnings,
        IReadOnlyList<string> assumptions,
        IReadOnlyList<string> filesNotProcessed,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(run);
        outputFiles ??= [];
        commandsRun ??= [];
        warnings ??= [];
        assumptions ??= [];
        filesNotProcessed ??= [];

        var textResult = await ReadTextResultAsync(run.SummaryPath, ct);
        var hasExplicitTextResult = !string.IsNullOrWhiteSpace(textResult);
        var effectiveWarnings = warnings.ToList();
        if (!hasExplicitTextResult)
        {
            textResult = outputFiles.Count > 0
                ? $"Job completed and created {outputFiles.Count} output file(s)."
                : "Job completed, but Codex did not return a text result.";
            effectiveWarnings.Add("summary.md was missing or empty.");
        }

        var resultType = DetermineResultType(taskMode, hasExplicitTextResult, outputFiles.Count);
        return new RunResult(
            resultType,
            textResult.Trim(),
            outputFiles,
            commandsRun,
            effectiveWarnings,
            assumptions,
            filesNotProcessed);
    }

    private static async Task<string> ReadTextResultAsync(string path, CancellationToken ct)
    {
        if (!File.Exists(path))
        {
            return string.Empty;
        }

        return await File.ReadAllTextAsync(path, ct);
    }

    private static ResultType DetermineResultType(TaskMode taskMode, bool hasExplicitTextResult, int outputFileCount)
    {
        if (taskMode == TaskMode.ExternalAction)
        {
            return ResultType.ExternalAction;
        }

        if (outputFileCount > 0 && hasExplicitTextResult)
        {
            return ResultType.Hybrid;
        }

        if (outputFileCount > 0)
        {
            return ResultType.Files;
        }

        return ResultType.Text;
    }
}
