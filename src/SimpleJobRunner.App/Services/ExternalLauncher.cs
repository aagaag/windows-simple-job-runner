using System.Diagnostics;
using System.IO;

namespace SimpleJobRunner.App.Services;

public sealed class ExternalLauncher : IExternalLauncher
{
    public void OpenFolder(string path)
    {
        Directory.CreateDirectory(path);
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }

    public void OpenFile(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Output file does not exist.", path);
        }

        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }
}
