using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using SimpleJobRunner.App.Mvvm;
using SimpleJobRunner.App.Services;
using SimpleJobRunner.Core;
using SimpleJobRunner.Core.Runs;

namespace SimpleJobRunner.App.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly ISettingsStore _settingsStore;
    private readonly ICredentialStore _credentialStore;
    private readonly IFilePicker _filePicker;
    private readonly IMessageService _messageService;
    private readonly IExternalLauncher _launcher;
    private readonly IAudioRecorder _audioRecorder;
    private readonly ITranscriptionService _transcriptionService;
    private readonly ICodexAvailabilityService _codexAvailabilityService;
    private readonly IPromptBuilder _promptBuilder;
    private readonly ICodexRunner _codexRunner;
    private readonly IOutputCollector _outputCollector;

    private string _promptText = string.Empty;
    private string _statusMessage = "Ready.";
    private string _setupMessage = "Checking setup.";
    private string _outputRoot = AppPaths.DefaultOutputRoot;
    private bool _isBusy;
    private bool _isRecording;
    private RunContext? _currentRun;
    private OutputItemViewModel? _selectedOutput;

    public MainViewModel(
        ISettingsStore settingsStore,
        ICredentialStore credentialStore,
        IFilePicker filePicker,
        IMessageService messageService,
        IExternalLauncher launcher,
        IAudioRecorder audioRecorder,
        ITranscriptionService transcriptionService,
        ICodexAvailabilityService codexAvailabilityService,
        IPromptBuilder promptBuilder,
        ICodexRunner codexRunner,
        IOutputCollector outputCollector)
    {
        _settingsStore = settingsStore;
        _credentialStore = credentialStore;
        _filePicker = filePicker;
        _messageService = messageService;
        _launcher = launcher;
        _audioRecorder = audioRecorder;
        _transcriptionService = transcriptionService;
        _codexAvailabilityService = codexAvailabilityService;
        _promptBuilder = promptBuilder;
        _codexRunner = codexRunner;
        _outputCollector = outputCollector;

        AddFilesCommand = new AsyncRelayCommand(AddFilesAsync);
        AddFolderCommand = new AsyncRelayCommand(AddFolderAsync);
        ClearInputsCommand = new RelayCommand(() => Inputs.Clear());
        RecordCommand = new AsyncRelayCommand(StartRecordingAsync);
        StopRecordingCommand = new AsyncRelayCommand(StopRecordingAsync);
        RunJobCommand = new AsyncRelayCommand(RunJobAsync);
        ForgetJobCommand = new AsyncRelayCommand(ForgetJobAsync);
        SettingsCommand = new AsyncRelayCommand(OpenSettingsAsync);
        OpenOutputsFolderCommand = new RelayCommand(OpenOutputsFolder);
        OpenSelectedOutputCommand = new RelayCommand(OpenSelectedOutput);
    }

    public event Func<Task>? SettingsRequested;

    public ObservableCollection<InputItemViewModel> Inputs { get; } = [];
    public ObservableCollection<ProgressItemViewModel> ProgressItems { get; } = [];
    public ObservableCollection<OutputItemViewModel> Outputs { get; } = [];

    public ICommand AddFilesCommand { get; }
    public ICommand AddFolderCommand { get; }
    public ICommand ClearInputsCommand { get; }
    public ICommand RecordCommand { get; }
    public ICommand StopRecordingCommand { get; }
    public ICommand RunJobCommand { get; }
    public ICommand ForgetJobCommand { get; }
    public ICommand SettingsCommand { get; }
    public ICommand OpenOutputsFolderCommand { get; }
    public ICommand OpenSelectedOutputCommand { get; }

    public string PromptText
    {
        get => _promptText;
        set => SetProperty(ref _promptText, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public string SetupMessage
    {
        get => _setupMessage;
        private set => SetProperty(ref _setupMessage, value);
    }

    public string OutputRoot
    {
        get => _outputRoot;
        private set => SetProperty(ref _outputRoot, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                OnPropertyChanged(nameof(BusyVisibility));
            }
        }
    }

    public bool IsRecording
    {
        get => _isRecording;
        private set
        {
            if (SetProperty(ref _isRecording, value))
            {
                OnPropertyChanged(nameof(RecordButtonText));
            }
        }
    }

    public bool NeedsSetup { get; private set; }

    public string RecordButtonText => IsRecording ? "Recording" : "Record";

    public Visibility BusyVisibility => IsBusy ? Visibility.Visible : Visibility.Collapsed;

    public Visibility SetupBannerVisibility => NeedsSetup ? Visibility.Visible : Visibility.Collapsed;

    public OutputItemViewModel? SelectedOutput
    {
        get => _selectedOutput;
        set => SetProperty(ref _selectedOutput, value);
    }

    public async Task InitializeAsync(IEnumerable<string> initialPaths, CancellationToken ct)
    {
        AddInputPaths(initialPaths);
        await RefreshSetupStatusAsync(ct);
    }

    public void AddInputPaths(IEnumerable<string> paths)
    {
        foreach (var path in paths.Where(path => File.Exists(path) || Directory.Exists(path)))
        {
            var fullPath = Path.GetFullPath(path);
            if (Inputs.Any(item => string.Equals(item.Path, fullPath, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            Inputs.Add(new InputItemViewModel(fullPath));
        }
    }

    public async Task RefreshSetupStatusAsync(CancellationToken ct)
    {
        var settings = await _settingsStore.LoadAsync(ct);
        OutputRoot = settings.OutputRoot;
        var keyConfigured = !string.IsNullOrWhiteSpace(await _credentialStore.GetOpenAiApiKeyAsync(ct));
        var codex = await _codexAvailabilityService.CheckAsync(ct);

        NeedsSetup = !keyConfigured || !codex.SupportsRequiredExecFlags;
        OnPropertyChanged(nameof(SetupBannerVisibility));

        if (!codex.IsAvailable)
        {
            SetupMessage = "Setup needed: Codex CLI was not found on PATH.";
        }
        else if (!codex.SupportsRequiredExecFlags)
        {
            SetupMessage = "Setup needed: Codex CLI does not report all required non-interactive flags.";
        }
        else if (!keyConfigured)
        {
            SetupMessage = "Setup needed: add an OpenAI API key for speech-to-text.";
        }
        else
        {
            SetupMessage = $"Ready. Outputs: {OutputRoot}";
        }
    }

    public async Task OpenSettingsAsync()
    {
        if (SettingsRequested is not null)
        {
            await SettingsRequested.Invoke();
        }
    }

    private async Task AddFilesAsync()
    {
        AddInputPaths(await _filePicker.PickFilesAsync());
    }

    private async Task AddFolderAsync()
    {
        var folder = await _filePicker.PickFolderAsync(null);
        if (!string.IsNullOrWhiteSpace(folder))
        {
            AddInputPaths([folder]);
        }
    }

    private async Task StartRecordingAsync()
    {
        if (IsBusy || IsRecording)
        {
            return;
        }

        try
        {
            await _audioRecorder.StartAsync(CancellationToken.None);
            IsRecording = true;
            StatusMessage = "Recording.";
        }
        catch (Exception ex)
        {
            _messageService.ShowError("Recording failed", ex.Message);
        }
    }

    private async Task StopRecordingAsync()
    {
        if (!IsRecording)
        {
            return;
        }

        string? audioPath = null;
        try
        {
            IsBusy = true;
            audioPath = await _audioRecorder.StopAsync(CancellationToken.None);
            IsRecording = false;
            StatusMessage = "Transcribing.";
            var transcript = await _transcriptionService.TranscribeAsync(audioPath, SimpleJobConstants.TranscriptionPromptHint, CancellationToken.None);
            PromptText = string.IsNullOrWhiteSpace(PromptText) ? transcript : PromptText + Environment.NewLine + transcript;
            StatusMessage = "Transcript ready to edit.";
        }
        catch (Exception ex)
        {
            IsRecording = false;
            _messageService.ShowError("Transcription failed", ex.Message);
        }
        finally
        {
            IsBusy = false;
            if (!string.IsNullOrWhiteSpace(audioPath) && File.Exists(audioPath))
            {
                File.Delete(audioPath);
            }
        }
    }

    private async Task RunJobAsync()
    {
        if (IsBusy)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(PromptText))
        {
            _messageService.ShowError("Prompt required", "Type or transcribe a task before running a job.");
            return;
        }

        IsBusy = true;
        ProgressItems.Clear();
        Outputs.Clear();
        try
        {
            var settings = await _settingsStore.LoadAsync(CancellationToken.None);
            var codex = await _codexAvailabilityService.CheckAsync(CancellationToken.None);
            if (!codex.SupportsRequiredExecFlags)
            {
                throw new InvalidOperationException(codex.ErrorMessage ?? "Codex CLI is not ready.");
            }

            string? codexApiKey = null;
            if (settings.CodexAuthMode == CodexAuthMode.UseStoredOpenAiApiKey)
            {
                codexApiKey = await _credentialStore.GetOpenAiApiKeyAsync(CancellationToken.None);
                if (string.IsNullOrWhiteSpace(codexApiKey))
                {
                    throw new InvalidOperationException("OpenAI API key is missing.");
                }
            }

            var runManager = new RunFolderManager(finalOutputRoot: settings.OutputRoot);
            var run = await runManager.CreateRunAsync(PromptText, CancellationToken.None);
            _currentRun = run;
            AddProgress("Created run folder.");
            await runManager.StageFilesAsync(run, Inputs.Select(item => item.Path).ToArray(), CancellationToken.None);
            AddProgress($"Copied {Inputs.Count} input path(s).");
            await runManager.WriteRunInstructionsAsync(run, CancellationToken.None);
            AddProgress("Wrote run instructions.");

            if (settings.RetainTranscripts)
            {
                await File.WriteAllTextAsync(run.TranscriptPath, PromptText);
            }

            var builtPrompt = _promptBuilder.BuildPrompt(PromptText, run);
            await foreach (var codexEvent in _codexRunner.RunAsync(run, builtPrompt, new CodexRunOptions
                           {
                               UseStoredApiKey = settings.CodexAuthMode == CodexAuthMode.UseStoredOpenAiApiKey,
                               OpenAiApiKey = codexApiKey,
                               DebugLogging = settings.DebugLogging
                           }, CancellationToken.None))
            {
                if (!string.IsNullOrWhiteSpace(codexEvent.Message))
                {
                    AddProgress($"{codexEvent.EventType}: {codexEvent.Message}");
                }
            }

            var outputs = await _outputCollector.CollectOutputsAsync(run, CancellationToken.None);
            if (outputs.Count == 0)
            {
                throw new InvalidOperationException("Codex finished but did not create any output files.");
            }

            foreach (var output in outputs)
            {
                Outputs.Add(new OutputItemViewModel(output.FinalPath, output.SizeBytes));
            }

            DeleteDirectoryIfExists(run.TempPath);
            AddProgress($"Created {outputs.Count} output file(s).");
            StatusMessage = $"Done. Outputs: {run.FinalOutputPath}";
        }
        catch (Exception ex)
        {
            StatusMessage = "Job failed.";
            _messageService.ShowError("Job failed", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ForgetJobAsync()
    {
        if (_currentRun is null)
        {
            return;
        }

        if (!_messageService.Confirm("Forget this job", "Delete the disposable run folder? Final copied outputs will remain."))
        {
            return;
        }

        var manager = new RunFolderManager();
        await manager.CleanupAsync(_currentRun, CleanupMode.DeleteRunFolder, CancellationToken.None);
        AddProgress("Deleted disposable run folder.");
        StatusMessage = "Job forgotten. Final outputs were kept.";
        _currentRun = null;
    }

    private void OpenOutputsFolder()
    {
        _launcher.OpenFolder(_currentRun?.FinalOutputPath ?? OutputRoot);
    }

    private void OpenSelectedOutput()
    {
        if (SelectedOutput is null)
        {
            return;
        }

        _launcher.OpenFile(SelectedOutput.FinalPath);
    }

    private void AddProgress(string message)
    {
        ProgressItems.Add(new ProgressItemViewModel(DateTimeOffset.Now, message));
    }

    private static void DeleteDirectoryIfExists(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }
}
