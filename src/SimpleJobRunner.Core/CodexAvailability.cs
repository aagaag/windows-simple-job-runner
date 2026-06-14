namespace SimpleJobRunner.Core;

public sealed record CodexAvailability(
    bool IsAvailable,
    string? Version,
    IReadOnlyList<string> MissingRequiredFlags,
    string? ErrorMessage)
{
    public bool SupportsRequiredExecFlags => IsAvailable && MissingRequiredFlags.Count == 0;
}
