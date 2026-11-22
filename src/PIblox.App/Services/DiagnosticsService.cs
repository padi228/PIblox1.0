using System.Text;

namespace PIblox.App.Services;

public static class DiagnosticsService
{
    public static string BuildReport(RobloxLocator locator)
    {
        var player = locator.FindPlayer();
        var studio = locator.FindStudio();
        var sb = new StringBuilder();

        sb.AppendLine("PIblox diagnostic report");
        sb.AppendLine("========================");
        sb.AppendLine($"Generated: {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}");
        sb.AppendLine($"App version: {typeof(DiagnosticsService).Assembly.GetName().Version}");
        sb.AppendLine($"Operating system: {Environment.OSVersion}");
        sb.AppendLine($"64-bit OS: {Environment.Is64BitOperatingSystem}");
        sb.AppendLine($".NET runtime: {Environment.Version}");
        sb.AppendLine($"Roblox Player: {(player is null ? "not detected" : player)}");
        sb.AppendLine($"Roblox Studio: {(studio is null ? "not detected" : studio)}");
        sb.AppendLine();
        sb.AppendLine("PIblox does not change Roblox files or system performance settings.");
        sb.AppendLine("This report contains local system and installation-path information.");

        return sb.ToString();
    }
}
