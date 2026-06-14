using System.Windows.Input;
using SimpleJobRunner.App.Mvvm;
using SimpleJobRunner.App.Services;
using SimpleJobRunner.Core;
using SimpleJobRunner.Core.Security;

namespace SimpleJobRunner.App.ViewModels;

public sealed class SettingsViewModel : ObservableObject
{
    private readonly ISettingsStore _settingsStore;
    private readonly ICredentialStore _credentialStore;
    private readonly IFilePicker _filePicker;
    private readonly IMessageService _messageService;
    private readonly IOpenAiApiKeyTester _keyTester;

    private string _apiKeyInput = string.Empty;
    private string _maskedApiKey = "(not set)";
    private string _outputRoot = AppPaths.DefaultOutputRoot;
    private string _selectedSpeechModel = "gpt-4o-transcribe";
    private CodexAuthModeChoice _selectedCodexAuthMode;
    private int _runRetentionDays = 7;
    private bool _debugLogging;
    private bool _retainTranscripts;
    private string _statusMessage = string.Empty;

    public SettingsViewModel(
        ISettingsStore settingsStore,
        ICredentialStore credentialStore,
        IFilePicker filePicker,
        IMessageService messageService,
        IOpenAiApiKeyTester keyTester)
    {
        _settingsStore = settingsStore;
        _credentialStore = credentialStore;
        _filePicker = filePicker;
        _messageService = messageService;
        _keyTester = keyTester;
        _selectedCodexAuthMode = CodexAuthModes[0];

        SaveSettingsCommand = new AsyncRelayCommand(SaveSettingsAsync);
        SaveKeyCommand = new AsyncRelayCommand(SaveKeyAsync);
        DeleteKeyCommand = new AsyncRelayCommand(DeleteKeyAsync);
        TestKeyCommand = new AsyncRelayCommand(TestKeyAsync);
        BrowseOutputFolderCommand = new AsyncRelayCommand(BrowseOutputFolderAsync);
        CloseCommand = new RelayCommand(() => CloseRequested?.Invoke());
    }

    public event Action? CloseRequested;

    public IReadOnlyList<string> SpeechModels { get; } =
    [
        "gpt-4o-transcribe",
        "gpt-4o-mini-transcribe"
    ];

    public IReadOnlyList<CodexAuthModeChoice> CodexAuthModes { get; } =
    [
        new(CodexAuthMode.ExistingCliAuth, "Use existing Codex CLI authentication (ChatGPT login)"),
        new(CodexAuthMode.UseStoredOpenAiApiKey, "Pass stored OpenAI API key to codex exec")
    ];

    public ICommand SaveSettingsCommand { get; }
    public ICommand SaveKeyCommand { get; }
    public ICommand DeleteKeyCommand { get; }
    public ICommand TestKeyCommand { get; }
    public ICommand BrowseOutputFolderCommand { get; }
    public ICommand CloseCommand { get; }

    public string ApiKeyInput
    {
        get => _apiKeyInput;
        set => SetProperty(ref _apiKeyInput, value);
    }

    public string MaskedApiKey
    {
        get => _maskedApiKey;
        private set => SetProperty(ref _maskedApiKey, value);
    }

    public string OutputRoot
    {
        get => _outputRoot;
        set => SetProperty(ref _outputRoot, value);
    }

    public string SelectedSpeechModel
    {
        get => _selectedSpeechModel;
        set => SetProperty(ref _selectedSpeechModel, value);
    }

    public CodexAuthModeChoice SelectedCodexAuthMode
    {
        get => _selectedCodexAuthMode;
        set => SetProperty(ref _selectedCodexAuthMode, value);
    }

    public int RunRetentionDays
    {
        get => _runRetentionDays;
        set => SetProperty(ref _runRetentionDays, value);
    }

    public bool DebugLogging
    {
        get => _debugLogging;
        set => SetProperty(ref _debugLogging, value);
    }

    public bool RetainTranscripts
    {
        get => _retainTranscripts;
        set => SetProperty(ref _retainTranscripts, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public async Task LoadAsync(CancellationToken ct)
    {
        var settings = await _settingsStore.LoadAsync(ct);
        OutputRoot = settings.OutputRoot;
        SelectedSpeechModel = settings.SpeechModel;
        SelectedCodexAuthMode = CodexAuthModes.First(choice => choice.Mode == settings.CodexAuthMode);
        RunRetentionDays = settings.RunRetentionDays;
        DebugLogging = settings.DebugLogging;
        RetainTranscripts = settings.RetainTranscripts;
        MaskedApiKey = SecretMasker.MaskOpenAiKey(await _credentialStore.GetOpenAiApiKeyAsync(ct));
    }

    private async Task SaveSettingsAsync()
    {
        var settings = new AppSettings
        {
            OutputRoot = OutputRoot,
            SpeechModel = SelectedSpeechModel,
            CodexAuthMode = SelectedCodexAuthMode.Mode,
            RunRetentionDays = RunRetentionDays,
            DebugLogging = DebugLogging,
            RetainTranscripts = RetainTranscripts
        };

        await _settingsStore.SaveAsync(settings, CancellationToken.None);
        StatusMessage = "Settings saved.";
    }

    private async Task SaveKeyAsync()
    {
        if (string.IsNullOrWhiteSpace(ApiKeyInput))
        {
            _messageService.ShowError("API key required", "Enter an OpenAI API key first.");
            return;
        }

        await _credentialStore.SaveOpenAiApiKeyAsync(ApiKeyInput.Trim(), CancellationToken.None);
        MaskedApiKey = SecretMasker.MaskOpenAiKey(ApiKeyInput);
        ApiKeyInput = string.Empty;
        StatusMessage = "API key saved in Windows Credential Locker.";
    }

    private async Task DeleteKeyAsync()
    {
        await _credentialStore.DeleteOpenAiApiKeyAsync(CancellationToken.None);
        ApiKeyInput = string.Empty;
        MaskedApiKey = SecretMasker.MaskOpenAiKey(null);
        StatusMessage = "API key deleted.";
    }

    private async Task TestKeyAsync()
    {
        var key = string.IsNullOrWhiteSpace(ApiKeyInput)
            ? await _credentialStore.GetOpenAiApiKeyAsync(CancellationToken.None)
            : ApiKeyInput.Trim();

        if (string.IsNullOrWhiteSpace(key))
        {
            _messageService.ShowError("API key required", "Set or enter an API key before testing.");
            return;
        }

        try
        {
            await _keyTester.TestAsync(key, CancellationToken.None);
            StatusMessage = "API key test passed.";
        }
        catch (Exception ex)
        {
            _messageService.ShowError("API key test failed", ex.Message);
        }
    }

    private async Task BrowseOutputFolderAsync()
    {
        var folder = await _filePicker.PickFolderAsync(OutputRoot);
        if (!string.IsNullOrWhiteSpace(folder))
        {
            OutputRoot = folder;
        }
    }
}

public sealed record CodexAuthModeChoice(CodexAuthMode Mode, string Label)
{
    public override string ToString() => Label;
}
