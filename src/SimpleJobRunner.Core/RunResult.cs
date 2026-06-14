namespace SimpleJobRunner.Core;

public sealed record RunResult(
    ResultType ResultType,
    string TextResult,
    IReadOnlyList<OutputFile> OutputFiles,
    IReadOnlyList<string> CommandsRun,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Assumptions,
    IReadOnlyList<string> FilesNotProcessed)
{
    public static RunResult Failed(string message) =>
        new(ResultType.Failed, message, [], [], [message], [], []);
}
