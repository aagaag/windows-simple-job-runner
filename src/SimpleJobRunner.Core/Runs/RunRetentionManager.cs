namespace SimpleJobRunner.Core.Runs;

public sealed class RunRetentionManager(string? localStateRoot = null, TimeProvider? timeProvider = null)
{
    private readonly string _runsRoot = Path.Combine(string.IsNullOrWhiteSpace(localStateRoot) ? AppPaths.LocalStateRoot : localStateRoot, "runs");
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public Task CleanupSuccessfulRunAsync(RunContext run, AppSettings settings, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(settings);
        ct.ThrowIfCancellationRequested();

        DeleteDirectoryIfExists(run.TempPath);
        if (!settings.RetainPrompts)
        {
            DeleteFileIfExists(run.PromptPath);
        }

        if (!settings.RetainTranscripts)
        {
            DeleteFileIfExists(run.TranscriptPath);
        }

        if (!settings.DebugLogging)
        {
            DeleteFileIfExists(run.EventsPath);
        }

        return Task.CompletedTask;
    }

    public Task<int> PurgeOldRunsAsync(int retentionDays, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (!Directory.Exists(_runsRoot))
        {
            return Task.FromResult(0);
        }

        var cutoff = _timeProvider.GetLocalNow().DateTime.AddDays(-Math.Max(0, retentionDays));
        var deleted = 0;
        foreach (var directory in Directory.EnumerateDirectories(_runsRoot))
        {
            ct.ThrowIfCancellationRequested();
            var info = new DirectoryInfo(directory);
            if (info.LastWriteTime <= cutoff)
            {
                info.Delete(recursive: true);
                deleted++;
            }
        }

        return Task.FromResult(deleted);
    }

    public Task<int> PurgeOldDiagnosticsAsync(int retentionDays, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (!Directory.Exists(_runsRoot))
        {
            return Task.FromResult(0);
        }

        var cutoff = _timeProvider.GetLocalNow().DateTime.AddDays(-Math.Max(1, retentionDays));
        var deleted = 0;
        foreach (var path in Directory.EnumerateFiles(_runsRoot, "diagnostics.log", SearchOption.AllDirectories)
                     .Concat(Directory.EnumerateFiles(_runsRoot, "events.jsonl", SearchOption.AllDirectories)))
        {
            ct.ThrowIfCancellationRequested();
            var info = new FileInfo(path);
            if (info.LastWriteTime <= cutoff)
            {
                info.Delete();
                deleted++;
            }
        }

        return Task.FromResult(deleted);
    }

    private static void DeleteDirectoryIfExists(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }

    private static void DeleteFileIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
