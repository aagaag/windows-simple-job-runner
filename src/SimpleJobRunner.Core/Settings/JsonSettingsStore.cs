using System.Text.Json;

namespace SimpleJobRunner.Core.Settings;

public sealed class JsonSettingsStore : ISettingsStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    public JsonSettingsStore(string? localStateRoot = null)
    {
        var stateRoot = string.IsNullOrWhiteSpace(localStateRoot) ? AppPaths.LocalStateRoot : localStateRoot;
        SettingsPath = Path.Combine(stateRoot, "config", "settings.json");
    }

    public string SettingsPath { get; }

    public async Task<AppSettings> LoadAsync(CancellationToken ct)
    {
        if (!File.Exists(SettingsPath))
        {
            return new AppSettings();
        }

        await using var stream = File.OpenRead(SettingsPath);
        var settings = await JsonSerializer.DeserializeAsync<AppSettings>(stream, SerializerOptions, ct)
            ?? new AppSettings();
        settings.Normalize();
        return settings;
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Normalize();
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        await using var stream = File.Open(SettingsPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await JsonSerializer.SerializeAsync(stream, settings, SerializerOptions, ct);
    }
}
