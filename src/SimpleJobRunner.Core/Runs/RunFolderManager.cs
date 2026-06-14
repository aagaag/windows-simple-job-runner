namespace SimpleJobRunner.Core.Runs;

public sealed class RunFolderManager : IRunFolderManager
{
    private readonly string _runsRoot;
    private readonly string _finalOutputRoot;
    private readonly TimeProvider _timeProvider;

    public RunFolderManager(string? localStateRoot = null, string? finalOutputRoot = null, TimeProvider? timeProvider = null)
    {
        var stateRoot = string.IsNullOrWhiteSpace(localStateRoot) ? AppPaths.LocalStateRoot : localStateRoot;
        _runsRoot = Path.Combine(stateRoot, "runs");
        _finalOutputRoot = string.IsNullOrWhiteSpace(finalOutputRoot) ? AppPaths.DefaultOutputRoot : finalOutputRoot;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public Task<RunContext> CreateRunAsync(string? titleHint, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var timestamp = _timeProvider.GetLocalNow().DateTime;
        var baseSlug = $"{timestamp:yyyy-MM-dd_HHmmss}_{Slugify(titleHint)}";
        var runRoot = GetUniqueDirectory(Path.Combine(_runsRoot, baseSlug));
        var slug = Path.GetFileName(runRoot);

        var inbox = Path.Combine(runRoot, "inbox");
        var outputs = Path.Combine(runRoot, "outputs");
        var temp = Path.Combine(runRoot, "temp");
        Directory.CreateDirectory(inbox);
        Directory.CreateDirectory(outputs);
        Directory.CreateDirectory(temp);

        var context = new RunContext(
            slug,
            runRoot,
            inbox,
            outputs,
            temp,
            Path.Combine(runRoot, "prompt.txt"),
            Path.Combine(runRoot, "summary.md"),
            Path.Combine(runRoot, "transcript.txt"),
            Path.Combine(runRoot, "events.jsonl"),
            Path.Combine(_finalOutputRoot, slug));

        return Task.FromResult(context);
    }

    public async Task StageFilesAsync(RunContext run, IReadOnlyList<string> paths, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(run);
        Directory.CreateDirectory(run.InboxPath);

        foreach (var path in paths.Select(Path.GetFullPath).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            ct.ThrowIfCancellationRequested();
            if (File.Exists(path))
            {
                var destination = GetUniquePath(Path.Combine(run.InboxPath, Path.GetFileName(path)));
                File.Copy(path, destination, overwrite: false);
                continue;
            }

            if (Directory.Exists(path))
            {
                var destinationRoot = GetUniqueDirectory(Path.Combine(run.InboxPath, new DirectoryInfo(path).Name));
                Directory.CreateDirectory(destinationRoot);
                await CopyDirectoryAsync(path, destinationRoot, ct);
                continue;
            }

            throw new FileNotFoundException("Input path does not exist.", path);
        }
    }

    public Task WriteRunInstructionsAsync(RunContext run, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(run);
        return File.WriteAllTextAsync(Path.Combine(run.RunRoot, "AGENTS.md"), SimpleJobConstants.RunInstructions, ct);
    }

    public Task CleanupAsync(RunContext run, CleanupMode mode, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(run);
        ct.ThrowIfCancellationRequested();

        if (mode == CleanupMode.DeleteRunAndFinalOutputs)
        {
            DeleteDirectoryIfExists(run.FinalOutputPath);
        }

        if (mode is CleanupMode.DeleteRunFolder or CleanupMode.DeleteRunAndFinalOutputs)
        {
            DeleteDirectoryIfExists(run.RunRoot);
            return Task.CompletedTask;
        }

        DeleteDirectoryIfExists(run.InboxPath);
        DeleteDirectoryIfExists(run.TempPath);
        DeleteDirectoryIfExists(run.OutputsPath);
        DeleteFileIfExists(run.PromptPath);
        DeleteFileIfExists(run.TranscriptPath);
        DeleteFileIfExists(run.EventsPath);
        DeleteFileIfExists(run.SummaryPath);
        return Task.CompletedTask;
    }

    private static async Task CopyDirectoryAsync(string sourceRoot, string destinationRoot, CancellationToken ct)
    {
        foreach (var directory in Directory.EnumerateDirectories(sourceRoot, "*", SearchOption.AllDirectories))
        {
            ct.ThrowIfCancellationRequested();
            var relative = Path.GetRelativePath(sourceRoot, directory);
            Directory.CreateDirectory(Path.Combine(destinationRoot, relative));
        }

        foreach (var file in Directory.EnumerateFiles(sourceRoot, "*", SearchOption.AllDirectories))
        {
            ct.ThrowIfCancellationRequested();
            var relative = Path.GetRelativePath(sourceRoot, file);
            var destination = Path.Combine(destinationRoot, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            await using var sourceStream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read);
            await using var destinationStream = File.Open(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            await sourceStream.CopyToAsync(destinationStream, ct);
        }
    }

    private static string Slugify(string? titleHint)
    {
        var input = string.IsNullOrWhiteSpace(titleHint) ? "job" : titleHint.Trim();
        var characters = input
            .Select(ch => char.IsLetterOrDigit(ch) ? char.ToLowerInvariant(ch) : '-')
            .ToArray();

        var slug = string.Join('-', new string(characters).Split('-', StringSplitOptions.RemoveEmptyEntries));
        if (slug.Length > 48)
        {
            slug = slug[..48].Trim('-');
        }

        return string.IsNullOrWhiteSpace(slug) ? "job" : slug;
    }

    private static string GetUniqueDirectory(string requestedPath)
    {
        if (!Directory.Exists(requestedPath) && !File.Exists(requestedPath))
        {
            return requestedPath;
        }

        for (var i = 2; ; i++)
        {
            var candidate = $"{requestedPath}-{i}";
            if (!Directory.Exists(candidate) && !File.Exists(candidate))
            {
                return candidate;
            }
        }
    }

    private static string GetUniquePath(string requestedPath)
    {
        if (!File.Exists(requestedPath) && !Directory.Exists(requestedPath))
        {
            return requestedPath;
        }

        var directory = Path.GetDirectoryName(requestedPath)!;
        var name = Path.GetFileNameWithoutExtension(requestedPath);
        var extension = Path.GetExtension(requestedPath);
        for (var i = 2; ; i++)
        {
            var candidate = Path.Combine(directory, $"{name} ({i}){extension}");
            if (!File.Exists(candidate) && !Directory.Exists(candidate))
            {
                return candidate;
            }
        }
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
