namespace SimpleJobRunner.Core;

public static class AppPaths
{
    public static string LocalStateRoot =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SimpleJobRunner");

    public static string DefaultOutputRoot =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Simple Job Outputs");

    public static string DefaultAudioTempRoot => Path.Combine(LocalStateRoot, "temp");
}
