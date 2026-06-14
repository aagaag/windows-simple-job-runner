namespace SimpleJobRunner.App.Services;

public interface IAudioRecorder
{
    bool IsRecording { get; }
    Task<string> StartAsync(CancellationToken ct);
    Task<string> StopAsync(CancellationToken ct);
}
