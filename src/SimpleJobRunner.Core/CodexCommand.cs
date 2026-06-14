namespace SimpleJobRunner.Core;

public sealed record CodexCommand(string FileName, IReadOnlyList<string> Arguments, string WorkingDirectory)
{
    public string DisplayArguments => string.Join(" ", Arguments.Select(QuoteIfNeeded));

    private static string QuoteIfNeeded(string value)
    {
        return value.Contains(' ') || value.Contains('\t')
            ? '"' + value.Replace("\"", "\\\"") + '"'
            : value;
    }
}
