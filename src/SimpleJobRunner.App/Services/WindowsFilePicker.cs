using System.IO;
using Microsoft.Win32;
using Forms = System.Windows.Forms;

namespace SimpleJobRunner.App.Services;

public sealed class WindowsFilePicker : IFilePicker
{
    public Task<IReadOnlyList<string>> PickFilesAsync()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Multiselect = true,
            CheckFileExists = true,
            Title = "Add input files"
        };

        return Task.FromResult<IReadOnlyList<string>>(dialog.ShowDialog() == true ? dialog.FileNames : []);
    }

    public Task<string?> PickFolderAsync(string? initialPath)
    {
        using var dialog = new Forms.FolderBrowserDialog
        {
            Description = "Choose a folder",
            UseDescriptionForTitle = true,
            SelectedPath = Directory.Exists(initialPath) ? initialPath : string.Empty
        };

        return Task.FromResult(dialog.ShowDialog() == Forms.DialogResult.OK ? dialog.SelectedPath : null);
    }
}
