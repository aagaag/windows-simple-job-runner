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
        Assert.Contains("disposable one-off Simple Job task", text);
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

        var prompt = builder.BuildPrompt("Make a CSV", run, TaskMode.FileJob);

        Assert.Contains("Use the instructions in AGENTS.md.", prompt);
        Assert.Contains("Task mode:", prompt);
        Assert.Contains("File Job", prompt);
        Assert.Contains("User task:", prompt);
        Assert.Contains("./inbox", prompt);
        Assert.Contains("./outputs", prompt);
        Assert.Throws<InvalidOperationException>(() => builder.BuildPrompt("use " + FakeOpenAiKey(), run, TaskMode.TextQuery));
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
    public void CodexCommandBuilder_CanUseReadOnlySandboxForTextQueries()
    {
        var run = TestRun();
        var command = new CodexCommandBuilder().Build(run, CodexSandboxMode.ReadOnly);

        Assert.Contains("read-only", command.Arguments);
        Assert.DoesNotContain("workspace-write", command.Arguments);
    }

    [Fact]
    public void TaskModeClassifier_SelectsExpectedModes()
    {
        var classifier = new TaskModeClassifier();

        Assert.Equal(TaskMode.TextQuery, classifier.Classify("How much free space is available on the local disks?"));
        Assert.Equal(TaskMode.FileJob, classifier.Classify("Create a small CSV file in outputs."));
        Assert.Equal(TaskMode.ExternalAction, classifier.Classify("Create a private GitHub repository called simple-job-test."));
        Assert.Equal(TaskMode.AdminSensitive, classifier.Classify("Install packages and restart service."));
        Assert.Equal(TaskMode.ExternalAction, classifier.Classify("Delete the GitHub repo called old-project."));
        Assert.Equal(TaskMode.AdminSensitive, classifier.Classify(@"Delete all files in C:\Temp\OldBackups."));
    }

    [Fact]
    public async Task RunResultFactory_AllowsSuccessfulTextResultWithoutOutputFiles()
    {
        var run = TestRun();
        Directory.CreateDirectory(run.RunRoot);
        await File.WriteAllTextAsync(run.SummaryPath, "Drive | Free\nC: | 100 GB");

        var result = await new RunResultFactory().CreateAsync(
            run,
            TaskMode.TextQuery,
            [],
            ["codex exec --sandbox read-only"],
            [],
            [],
            [],
            CancellationToken.None);

        Assert.Equal(ResultType.Text, result.ResultType);
        Assert.Equal("Drive | Free\nC: | 100 GB", result.TextResult);
        Assert.Empty(result.OutputFiles);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public async Task RunResultFactory_CreatesHybridResultWhenTextAndFilesExist()
    {
        var run = TestRun();
        Directory.CreateDirectory(run.RunRoot);
        await File.WriteAllTextAsync(run.SummaryPath, "Created sample.csv with three rows.");
        var output = new OutputFile(
            Path.Combine(run.OutputsPath, "sample.csv"),
            Path.Combine(run.FinalOutputPath, "sample.csv"),
            32);

        var result = await new RunResultFactory().CreateAsync(
            run,
            TaskMode.FileJob,
            [output],
            ["codex exec --sandbox workspace-write"],
            [],
            [],
            [],
            CancellationToken.None);

        Assert.Equal(ResultType.Hybrid, result.ResultType);
        Assert.Equal("Created sample.csv with three rows.", result.TextResult);
        Assert.Single(result.OutputFiles);
    }

    [Fact]
    public async Task RunResultFactory_ProvidesFallbackTextWhenSummaryIsMissing()
    {
        var run = TestRun();
        Directory.CreateDirectory(run.RunRoot);
        var output = new OutputFile(
            Path.Combine(run.OutputsPath, "sample.csv"),
            Path.Combine(run.FinalOutputPath, "sample.csv"),
            32);

        var result = await new RunResultFactory().CreateAsync(
            run,
            TaskMode.FileJob,
            [output],
            [],
            [],
            [],
            [],
            CancellationToken.None);

        Assert.Equal(ResultType.Files, result.ResultType);
        Assert.Contains("created 1 output file", result.TextResult);
        Assert.Contains("summary.md was missing or empty.", result.Warnings);
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
    public void SecretMasker_RedactsTokensAndSensitiveAssignments()
    {
        var openAiLike = "sk-" + "test-secret-value";
        var standaloneGitHubLike = "ghp_" + "standalonetoken";
        var assignedGitHubLike = "ghp_" + "testtoken";
        var bearerValue = "abcdefgh" + "ijklmnop";
        var passwordValue = "super" + "secret";
        var argumentValue = "visible" + "value";
        var text =
            "Authorization: Bearer "
            + bearerValue
            + Environment.NewLine
            + "password="
            + passwordValue
            + Environment.NewLine
            + "--api-key "
            + argumentValue
            + Environment.NewLine
            + openAiLike
            + Environment.NewLine
            + standaloneGitHubLike
            + Environment.NewLine
            + "GITHUB_TOKEN="
            + assignedGitHubLike;

        var redacted = SecretMasker.Redact(text);

        Assert.DoesNotContain(openAiLike, redacted);
        Assert.DoesNotContain(standaloneGitHubLike, redacted);
        Assert.DoesNotContain(assignedGitHubLike, redacted);
        Assert.DoesNotContain(bearerValue, redacted);
        Assert.DoesNotContain(passwordValue, redacted);
        Assert.DoesNotContain(argumentValue, redacted);
        Assert.Contains("[redacted]", redacted);
    }

    [Fact]
    public async Task RunMetadataStore_WritesLifecycleMetadata()
    {
        var run = TestRun();
        Directory.CreateDirectory(run.RunRoot);
        var store = new RunMetadataStore(new FixedTimeProvider(new DateTimeOffset(2026, 6, 14, 12, 3, 4, TimeSpan.Zero)));
        var metadata = store.Create(run, TaskMode.TextQuery, CodexSandboxMode.ReadOnly, "0.3.0", 2, externalActionConfirmed: false);

        await store.WriteAsync(run, metadata, CancellationToken.None);
        await store.UpdateStatusAsync(run, metadata, RunStatus.Completed, CancellationToken.None);

        var saved = await store.ReadAsync(run, CancellationToken.None);
        Assert.NotNull(saved);
        Assert.Equal(run.Slug, saved.RunId);
        Assert.Equal(RunStatus.Completed, saved.Status);
        Assert.Equal(TaskMode.TextQuery, saved.Mode);
        Assert.Equal("read-only", saved.Sandbox);
        Assert.Equal(2, saved.InputFileCount);
        Assert.NotNull(saved.CompletedAt);
        Assert.True(File.Exists(run.MetadataPath));
    }

    [Fact]
    public async Task RunRetentionManager_CleansSuccessfulRunTransientFiles()
    {
        var run = TestRun();
        Directory.CreateDirectory(run.RunRoot);
        Directory.CreateDirectory(run.TempPath);
        Directory.CreateDirectory(run.FinalOutputPath);
        await File.WriteAllTextAsync(run.PromptPath, "prompt");
        await File.WriteAllTextAsync(run.TranscriptPath, "transcript");
        await File.WriteAllTextAsync(run.EventsPath, "events");
        await File.WriteAllTextAsync(run.SummaryPath, "summary");
        await File.WriteAllTextAsync(run.DiagnosticsPath, "diagnostics");
        await File.WriteAllTextAsync(Path.Combine(run.FinalOutputPath, "result.txt"), "kept");

        await new RunRetentionManager().CleanupSuccessfulRunAsync(
            run,
            new AppSettings
            {
                RetainPrompts = false,
                RetainTranscripts = false,
                DebugLogging = false
            },
            CancellationToken.None);

        Assert.False(Directory.Exists(run.TempPath));
        Assert.False(File.Exists(run.PromptPath));
        Assert.False(File.Exists(run.TranscriptPath));
        Assert.False(File.Exists(run.EventsPath));
        Assert.True(File.Exists(run.SummaryPath));
        Assert.True(File.Exists(run.DiagnosticsPath));
        Assert.True(File.Exists(Path.Combine(run.FinalOutputPath, "result.txt")));
    }

    [Fact]
    public async Task RunRetentionManager_PurgesOldRunFoldersOnly()
    {
        var stateRoot = Path.Combine(_root, "state");
        var runsRoot = Path.Combine(stateRoot, "runs");
        var oldRun = Path.Combine(runsRoot, "old");
        var newRun = Path.Combine(runsRoot, "new");
        Directory.CreateDirectory(oldRun);
        Directory.CreateDirectory(newRun);
        Directory.SetLastWriteTime(oldRun, new DateTime(2026, 6, 1));
        Directory.SetLastWriteTime(newRun, new DateTime(2026, 6, 14));

        var manager = new RunRetentionManager(
            stateRoot,
            new FixedTimeProvider(new DateTimeOffset(2026, 6, 14, 12, 0, 0, TimeSpan.Zero)));
        var deleted = await manager.PurgeOldRunsAsync(7, CancellationToken.None);

        Assert.Equal(1, deleted);
        Assert.False(Directory.Exists(oldRun));
        Assert.True(Directory.Exists(newRun));
    }

    [Fact]
    public async Task RunRetentionManager_PurgesOldDiagnosticsOnly()
    {
        var stateRoot = Path.Combine(_root, "state");
        var oldRun = Path.Combine(stateRoot, "runs", "old");
        var newRun = Path.Combine(stateRoot, "runs", "new");
        Directory.CreateDirectory(oldRun);
        Directory.CreateDirectory(newRun);
        var oldDiagnostics = Path.Combine(oldRun, "diagnostics.log");
        var newDiagnostics = Path.Combine(newRun, "diagnostics.log");
        await File.WriteAllTextAsync(oldDiagnostics, "old");
        await File.WriteAllTextAsync(newDiagnostics, "new");
        File.SetLastWriteTime(oldDiagnostics, new DateTime(2026, 6, 1));
        File.SetLastWriteTime(newDiagnostics, new DateTime(2026, 6, 14));

        var manager = new RunRetentionManager(
            stateRoot,
            new FixedTimeProvider(new DateTimeOffset(2026, 6, 14, 12, 0, 0, TimeSpan.Zero)));
        var deleted = await manager.PurgeOldDiagnosticsAsync(1, CancellationToken.None);

        Assert.Equal(1, deleted);
        Assert.False(File.Exists(oldDiagnostics));
        Assert.True(File.Exists(newDiagnostics));
        Assert.True(Directory.Exists(oldRun));
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
