using System.Windows;
using SimpleJobRunner.App.ViewModels;

namespace SimpleJobRunner.App;

public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _viewModel;

    public SettingsWindow(SettingsViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
        _viewModel.CloseRequested += Close;
    }

    private void ApiKeyBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        _viewModel.ApiKeyInput = ApiKeyBox.Password;
    }
}
