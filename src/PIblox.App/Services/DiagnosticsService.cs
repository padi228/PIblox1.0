using System.Text;

namespace PIblox.App.Services;

public static class DiagnosticsService
{
    public static string BuildReport(RobloxLocator locator)
    {
        var sb = new StringBuilder();
        sb.AppendLine("PIblox local diagnostics");
        sb.AppendLine($"Windows: {Environment.OSVersion}");
        sb.AppendLine($".NET: {Environment.Version}");
        sb.AppendLine($"Player: {(locator.FindPlayer() is null ? "not detected" : "detected")}");
        sb.AppendLine($"Studio: {(locator.FindStudio() is null ? "not detected" : "detected")}");
        sb.AppendLine("No files or system settings were modified.");
        return sb.ToString();
    }
}
