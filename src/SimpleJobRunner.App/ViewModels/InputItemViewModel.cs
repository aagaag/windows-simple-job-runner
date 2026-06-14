using System.IO;

namespace SimpleJobRunner.App.ViewModels;

public sealed record InputItemViewModel(string Path)
{
    public string Name => Directory.Exists(Path)
        ? new DirectoryInfo(Path).Name
        : System.IO.Path.GetFileName(Path);
}
