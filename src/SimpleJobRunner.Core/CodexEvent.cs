namespace SimpleJobRunner.Core;

public sealed record CodexEvent(DateTimeOffset Timestamp, string EventType, string Message, string? RawJson = null)
{
    public static CodexEvent Progress(string message) =>
        new(DateTimeOffset.Now, "progress", message);

    public static CodexEvent Error(string message) =>
        new(DateTimeOffset.Now, "error", message);
}
