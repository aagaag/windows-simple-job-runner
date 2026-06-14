using System.Net.Http;
using System.Windows;
using SimpleJobRunner.App.Services;
using SimpleJobRunner.App.ViewModels;
using SimpleJobRunner.Core.Codex;
using SimpleJobRunner.Core.Credentials;
using SimpleJobRunner.Core.Outputs;
using SimpleJobRunner.Core.Prompts;
using SimpleJobRunner.Core.Settings;
using SimpleJobRunner.Core.Transcription;

namespace SimpleJobRunner.App;

public partial class App : System.Windows.Application
{
    private readonly HttpClient _httpClient = new();

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var settingsStore = new JsonSettingsStore();
        var credentialStore = new WindowsCredentialStore();
        var filePicker = new WindowsFilePicker();
        var messageService = new WpfMessageService();
        var launcher = new ExternalLauncher();
        var recorder = new NAudioWavRecorder();
        var codexAvailability = new CodexAvailabilityService();
        var promptBuilder = new PromptBuilder();
        var codexRunner = new CodexRunner();
        var outputCollector = new OutputCollector();
        var transcription = new OpenAiTranscriptionService(
            _httpClient,
            credentialStore,
            async ct => (await settingsStore.LoadAsync(ct)).SpeechModel);
        var keyTester = new OpenAiApiKeyTester(_httpClient);

        var viewModel = new MainViewModel(
            settingsStore,
            credentialStore,
            filePicker,
            messageService,
            launcher,
            recorder,
            transcription,
            codexAvailability,
            promptBuilder,
            codexRunner,
            outputCollector);

        var mainWindow = new MainWindow(viewModel);
        viewModel.SettingsRequested += async () =>
        {
            var settingsViewModel = new SettingsViewModel(settingsStore, credentialStore, filePicker, messageService, keyTester);
            await settingsViewModel.LoadAsync(CancellationToken.None);
            var settingsWindow = new SettingsWindow(settingsViewModel) { Owner = mainWindow };
            settingsWindow.ShowDialog();
            await viewModel.RefreshSetupStatusAsync(CancellationToken.None);
        };

        mainWindow.Show();

        try
        {
            await viewModel.InitializeAsync(e.Args, CancellationToken.None);
            if (viewModel.NeedsSetup)
            {
                await viewModel.OpenSettingsAsync();
            }
        }
        catch (Exception ex)
        {
            messageService.ShowError("Startup failed", ex.Message);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _httpClient.Dispose();
        base.OnExit(e);
    }
}
