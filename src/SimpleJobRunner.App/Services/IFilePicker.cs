namespace SimpleJobRunner.App.Services;

public interface IFilePicker
{
    Task<IReadOnlyList<string>> PickFilesAsync();
    Task<string?> PickFolderAsync(string? initialPath);
    Task<string?> PickSaveFileAsync(string defaultFileName, string filter);
}
