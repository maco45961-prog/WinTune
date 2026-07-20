using System.Text.Json;
using WinBenchmark.Sessions;

namespace WinBenchmark.Integration;

public static class CoreStateReader
{
    public static SystemStateSnapshot? ReadState()
    {
        var path = FindStateFile();
        if (path == null) return null;

        try
        {
            var json = File.ReadAllText(path);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var snapshot = new SystemStateSnapshot
            {
                ProfileApplied = root.TryGetProperty("profile_applied", out var p) ? p.GetString() : null,
                Hostname = root.TryGetProperty("system_snapshot", out var ss)
                    ? (ss.TryGetProperty("hostname", out var hn) ? hn.GetString() : null)
                    : null,
                OsVersion = root.TryGetProperty("system_snapshot", out var ss2)
                    ? (ss2.TryGetProperty("os_version", out var ov) ? ov.GetString() : null)
                    : null,
                TotalRamGb = root.TryGetProperty("system_snapshot", out var ss3)
                    ? (ss3.TryGetProperty("total_ram_gb", out var ram) ? ram.GetDouble() : null)
                    : null,
                CpuName = root.TryGetProperty("system_snapshot", out var ss4)
                    ? (ss4.TryGetProperty("cpu_name", out var cpu) ? cpu.GetString() : null)
                    : null,
                ActiveTweaks = new List<string>(),
            };

            if (root.TryGetProperty("active_tweaks", out var tweaks))
            {
                foreach (var t in tweaks.EnumerateArray())
                    snapshot.ActiveTweaks.Add(t.GetString() ?? "");
            }

            return snapshot;
        }
        catch { return null; }
    }

    private static string? FindStateFile()
    {
        // Relative to app
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        var dir = new DirectoryInfo(appDir);

        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "winoptimizer-core", "shared", "system-state.json");
            if (File.Exists(candidate))
                return candidate;

            var candidate2 = Path.Combine(dir.FullName, "..", "..", "..", "..", "winoptimizer-core", "shared", "system-state.json");
            if (File.Exists(candidate2))
                return Path.GetFullPath(candidate2);

            dir = dir.Parent;
        }

        var env = Environment.GetEnvironmentVariable("WINOPTIMIZER_CORE_PATH");
        if (env != null)
        {
            var candidate = Path.Combine(env, "shared", "system-state.json");
            if (File.Exists(candidate)) return candidate;
        }

        return null;
    }

    public static string? FindCorePath()
    {
        var stateFile = FindStateFile();
        if (stateFile == null) return null;
        return Path.GetDirectoryName(Path.GetDirectoryName(stateFile));
    }
}
