using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;

namespace WinBenchmark.Capture;

public class SystemMetricsSnapshot
{
    public DateTime Timestamp { get; set; }
    public double CpuUsagePercent { get; set; }
    public double[] CpuPerCorePercent { get; set; } = Array.Empty<double>();
    public double RamUsedGb { get; set; }
    public double RamPercent { get; set; }
    public double PagefileUsedGb { get; set; }
    public double? CpuTempCelsius { get; set; }
    public double? GpuTempCelsius { get; set; }
    public double PingMs { get; set; }
    public double JitterMs { get; set; }
    public double PacketLossPercent { get; set; }
}

public class SessionMetrics
{
    public List<SystemMetricsSnapshot> Snapshots { get; set; } = new();
    public double AvgCpu { get; set; }
    public double MaxCpu { get; set; }
    public double AvgRamGb { get; set; }
    public double AvgPingMs { get; set; }
    public double AvgJitterMs { get; set; }
    public double AvgFps { get; set; }
    public double Low1PercentFps { get; set; }
    public double Low01PercentFps { get; set; }
    public double AvgFrameTimeMs { get; set; }
    public double DurationSeconds { get; set; }
}

public static class SystemMetrics
{
    private static PerformanceCounter? _cpuCounter;
    private static PerformanceCounter[]? _coreCounters;
    private static PerformanceCounter? _ramCounter;
    private static PerformanceCounter? _pagefileCounter;
    private static int _coreCount;

    public static void Initialize()
    {
        try
        {
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _cpuCounter.NextValue();

            _coreCount = Environment.ProcessorCount;
            _coreCounters = new PerformanceCounter[_coreCount];
            for (int i = 0; i < _coreCount; i++)
            {
                _coreCounters[i] = new PerformanceCounter("Processor", "% Processor Time", i.ToString());
                _coreCounters[i].NextValue();
            }

            _ramCounter = new PerformanceCounter("Memory", "Available MBytes");
            _ramCounter.NextValue();

            _pagefileCounter = new PerformanceCounter("Paging File", "% Usage", "_Total");
            _pagefileCounter.NextValue();
        }
        catch { }
    }

    public static SystemMetricsSnapshot CaptureSnapshot(double ping, double jitter, double loss)
    {
        var snap = new SystemMetricsSnapshot
        {
            Timestamp = DateTime.UtcNow,
            PingMs = ping,
            JitterMs = jitter,
            PacketLossPercent = loss,
        };

        try
        {
            snap.CpuUsagePercent = Math.Round((_cpuCounter?.NextValue() ?? 0), 1);
        }
        catch { }

        try
        {
            snap.CpuPerCorePercent = _coreCounters?.Select(c => Math.Round(c.NextValue(), 1)).ToArray() ?? Array.Empty<double>();
        }
        catch { }

        try
        {
            long totalRamBytes = 0;
            using var searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
            foreach (var o in searcher.Get())
                totalRamBytes = Convert.ToInt64(o["TotalPhysicalMemory"]);
            double totalRamGb = totalRamBytes / (1024.0 * 1024 * 1024);

            double availableMb = _ramCounter?.NextValue() ?? 0;
            double usedGb = totalRamGb - (availableMb / 1024.0);
            snap.RamUsedGb = Math.Round(usedGb, 2);
            snap.RamPercent = totalRamGb > 0 ? Math.Round(usedGb / totalRamGb * 100, 1) : 0;
        }
        catch { }

        try
        {
            double totalRamGb = 0;
            using var searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
            foreach (var o in searcher.Get())
                totalRamGb = Convert.ToInt64(o["TotalPhysicalMemory"]) / (1024.0 * 1024 * 1024);

            double pagefileUsage = _pagefileCounter?.NextValue() ?? 0;
            snap.PagefileUsedGb = Math.Round(totalRamGb * pagefileUsage / 100.0, 2);
        }
        catch { }

        try
        {
            snap.CpuTempCelsius = ReadCpuTemp();
        }
        catch { }

        try
        {
            snap.GpuTempCelsius = ReadGpuTemp();
        }
        catch { }

        return snap;
    }

    private static double? ReadCpuTemp()
    {
        using var searcher = new ManagementObjectSearcher(
            @"root\WMI", "SELECT Temperature FROM MSAcpi_ThermalZoneTemperature");
        var temps = new List<double>();
        foreach (var o in searcher.Get())
        {
            if (o["Temperature"] is uint t)
                temps.Add((t - 2732) / 10.0);
        }
        return temps.Count > 0 ? Math.Round(temps.Max(), 1) : null;
    }

    private static double? ReadGpuTemp()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                @"root\Microsoft\Windows\Display", "SELECT CurrentTemperature FROM MSDisplay_DisplaySensor");
            foreach (var o in searcher.Get())
            {
                if (o["CurrentTemperature"] is uint t)
                    return Math.Round((t - 2732) / 10.0, 1);
            }
        }
        catch { }

        try
        {
            using var searcher2 = new ManagementObjectSearcher(
                @"root\CIMV2", "SELECT * FROM Win32_PerfFormattedData_GPU_GPUAdapterMemory");
            foreach (var o in searcher2.Get()) { }
        }
        catch { }

        return null;
    }
}
