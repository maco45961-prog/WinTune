using System.Text.Json;

namespace WinNetTools.Integration;

public class SystemState
{
    public string? Version { get; set; }
    public string? LastUpdated { get; set; }
    public List<string> ActiveTweaks { get; set; } = new();
    public List<TweakHistoryEntry> TweakHistory { get; set; } = new();
    public string? ProfileApplied { get; set; }
    public SystemSnapshot? SystemSnapshot { get; set; }
}

public class TweakHistoryEntry
{
    public string? TweakId { get; set; }
    public string? Action { get; set; }
    public string? Timestamp { get; set; }
}

public class SystemSnapshot
{
    public string? Hostname { get; set; }
    public string? OsVersion { get; set; }
    public double? TotalRamGb { get; set; }
    public string? CpuName { get; set; }
    public bool? VanguardInstalled { get; set; }
    public bool? VbsEnabled { get; set; }
}

public static class CoreStateReader
{
    private static readonly string[] NetworkTweakIds =
    {
        "disable-nagle-algorithm",
        "optimize-dns"
    };

    public static SystemState? ReadState(string? stateFilePath = null)
    {
        if (stateFilePath == null)
        {
            var corePath = FindCorePath();
            if (corePath == null) return null;
            stateFilePath = Path.Combine(corePath, "shared", "system-state.json");
        }

        if (!File.Exists(stateFilePath)) return null;

        try
        {
            var json = File.ReadAllText(stateFilePath);
            return JsonSerializer.Deserialize<SystemState>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch { return null; }
    }

    public static List<string> GetAppliedNetworkTweaks(SystemState? state = null)
    {
        state ??= ReadState();
        if (state?.ActiveTweaks == null) return new List<string>();

        return state.ActiveTweaks
            .Where(t => NetworkTweakIds.Contains(t))
            .ToList();
    }

    public static List<string> GetMissingNetworkTweaks(SystemState? state = null)
    {
        state ??= ReadState();
        var applied = GetAppliedNetworkTweaks(state);
        return NetworkTweakIds.Where(t => !applied.Contains(t)).ToList();
    }

    private static string? FindCorePath()
    {
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        var dir = new DirectoryInfo(appDir);

        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "winoptimizer-core", "shared", "system-state.json");
            if (File.Exists(candidate))
                return dir.FullName;

            var candidate2 = Path.Combine(dir.FullName, "..", "winoptimizer-core", "shared", "system-state.json");
            if (File.Exists(candidate2))
                return Path.GetFullPath(Path.Combine(dir.FullName, "..", "winoptimizer-core"));

            dir = dir.Parent;
        }

        var fromEnv = Environment.GetEnvironmentVariable("WINOPTIMIZER_CORE_PATH");
        if (fromEnv != null && Directory.Exists(fromEnv))
            return fromEnv;

        return null;
    }
}
