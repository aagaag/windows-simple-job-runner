using System.Collections.Generic;

namespace SimpleJobRunner.Core;

public interface ICredentialStore
{
    Task SaveOpenAiApiKeyAsync(string apiKey, CancellationToken ct);
    Task<string?> GetOpenAiApiKeyAsync(CancellationToken ct);
    Task DeleteOpenAiApiKeyAsync(CancellationToken ct);
}

public interface ITranscriptionService
{
    Task<string> TranscribeAsync(string audioPath, string promptHint, CancellationToken ct);
}

public interface IRunFolderManager
{
    Task<RunContext> CreateRunAsync(string? titleHint, CancellationToken ct);
    Task StageFilesAsync(RunContext run, IReadOnlyList<string> paths, CancellationToken ct);
    Task WriteRunInstructionsAsync(RunContext run, CancellationToken ct);
    Task CleanupAsync(RunContext run, CleanupMode mode, CancellationToken ct);
}

public interface IPromptBuilder
{
    string BuildPrompt(string userPrompt, RunContext run, TaskMode taskMode);
}

public interface ICodexRunner
{
    IAsyncEnumerable<CodexEvent> RunAsync(RunContext run, string prompt, CodexRunOptions options, CancellationToken ct);
}

public interface IOutputCollector
{
    Task<IReadOnlyList<OutputFile>> CollectOutputsAsync(RunContext run, CancellationToken ct);
}

public interface ISettingsStore
{
    string SettingsPath { get; }
    Task<AppSettings> LoadAsync(CancellationToken ct);
    Task SaveAsync(AppSettings settings, CancellationToken ct);
}

public interface ICodexAvailabilityService
{
    Task<CodexAvailability> CheckAsync(CancellationToken ct);
}

public interface IOpenAiApiKeyTester
{
    Task TestAsync(string apiKey, CancellationToken ct);
}
