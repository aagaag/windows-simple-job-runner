using System.IO;

namespace SimpleJobRunner.App.ViewModels;

public sealed record OutputItemViewModel(string FinalPath, long SizeBytes)
{
    public string Name => Path.GetFileName(FinalPath);
}
