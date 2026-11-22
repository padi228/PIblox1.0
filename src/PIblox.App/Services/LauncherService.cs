using System.Diagnostics;

namespace PIblox.App.Services;

public static class LauncherService
{
    public static void Start(string executablePath)
    {
        if (!File.Exists(executablePath)) throw new FileNotFoundException("Executable was not found.", executablePath);
        Process.Start(new ProcessStartInfo(executablePath) { UseShellExecute = true });
    }
}
