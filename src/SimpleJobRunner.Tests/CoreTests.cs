using SimpleJobRunner.Core;
using SimpleJobRunner.Core.Codex;
using SimpleJobRunner.Core.Outputs;
using SimpleJobRunner.Core.Prompts;
using SimpleJobRunner.Core.Runs;
using SimpleJobRunner.Core.Security;
using SimpleJobRunner.Core.Settings;

namespace SimpleJobRunner.Tests;

public sealed class CoreTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "sjr-tests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public async Task CreateRunAsync_CreatesExpectedRunFoldersWithoutGit()
    {
        var manager = CreateManager();
        var run = await manager.CreateRunAsync("CSV summary", CancellationToken.None);

        Assert.True(Directory.Exists(run.InboxPath));
        Assert.True(Directory.Exists(run.OutputsPath));
        Assert.True(Directory.Exists(run.TempPath));
        Assert.Contains("2026-06-14_120304_csv-summary", run.RunRoot);
        Assert.False(Directory.Exists(Path.Combine(run.RunRoot, ".git")));
    }

    [Fact]
    public async Task StageFilesAsync_CopiesFilesAndFoldersWithoutChangingOriginals()
    {
        var sourceRoot = Path.Combine(_root, "source");
        Directory.CreateDirectory(sourceRoot);
        var sourceFile = Path.Combine(sourceRoot, "input.txt");
        await File.WriteAllTextAsync(sourceFile, "original");
        var folder = Path.Combine(sourceRoot, "folder");
        Directory.CreateDirectory(folder);
        await File.WriteAllTextAsync(Path.Combine(folder, "nested.txt"), "nested");

        var manager = CreateManager();
        var run = await manager.CreateRunAsync("job", CancellationToken.None);
        await manager.StageFilesAsync(run, [sourceFile, folder], CancellationToken.None);

        Assert.Equal("original", await File.ReadAllTextAsync(sourceFile));
        Assert.True(File.Exists(Path.Combine(run.InboxPath, "input.txt")));
        Assert.True(File.Exists(Path.Combine(run.InboxPath, "folder", "nested.txt")));
    }

    [Fact]
    public async Task WriteRunInstructionsAsync_WritesGuardrails()
    {
        var manager = CreateManager();
        var run = await manager.CreateRunAsync("job", CancellationToken.None);

        await manager.WriteRunInstructionsAsync(run, CancellationToken.None);

        var text = await File.ReadAllTextAsync(Path.Combine(run.RunRoot, "AGENTS.md"));
        Assert.Contains("disposable one-off file-production task", text);
        Assert.Contains("Do not initialize Git", text);
        Assert.Contains("Treat input files as confidential", text);
    }

    [Fact]
    public async Task CleanupAsync_DeleteRunFolder_PreservesFinalOutputs()
    {
        var manager = CreateManager();
        var run = await manager.CreateRunAsync("job", CancellationToken.None);
        Directory.CreateDirectory(run.FinalOutputPath);
        await File.WriteAllTextAsync(Path.Combine(run.FinalOutputPath, "result.txt"), "kept");

        await manager.CleanupAsync(run, CleanupMode.DeleteRunFolder, CancellationToken.None);

        Assert.False(Directory.Exists(run.RunRoot));
        Assert.True(File.Exists(Path.Combine(run.FinalOutputPath, "result.txt")));
    }

    [Fact]
    public void PromptBuilder_BuildsExpectedPromptAndRejectsSecrets()
    {
        var run = TestRun();
        var builder = new PromptBuilder();

        var prompt = builder.BuildPrompt("Make a CSV", run);

        Assert.Contains("Use the instructions in AGENTS.md.", prompt);
        Assert.Contains("User task:", prompt);
        Assert.Contains("./inbox", prompt);
        Assert.Contains("./outputs", prompt);
        Assert.Throws<InvalidOperationException>(() => builder.BuildPrompt("use " + FakeOpenAiKey(), run));
    }

    [Fact]
    public void CodexCommandBuilder_UsesRequiredSafeFlags()
    {
        var run = TestRun();
        var command = new CodexCommandBuilder().Build(run);

        Assert.Equal("codex", command.FileName);
        Assert.Equal("exec", command.Arguments[0]);
        Assert.Contains("--skip-git-repo-check", command.Arguments);
        Assert.Contains("--ephemeral", command.Arguments);
        Assert.Contains("--json", command.Arguments);
        Assert.Contains("--output-last-message", command.Arguments);
        Assert.Contains("workspace-write", command.Arguments);
        Assert.DoesNotContain("--dangerously-bypass-approvals-and-sandbox", command.Arguments);
        Assert.DoesNotContain("danger-full-access", command.Arguments);
    }

    [Fact]
    public void CodexJsonlParser_ParsesTypedEventsAndPlainText()
    {
        var parser = new CodexJsonlParser();

        var typed = parser.ParseLine("""{"type":"task_started","message":"running"}""");
        var plain = parser.ParseLine("not json");

        Assert.Equal("task_started", typed.EventType);
        Assert.Equal("running", typed.Message);
        Assert.Equal("stdout", plain.EventType);
    }

    [Fact]
    public async Task OutputCollector_CopiesNonEmptyOutputsAndRejectsEmptyFiles()
    {
        var run = TestRun();
        Directory.CreateDirectory(run.OutputsPath);
        await File.WriteAllTextAsync(Path.Combine(run.OutputsPath, "result.csv"), "a,b");

        var collector = new OutputCollector();
        var outputs = await collector.CollectOutputsAsync(run, CancellationToken.None);

        Assert.Single(outputs);
        Assert.True(File.Exists(Path.Combine(run.FinalOutputPath, "result.csv")));

        await File.WriteAllTextAsync(Path.Combine(run.OutputsPath, "empty.txt"), string.Empty);
        await Assert.ThrowsAsync<InvalidOperationException>(() => collector.CollectOutputsAsync(run, CancellationToken.None));
    }

    [Fact]
    public async Task JsonSettingsStore_DoesNotPersistSecrets()
    {
        var store = new JsonSettingsStore(_root);
        var settings = new AppSettings
        {
            OutputRoot = Path.Combine(_root, "outputs"),
            SpeechModel = "gpt-4o-mini-transcribe",
            CodexAuthMode = CodexAuthMode.UseStoredOpenAiApiKey,
            DebugLogging = true
        };

        await store.SaveAsync(settings, CancellationToken.None);

        var text = await File.ReadAllTextAsync(store.SettingsPath);
        Assert.DoesNotContain(FakeOpenAiKey(), text);
        Assert.False(SecretMasker.ContainsLikelyOpenAiKey(text));
    }

    [Fact]
    public async Task CredentialStore_CanBeTestedThroughAbstraction()
    {
        ICredentialStore store = new FakeCredentialStore();
        await store.SaveOpenAiApiKeyAsync(FakeOpenAiKey(), CancellationToken.None);

        Assert.Equal(FakeOpenAiKey(), await store.GetOpenAiApiKeyAsync(CancellationToken.None));

        await store.DeleteOpenAiApiKeyAsync(CancellationToken.None);
        Assert.Null(await store.GetOpenAiApiKeyAsync(CancellationToken.None));
    }

    [Fact]
    public void SecretMasker_MasksAndDetectsOpenAiKeys()
    {
        Assert.Equal("sk-...7890", SecretMasker.MaskOpenAiKey("sk-1234567890"));
        Assert.True(SecretMasker.ContainsLikelyOpenAiKey("value " + FakeOpenAiKey()));
    }

    [Fact]
    public void SetupStatusCalculator_AllowsTypedJobsWithoutOpenAiApiKeyWhenUsingCliAuth()
    {
        var status = SetupStatusCalculator.Evaluate(
            new AppSettings
            {
                OutputRoot = Path.Combine(_root, "outputs"),
                CodexAuthMode = CodexAuthMode.ExistingCliAuth
            },
            openAiApiKeyConfigured: false,
            ReadyCodex());

        Assert.False(status.NeedsSetup);
        Assert.False(status.VoiceInputAvailable);
        Assert.Contains("Ready for typed jobs", status.Message);
    }

    [Fact]
    public void SetupStatusCalculator_RequiresKeyWhenCodexApiKeyModeIsSelected()
    {
        var status = SetupStatusCalculator.Evaluate(
            new AppSettings
            {
                OutputRoot = Path.Combine(_root, "outputs"),
                CodexAuthMode = CodexAuthMode.UseStoredOpenAiApiKey
            },
            openAiApiKeyConfigured: false,
            ReadyCodex());

        Assert.True(status.NeedsSetup);
        Assert.False(status.VoiceInputAvailable);
        Assert.Contains("API-key mode", status.Message);
    }

    [Fact]
    public void SetupStatusCalculator_EnablesVoiceInputWhenOpenAiApiKeyIsSaved()
    {
        var status = SetupStatusCalculator.Evaluate(
            new AppSettings
            {
                OutputRoot = Path.Combine(_root, "outputs"),
                CodexAuthMode = CodexAuthMode.ExistingCliAuth
            },
            openAiApiKeyConfigured: true,
            ReadyCodex());

        Assert.False(status.NeedsSetup);
        Assert.True(status.VoiceInputAvailable);
        Assert.Contains("Voice transcription is enabled", status.Message);
    }

    private RunFolderManager CreateManager()
    {
        return new RunFolderManager(
            Path.Combine(_root, "state"),
            Path.Combine(_root, "final"),
            new FixedTimeProvider(new DateTimeOffset(2026, 6, 14, 12, 3, 4, TimeSpan.Zero)));
    }

    private static string FakeOpenAiKey() => "sk-" + "testsecretsecretsecretsecret";

    private static CodexAvailability ReadyCodex() => new(true, "codex-cli test", [], null);

    private RunContext TestRun()
    {
        var runRoot = Path.Combine(_root, "run");
        return new RunContext(
            "2026-06-14_120304_job",
            runRoot,
            Path.Combine(runRoot, "inbox"),
            Path.Combine(runRoot, "outputs"),
            Path.Combine(runRoot, "temp"),
            Path.Combine(runRoot, "prompt.txt"),
            Path.Combine(runRoot, "summary.md"),
            Path.Combine(runRoot, "transcript.txt"),
            Path.Combine(runRoot, "events.jsonl"),
            Path.Combine(_root, "final", "2026-06-14_120304_job"));
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }

    private sealed class FakeCredentialStore : ICredentialStore
    {
        private string? _value;

        public Task SaveOpenAiApiKeyAsync(string apiKey, CancellationToken ct)
        {
            _value = apiKey;
            return Task.CompletedTask;
        }

        public Task<string?> GetOpenAiApiKeyAsync(CancellationToken ct)
        {
            return Task.FromResult(_value);
        }

        public Task DeleteOpenAiApiKeyAsync(CancellationToken ct)
        {
            _value = null;
            return Task.CompletedTask;
        }
    }
}
