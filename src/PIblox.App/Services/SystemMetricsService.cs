using System.Runtime.InteropServices;

namespace PIblox.App.Services;

public sealed class SystemMetricsService
{
    private ulong? _previousIdle;
    private ulong? _previousTotal;

    public SystemMetrics Read()
    {
        var cpu = ReadCpuPercent();
        var memory = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
        var memoryAvailable = GlobalMemoryStatusEx(ref memory);

        return new SystemMetrics(
            cpu,
            memoryAvailable ? (double)memory.dwMemoryLoad : null,
            memoryAvailable ? memory.ullTotalPhys : null,
            memoryAvailable ? memory.ullAvailPhys : null);
    }

    private double? ReadCpuPercent()
    {
        if (!GetSystemTimes(out var idle, out var kernel, out var user))
            return null;

        var idleValue = idle.Value;
        var totalValue = kernel.Value + user.Value;

        if (_previousIdle.HasValue && _previousTotal.HasValue)
        {
            var idleDelta = idleValue - _previousIdle.Value;
            var totalDelta = totalValue - _previousTotal.Value;
            _previousIdle = idleValue;
            _previousTotal = totalValue;

            if (totalDelta == 0)
                return 0;

            return Math.Clamp(100.0 * (totalDelta - idleDelta) / totalDelta, 0, 100);
        }

        _previousIdle = idleValue;
        _previousTotal = totalValue;
        return null;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct FILETIME
    {
        public uint Low;
        public uint High;
        public ulong Value => ((ulong)High << 32) | Low;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSystemTimes(out FILETIME lpIdleTime, out FILETIME lpKernelTime, out FILETIME lpUserTime);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);
}

public sealed record SystemMetrics(double? CpuPercent, double? MemoryPercent, ulong? TotalMemoryBytes, ulong? AvailableMemoryBytes);
