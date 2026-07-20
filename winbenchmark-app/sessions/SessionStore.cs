using System.Text.Json;
using WinBenchmark.Capture;

namespace WinBenchmark.Sessions;

public class BenchmarkSession
{
    public string SessionId { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public DateTime EndTime { get; set; }
    public int DurationSeconds { get; set; }
    public string? TargetProcess { get; set; }

    // Metrics
    public double AvgCpuPercent { get; set; }
    public double MaxCpuPercent { get; set; }
    public double[] PerCoreAvgPercent { get; set; } = Array.Empty<double>();
    public double AvgRamGb { get; set; }
    public double AvgPagefileGb { get; set; }
    public double? AvgCpuTempC { get; set; }
    public double? AvgGpuTempC { get; set; }
    public double AvgPingMs { get; set; }
    public double AvgJitterMs { get; set; }
    public double PacketLossPercent { get; set; }
    public double AvgFps { get; set; }
    public double Low1PercentFps { get; set; }
    public double Low01PercentFps { get; set; }
    public double AvgFrameTimeMs { get; set; }
    public int TotalFrames { get; set; }

    // Snapshots for traceability
    public List<SystemMetricsSnapshot> MetricSnapshots { get; set; } = new();
    public List<double> FrameTimesMs { get; set; } = new();
    public string FrameSource { get; set; } = "";

    // System state at time of capture
    public SystemStateSnapshot? SystemState { get; set; }
}

public class SystemStateSnapshot
{
    public string? ProfileApplied { get; set; }
    public List<string> ActiveTweaks { get; set; } = new();
    public string? Hostname { get; set; }
    public string? OsVersion { get; set; }
    public double? TotalRamGb { get; set; }
    public string? CpuName { get; set; }
}

public static class SessionStore
{
    private static readonly string DataDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "WinBenchmark", "sessions");

    static SessionStore()
    {
        Directory.CreateDirectory(DataDir);
    }

    public static string SaveSession(BenchmarkSession session)
    {
        var path = Path.Combine(DataDir, $"session_{session.SessionId}.json");
        var json = JsonSerializer.Serialize(session, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
        return path;
    }

    public static BenchmarkSession? LoadSession(string sessionId)
    {
        var path = Path.Combine(DataDir, $"session_{sessionId}.json");
        if (!File.Exists(path)) return null;
        return JsonSerializer.Deserialize<BenchmarkSession>(File.ReadAllText(path));
    }

    public static List<string> ListSessionIds()
    {
        return Directory.GetFiles(DataDir, "session_*.json")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(n => n != null)
            .Select(n => n!.Replace("session_", ""))
            .OrderByDescending(id => id)
            .ToList();
    }

    public static List<BenchmarkSession> ListSessions()
    {
        return ListSessionIds()
            .Select(LoadSession)
            .Where(s => s != null)
            .Cast<BenchmarkSession>()
            .OrderByDescending(s => s.StartTime)
            .ToList();
    }

    public static void DeleteSession(string sessionId)
    {
        var path = Path.Combine(DataDir, $"session_{sessionId}.json");
        if (File.Exists(path)) File.Delete(path);
    }
}
