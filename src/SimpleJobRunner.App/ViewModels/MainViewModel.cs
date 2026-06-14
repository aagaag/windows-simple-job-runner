using System.Collections.ObjectModel;
using System.IO;
using System.Text;
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
    private readonly TaskModeClassifier _taskModeClassifier = new();
    private readonly RunResultFactory _runResultFactory = new();

    private string _promptText = string.Empty;
    private string _textResult = string.Empty;
    private string _detailsText = string.Empty;
    private string _statusMessage = "Ready.";
    private string _setupMessage = "Checking setup.";
    private string _outputRoot = AppPaths.DefaultOutputRoot;
    private bool _isBusy;
    private bool _isRecording;
    private bool _voiceInputAvailable;
    private bool _isPromptExpanded;
    private bool _isPromptCollapsed;
    private bool _taskModeManuallySelected;
    private int _selectedResultTabIndex;
    private TaskModeChoice _selectedTaskMode;
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
        ExpandPromptCommand = new RelayCommand(() => IsPromptExpanded = true);
        CollapsePromptCommand = new RelayCommand(() => IsPromptCollapsed = true);
        AutoModeCommand = new RelayCommand(ResetModeToAuto);
        CopyTextCommand = new RelayCommand(CopyTextResult);
        SaveTextAsTxtCommand = new AsyncRelayCommand(() => SaveTextResultAsync(".txt"));
        SaveTextAsMdCommand = new AsyncRelayCommand(() => SaveTextResultAsync(".md"));
        ClearResultCommand = new RelayCommand(ClearResult);
        _selectedTaskMode = TaskModes[0];
    }

    public event Func<Task>? SettingsRequested;

    public ObservableCollection<InputItemViewModel> Inputs { get; } = [];
    public ObservableCollection<ProgressItemViewModel> ProgressItems { get; } = [];
    public ObservableCollection<OutputItemViewModel> Outputs { get; } = [];

    public IReadOnlyList<TaskModeChoice> TaskModes { get; } =
    [
        new(TaskMode.TextQuery, "Text Query"),
        new(TaskMode.FileJob, "File Job"),
        new(TaskMode.ExternalAction, "External Action"),
        new(TaskMode.AdminSensitive, "Admin / Sensitive")
    ];

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
    public ICommand ExpandPromptCommand { get; }
    public ICommand CollapsePromptCommand { get; }
    public ICommand AutoModeCommand { get; }
    public ICommand CopyTextCommand { get; }
    public ICommand SaveTextAsTxtCommand { get; }
    public ICommand SaveTextAsMdCommand { get; }
    public ICommand ClearResultCommand { get; }

    public string PromptText
    {
        get => _promptText;
        set
        {
            if (SetProperty(ref _promptText, value) && !_taskModeManuallySelected)
            {
                SetSelectedTaskMode(_taskModeClassifier.Classify(value), manual: false);
            }
        }
    }

    public string TextResult
    {
        get => _textResult;
        private set
        {
            if (SetProperty(ref _textResult, value))
            {
                OnPropertyChanged(nameof(HasTextResult));
            }
        }
    }

    public string DetailsText
    {
        get => _detailsText;
        private set => SetProperty(ref _detailsText, value);
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
                OnPropertyChanged(nameof(CanStartRecording));
                OnPropertyChanged(nameof(CanRunJob));
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
                OnPropertyChanged(nameof(CanStartRecording));
            }
        }
    }

    public bool NeedsSetup { get; private set; }

    public string RecordButtonText => IsRecording ? "Recording" : "Record";

    public TaskModeChoice SelectedTaskMode
    {
        get => _selectedTaskMode;
        set
        {
            if (value is not null)
            {
                SetSelectedTaskMode(value.Mode, manual: true);
            }
        }
    }

    public string ModeStatusText => _taskModeManuallySelected ? "Manual mode" : "Auto mode";

    public bool IsPromptExpanded
    {
        get => _isPromptExpanded;
        private set
        {
            if (SetProperty(ref _isPromptExpanded, value))
            {
                if (value)
                {
                    _isPromptCollapsed = false;
                    OnPropertyChanged(nameof(IsPromptCollapsed));
                    OnPropertyChanged(nameof(PromptEditorVisibility));
                }

                OnPropertyChanged(nameof(PromptEditorHeight));
                OnPropertyChanged(nameof(PromptEditorVisibility));
            }
        }
    }

    public bool IsPromptCollapsed
    {
        get => _isPromptCollapsed;
        private set
        {
            if (SetProperty(ref _isPromptCollapsed, value))
            {
                if (value)
                {
                    _isPromptExpanded = false;
                    OnPropertyChanged(nameof(IsPromptExpanded));
                    OnPropertyChanged(nameof(PromptEditorHeight));
                }

                OnPropertyChanged(nameof(PromptEditorVisibility));
            }
        }
    }

    public double PromptEditorHeight => IsPromptExpanded ? 240 : 112;

    public Visibility PromptEditorVisibility => IsPromptCollapsed ? Visibility.Collapsed : Visibility.Visible;

    public bool VoiceInputAvailable
    {
        get => _voiceInputAvailable;
        private set
        {
            if (SetProperty(ref _voiceInputAvailable, value))
            {
                OnPropertyChanged(nameof(CanStartRecording));
                OnPropertyChanged(nameof(RecordButtonToolTip));
            }
        }
    }

    public bool CanStartRecording => !IsBusy && !IsRecording && VoiceInputAvailable;

    public bool CanRunJob => !IsBusy;

    public string RecordButtonToolTip => VoiceInputAvailable
        ? "Record microphone audio, send it to OpenAI speech-to-text, insert the transcript into the prompt box, then let you edit before running. Audio is deleted after transcription."
        : "Voice transcription requires an OpenAI API key saved in Settings. You can still type a prompt and run jobs through existing Codex CLI authentication, including ChatGPT Pro.";

    public Visibility BusyVisibility => IsBusy ? Visibility.Visible : Visibility.Collapsed;

    public Visibility SetupBannerVisibility => NeedsSetup ? Visibility.Visible : Visibility.Collapsed;

    public int SelectedResultTabIndex
    {
        get => _selectedResultTabIndex;
        private set => SetProperty(ref _selectedResultTabIndex, value);
    }

    public bool HasTextResult => !string.IsNullOrWhiteSpace(TextResult);

    public Visibility EmptyFilesVisibility => Outputs.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

    public OutputItemViewModel? SelectedOutput
    {
        get => _selectedOutput;
        set => SetProperty(ref _selectedOutput, value);
    }

    public async Task InitializeAsync(IEnumerable<string> initialPaths, CancellationToken ct)
    {
        AddInputPaths(initialPaths);
        ResetModeToAuto();
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
        var setup = SetupStatusCalculator.Evaluate(settings, keyConfigured, codex);

        NeedsSetup = setup.NeedsSetup;
        VoiceInputAvailable = setup.VoiceInputAvailable;
        SetupMessage = setup.Message;
        OnPropertyChanged(nameof(SetupBannerVisibility));
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

        if (!VoiceInputAvailable)
        {
            _messageService.ShowError(
                "OpenAI API key required for voice",
                "Voice transcription uses the OpenAI speech-to-text API and needs an API key saved in Settings. You can still type a prompt and run Codex jobs through existing Codex CLI authentication.");
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

        var taskMode = SelectedTaskMode.Mode;
        if (taskMode is TaskMode.ExternalAction or TaskMode.AdminSensitive)
        {
            var message = _taskModeClassifier.ConfirmationMessage(taskMode, PromptText);
            if (!_messageService.Confirm(SelectedTaskMode.Label, message))
            {
                StatusMessage = "Run cancelled before external or sensitive action.";
                return;
            }
        }

        IsBusy = true;
        IsPromptCollapsed = true;
        SelectedResultTabIndex = 0;
        ProgressItems.Clear();
        Outputs.Clear();
        OnPropertyChanged(nameof(EmptyFilesVisibility));
        TextResult = string.Empty;
        DetailsText = string.Empty;
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

                AddProgress("Using stored OpenAI API key for this Codex child process only.");
            }
            else
            {
                AddProgress("Using existing Codex CLI authentication. The app is not setting CODEX_API_KEY.");
            }

            var sandboxMode = GetSandboxMode(taskMode);
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

            var builtPrompt = _promptBuilder.BuildPrompt(PromptText, run, taskMode);
            var commandsRun = new List<string>
            {
                $"codex exec --cd <run> --skip-git-repo-check --ephemeral --sandbox {sandboxMode.ToCliValue()} --json --output-last-message <run>\\summary.md -"
            };
            AddProgress($"Running Codex with {sandboxMode.ToCliValue()} sandbox.");
            await foreach (var codexEvent in _codexRunner.RunAsync(run, builtPrompt, new CodexRunOptions
                           {
                               UseStoredApiKey = settings.CodexAuthMode == CodexAuthMode.UseStoredOpenAiApiKey,
                               OpenAiApiKey = codexApiKey,
                               DebugLogging = settings.DebugLogging,
                               SandboxMode = sandboxMode
                           }, CancellationToken.None))
            {
                if (!string.IsNullOrWhiteSpace(codexEvent.Message))
                {
                    AddProgress($"{codexEvent.EventType}: {codexEvent.Message}");
                }
            }

            var outputs = await _outputCollector.CollectOutputsAsync(run, CancellationToken.None);
            foreach (var output in outputs)
            {
                Outputs.Add(new OutputItemViewModel(output.FinalPath, output.SizeBytes));
            }

            var warnings = new List<string>();
            if (taskMode == TaskMode.FileJob && outputs.Count == 0)
            {
                warnings.Add("File Job mode completed without generated files.");
            }

            var result = await _runResultFactory.CreateAsync(
                run,
                taskMode,
                outputs,
                commandsRun,
                warnings,
                [$"Task mode: {SelectedTaskMode.Label}"],
                [],
                CancellationToken.None);

            TextResult = result.TextResult;
            DetailsText = BuildDetailsText(result);
            OnPropertyChanged(nameof(EmptyFilesVisibility));
            DeleteDirectoryIfExists(run.TempPath);
            AddProgress(outputs.Count == 0 ? "Completed with text result and no generated files." : $"Created {outputs.Count} output file(s).");
            SelectedResultTabIndex = 0;
            StatusMessage = outputs.Count == 0
                ? "Done. Text result is ready."
                : $"Done. Text result and files are ready. Outputs: {run.FinalOutputPath}";
        }
        catch (Exception ex)
        {
            StatusMessage = "Job failed.";
            var failed = RunResult.Failed(ex.Message);
            TextResult = failed.TextResult;
            DetailsText = BuildDetailsText(failed);
            SelectedResultTabIndex = 0;
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

    private void ResetModeToAuto()
    {
        _taskModeManuallySelected = false;
        SetSelectedTaskMode(_taskModeClassifier.Classify(PromptText), manual: false);
    }

    private void SetSelectedTaskMode(TaskMode mode, bool manual)
    {
        var choice = TaskModes.First(item => item.Mode == mode);
        if (SetProperty(ref _selectedTaskMode, choice, nameof(SelectedTaskMode)))
        {
            OnPropertyChanged(nameof(ModeStatusText));
        }

        _taskModeManuallySelected = manual;
        OnPropertyChanged(nameof(ModeStatusText));
    }

    private static CodexSandboxMode GetSandboxMode(TaskMode mode)
    {
        return mode == TaskMode.TextQuery ? CodexSandboxMode.ReadOnly : CodexSandboxMode.WorkspaceWrite;
    }

    private void CopyTextResult()
    {
        if (!string.IsNullOrWhiteSpace(TextResult))
        {
            System.Windows.Clipboard.SetText(TextResult);
            StatusMessage = "Text result copied.";
        }
    }

    private async Task SaveTextResultAsync(string extension)
    {
        if (string.IsNullOrWhiteSpace(TextResult))
        {
            return;
        }

        var defaultName = $"simple-job-result-{DateTime.Now:yyyyMMdd-HHmmss}{extension}";
        var filter = extension.Equals(".md", StringComparison.OrdinalIgnoreCase)
            ? "Markdown files (*.md)|*.md|Text files (*.txt)|*.txt|All files (*.*)|*.*"
            : "Text files (*.txt)|*.txt|Markdown files (*.md)|*.md|All files (*.*)|*.*";
        var path = await _filePicker.PickSaveFileAsync(defaultName, filter);
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        await File.WriteAllTextAsync(path, TextResult);
        StatusMessage = $"Text result saved: {path}";
    }

    private void ClearResult()
    {
        TextResult = string.Empty;
        DetailsText = string.Empty;
        Outputs.Clear();
        ProgressItems.Clear();
        OnPropertyChanged(nameof(EmptyFilesVisibility));
        StatusMessage = "Result cleared.";
    }

    private static string BuildDetailsText(RunResult result)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Result type: {FormatResultType(result.ResultType)}");
        builder.AppendLine();
        AppendSection(builder, "Commands run", result.CommandsRun);
        AppendSection(builder, "Warnings", result.Warnings);
        AppendSection(builder, "Assumptions", result.Assumptions);
        AppendSection(builder, "Files not processed", result.FilesNotProcessed);
        AppendSection(builder, "Output files", result.OutputFiles.Select(file => file.FinalPath).ToArray());
        return builder.ToString().TrimEnd();
    }

    private static void AppendSection(StringBuilder builder, string title, IReadOnlyList<string> values)
    {
        builder.AppendLine(title + ":");
        if (values.Count == 0)
        {
            builder.AppendLine("- None");
        }
        else
        {
            foreach (var value in values)
            {
                builder.AppendLine("- " + value);
            }
        }

        builder.AppendLine();
    }

    private static string FormatResultType(ResultType resultType)
    {
        return resultType switch
        {
            ResultType.Text => "text",
            ResultType.Files => "files",
            ResultType.Hybrid => "hybrid",
            ResultType.ExternalAction => "external_action",
            ResultType.Failed => "failed",
            _ => "text"
        };
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
