using System.IO;
using System.Diagnostics;

namespace PIblox.App.Services;

public static class LauncherService
{
    public static void LaunchPlayer(string? detectedExecutable)
    {
        if (TryLaunchProtocol("roblox://"))
            return;

        LaunchExecutable(detectedExecutable, "Roblox Player");
    }

    public static void LaunchStudio(string? detectedExecutable)
    {
        if (TryLaunchProtocol("roblox-studio://"))
            return;

        LaunchExecutable(detectedExecutable, "Roblox Studio");
    }

    public static void OpenWebPage(Uri uri)
    {
        if (uri.Scheme != Uri.UriSchemeHttps)
            throw new ArgumentException("Only HTTPS links are allowed.", nameof(uri));

        Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
    }

    private static bool TryLaunchProtocol(string protocolUri)
    {
        try
        {
            Process.Start(new ProcessStartInfo(protocolUri) { UseShellExecute = true });
            return true;
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return false;
        }
    }

    private static void LaunchExecutable(string? path, string displayName)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            throw new FileNotFoundException($"{displayName} was not found. Install it from the official Roblox website.");

        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }
}
