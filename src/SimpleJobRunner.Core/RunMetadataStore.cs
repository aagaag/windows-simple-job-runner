using System.Text.Json;

namespace SimpleJobRunner.Core;

public sealed class RunMetadataStore(TimeProvider? timeProvider = null)
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public RunMetadata Create(
        RunContext run,
        TaskMode mode,
        CodexSandboxMode sandbox,
        string appVersion,
        int inputFileCount,
        bool externalActionConfirmed)
    {
        ArgumentNullException.ThrowIfNull(run);

        return new RunMetadata
        {
            RunId = run.Slug,
            CreatedAt = _timeProvider.GetUtcNow(),
            Status = RunStatus.Created,
            Mode = mode,
            Sandbox = sandbox.ToCliValue(),
            AppVersion = appVersion,
            InputFileCount = inputFileCount,
            ExternalActionRequested = mode == TaskMode.ExternalAction,
            ExternalActionConfirmed = externalActionConfirmed
        };
    }

    public async Task WriteAsync(RunContext run, RunMetadata metadata, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(metadata);
        Directory.CreateDirectory(run.RunRoot);
        await using var stream = File.Open(run.MetadataPath, FileMode.Create, FileAccess.Write, FileShare.Read);
        await JsonSerializer.SerializeAsync(stream, metadata, SerializerOptions, ct);
    }

    public async Task<RunMetadata?> ReadAsync(RunContext run, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(run);
        if (!File.Exists(run.MetadataPath))
        {
            return null;
        }

        await using var stream = File.OpenRead(run.MetadataPath);
        return await JsonSerializer.DeserializeAsync<RunMetadata>(stream, SerializerOptions, ct);
    }

    public async Task UpdateStatusAsync(RunContext run, RunMetadata metadata, RunStatus status, CancellationToken ct)
    {
        metadata.Status = status;
        if (status is RunStatus.Completed or RunStatus.Failed or RunStatus.Forgotten)
        {
            metadata.CompletedAt ??= _timeProvider.GetUtcNow();
        }

        await WriteAsync(run, metadata, ct);
    }
}
