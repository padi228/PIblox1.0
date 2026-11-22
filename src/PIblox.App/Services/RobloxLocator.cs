using System.IO;

namespace PIblox.App.Services;

public sealed class RobloxLocator
{
    private readonly string _versionsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Roblox", "Versions");

    public string? FindPlayer() => FindExecutable("RobloxPlayerBeta.exe", "RobloxPlayerLauncher.exe");
    public string? FindStudio() => FindExecutable("RobloxStudioBeta.exe");

    private string? FindExecutable(params string[] executableNames)
    {
        if (!Directory.Exists(_versionsDirectory))
            return null;

        IEnumerable<string> versionDirectories;
        try
        {
            versionDirectories = Directory.EnumerateDirectories(_versionsDirectory)
                .OrderByDescending(Directory.GetLastWriteTimeUtc)
                .ToArray();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }

        foreach (var directory in versionDirectories)
        {
            foreach (var name in executableNames)
            {
                var candidate = Path.Combine(directory, name);
                if (File.Exists(candidate))
                    return candidate;
            }
        }

        return null;
    }
}
