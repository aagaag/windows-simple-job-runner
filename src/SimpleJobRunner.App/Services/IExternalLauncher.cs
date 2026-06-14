namespace SimpleJobRunner.App.Services;

public interface IExternalLauncher
{
    void OpenFolder(string path);
    void OpenFile(string path);
}
