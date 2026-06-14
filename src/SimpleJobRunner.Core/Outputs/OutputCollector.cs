namespace SimpleJobRunner.Core.Outputs;

public sealed class OutputCollector : IOutputCollector
{
    public async Task<IReadOnlyList<OutputFile>> CollectOutputsAsync(RunContext run, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(run);
        Directory.CreateDirectory(run.FinalOutputPath);

        if (!Directory.Exists(run.OutputsPath))
        {
            return [];
        }

        var collected = new List<OutputFile>();
        foreach (var sourcePath in Directory.EnumerateFiles(run.OutputsPath, "*", SearchOption.AllDirectories))
        {
            ct.ThrowIfCancellationRequested();
            var info = new FileInfo(sourcePath);
            if (info.Length == 0)
            {
                throw new InvalidOperationException($"Output file is empty: {sourcePath}");
            }

            var relativePath = Path.GetRelativePath(run.OutputsPath, sourcePath);
            var finalPath = Path.Combine(run.FinalOutputPath, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(finalPath)!);

            await using var source = File.Open(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            await using var destination = File.Open(finalPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await source.CopyToAsync(destination, ct);
            collected.Add(new OutputFile(sourcePath, finalPath, info.Length));
        }

        return collected;
    }
}
