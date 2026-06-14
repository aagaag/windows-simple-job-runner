namespace SimpleJobRunner.Core;

public enum RunStatus
{
    Created = 0,
    Preparing = 1,
    Transcribing = 2,
    ReadyToRun = 3,
    RunningCodex = 4,
    CollectingOutputs = 5,
    Completed = 6,
    Failed = 7,
    Forgotten = 8
}
