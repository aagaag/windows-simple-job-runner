namespace SimpleJobRunner.Core;

public enum CleanupMode
{
    SanitizeRunFolder = 0,
    DeleteRunFolder = 1,
    DeleteRunAndFinalOutputs = 2
}
