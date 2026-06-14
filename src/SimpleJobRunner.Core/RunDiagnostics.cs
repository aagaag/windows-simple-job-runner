using System.IO.Compression;
using System.Text;
using SimpleJobRunner.Core.Security;

namespace SimpleJobRunner.Core;

public sealed class RunDiagnostics
{
    public async Task AppendAsync(RunContext run, string message, IEnumerable<string?>? additionalSecrets, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(run);
        Directory.CreateDirectory(run.RunRoot);
        var line = $"{DateTimeOffset.Now:O} {SecretMasker.Redact(message, additionalSecrets)}{Environment.NewLine}";
        await File.AppendAllTextAsync(run.DiagnosticsPath, line, Encoding.UTF8, ct);
    }

    public async Task<string> ReadAsync(RunContext run, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(run);
        if (!File.Exists(run.DiagnosticsPath))
        {
            return string.Empty;
        }

        return await File.ReadAllTextAsync(run.DiagnosticsPath, ct);
    }

    public async Task ExportBundleAsync(
        RunContext run,
        string destinationZipPath,
        string progressTimeline,
        string appVersion,
        IEnumerable<string?>? additionalSecrets,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(run);
        Directory.CreateDirectory(Path.GetDirectoryName(destinationZipPath)!);
        if (File.Exists(destinationZipPath))
        {
            File.Delete(destinationZipPath);
        }

        await using var zipStream = File.Open(destinationZipPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Create);

        await AddFileIfExistsAsync(archive, run.MetadataPath, "run.json", additionalSecrets, ct);
        await AddFileIfExistsAsync(archive, run.SummaryPath, "summary.md", additionalSecrets, ct);
        await AddFileIfExistsAsync(archive, run.DiagnosticsPath, "diagnostics.log", additionalSecrets, ct);
        await AddTextAsync(archive, "progress.txt", progressTimeline, additionalSecrets, ct);
        await AddTextAsync(archive, "environment.txt", $"App version: {appVersion}{Environment.NewLine}Run folder: {run.RunRoot}{Environment.NewLine}", additionalSecrets, ct);
    }

    private static async Task AddFileIfExistsAsync(ZipArchive archive, string sourcePath, string entryName, IEnumerable<string?>? additionalSecrets, CancellationToken ct)
    {
        if (!File.Exists(sourcePath))
        {
            return;
        }

        var text = await File.ReadAllTextAsync(sourcePath, ct);
        await AddTextAsync(archive, entryName, text, additionalSecrets, ct);
    }

    private static async Task AddTextAsync(ZipArchive archive, string entryName, string text, IEnumerable<string?>? additionalSecrets, CancellationToken ct)
    {
        var entry = archive.CreateEntry(entryName);
        await using var stream = entry.Open();
        await using var writer = new StreamWriter(stream, Encoding.UTF8);
        await writer.WriteAsync(SecretMasker.Redact(text, additionalSecrets).AsMemory(), ct);
    }
}
