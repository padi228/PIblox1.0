using Microsoft.Win32;
using System.IO;

namespace PIblox.App.Services;

public sealed class RobloxLocator
{
    public string? FindPlayer() => FindExecutable("RobloxPlayerBeta.exe", "RobloxPlayerLauncher.exe");
    public string? FindStudio() => FindExecutable("RobloxStudioBeta.exe");

    private static string? FindExecutable(params string[] executableNames)
    {
        var roots = new List<string>();
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        roots.Add(Path.Combine(local, "Roblox", "Versions"));
        roots.Add(Path.Combine(local, "Programs", "Roblox"));

        foreach (var root in roots.Where(Directory.Exists))
        {
            foreach (var name in executableNames)
            {
                try
                {
                    var hit = Directory.GetFiles(root, name, SearchOption.AllDirectories)
                        .FirstOrDefault(p => File.Exists(p));
                    if (hit is not null) return hit;
                }
                catch (UnauthorizedAccessException) { }
                catch (IOException) { }
            }
        }
        return null;
    }
}
