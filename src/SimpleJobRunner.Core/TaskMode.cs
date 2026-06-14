namespace SimpleJobRunner.Core;

public enum TaskMode
{
    TextQuery = 0,
    FileJob = 1,
    ExternalAction = 2,
    AdminSensitive = 3
}

public sealed record TaskModeChoice(TaskMode Mode, string Label)
{
    public override string ToString() => Label;
}
