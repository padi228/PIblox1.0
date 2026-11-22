using System.IO;
using System.Net.NetworkInformation;

namespace PIblox.App.Services;

public sealed record NetworkDiagnosticsSnapshot(
    bool WireGuardInstalled,
    string? WireGuardPath,
    int WireGuardAdapterCount,
    int ActiveWireGuardAdapterCount,
    IReadOnlyList<string> AdapterDetails,
    DateTimeOffset CheckedAt);

public sealed class NetworkDiagnosticsService
{
    private const string ExecutableName = "wireguard.exe";

    public NetworkDiagnosticsSnapshot Check()
    {
        var wireGuardPath = FindWireGuardExecutable();
        var adapters = FindWireGuardAdapters();

        return new NetworkDiagnosticsSnapshot(
            WireGuardInstalled: wireGuardPath is not null,
            WireGuardPath: wireGuardPath,
            WireGuardAdapterCount: adapters.Count,
            ActiveWireGuardAdapterCount: adapters.Count(adapter => adapter.OperationalStatus == OperationalStatus.Up),
            AdapterDetails: adapters.Select(adapter => $"{adapter.Name} — {adapter.OperationalStatus}").ToArray(),
            CheckedAt: DateTimeOffset.Now);
    }

    private static string? FindWireGuardExecutable()
    {
        var candidates = new List<string>();
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        if (!string.IsNullOrWhiteSpace(programFiles))
            candidates.Add(Path.Combine(programFiles, "WireGuard", ExecutableName));

        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        if (!string.IsNullOrWhiteSpace(programFilesX86))
            candidates.Add(Path.Combine(programFilesX86, "WireGuard", ExecutableName));

        var pathValue = Environment.GetEnvironmentVariable("PATH");
        if (!string.IsNullOrWhiteSpace(pathValue))
        {
            foreach (var directory in pathValue.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                try
                {
                    candidates.Add(Path.Combine(directory.Trim(), ExecutableName));
                }
                catch
                {
                    // Ignore malformed PATH entries and continue checking.
                }
            }
        }

        return candidates.Where(File.Exists)
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    private static List<NetworkInterface> FindWireGuardAdapters()
    {
        try
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .Where(adapter =>
                    adapter.Name.Contains("WireGuard", StringComparison.OrdinalIgnoreCase) ||
                    adapter.Description.Contains("WireGuard", StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
        catch (NetworkInformationException)
        {
            return [];
        }
    }
}
