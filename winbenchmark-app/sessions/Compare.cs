namespace WinBenchmark.Sessions;

public class ComparisonResult
{
    public BenchmarkSession? Before { get; set; }
    public BenchmarkSession? After { get; set; }
    public double DeltaFps { get; set; }
    public double DeltaLow1Percent { get; set; }
    public double DeltaLow01Percent { get; set; }
    public double DeltaFrameTimeMs { get; set; }
    public double DeltaCpuPercent { get; set; }
    public double DeltaRamGb { get; set; }
    public double DeltaPingMs { get; set; }
    public double DeltaJitterMs { get; set; }
    public bool SystemStateChanged { get; set; }
    public string Summary { get; set; } = "";
}

public static class SessionComparer
{
    public static ComparisonResult Compare(BenchmarkSession before, BenchmarkSession after)
    {
        var result = new ComparisonResult
        {
            Before = before,
            After = after,
            DeltaFps = Math.Round(after.AvgFps - before.AvgFps, 1),
            DeltaLow1Percent = Math.Round(after.Low1PercentFps - before.Low1PercentFps, 1),
            DeltaLow01Percent = Math.Round(after.Low01PercentFps - before.Low01PercentFps, 1),
            DeltaFrameTimeMs = Math.Round(after.AvgFrameTimeMs - before.AvgFrameTimeMs, 2),
            DeltaCpuPercent = Math.Round(after.AvgCpuPercent - before.AvgCpuPercent, 1),
            DeltaRamGb = Math.Round(after.AvgRamGb - before.AvgRamGb, 2),
            DeltaPingMs = Math.Round(after.AvgPingMs - before.AvgPingMs, 1),
            DeltaJitterMs = Math.Round(after.AvgJitterMs - before.AvgJitterMs, 1),
        };

        result.SystemStateChanged = !SessionsEqual(before.SystemState, after.SystemState);

        var lines = new List<string>();
        if (result.DeltaFps > 0)
            lines.Add($"FPS promedio subió de {before.AvgFps:F1} a {after.AvgFps:F1} ({result.DeltaFps:+0.0;-0.0} FPS)");
        else if (result.DeltaFps < 0)
            lines.Add($"FPS promedio bajó de {before.AvgFps:F1} a {after.AvgFps:F1} ({result.DeltaFps:+0.0;-0.0} FPS)");

        if (result.DeltaLow1Percent > 0)
            lines.Add($"1% low subió de {before.Low1PercentFps:F1} a {after.Low1PercentFps:F1} ({result.DeltaLow1Percent:+0.0;-0.0} FPS) — mejor microstutter");
        else if (result.DeltaLow1Percent < 0)
            lines.Add($"1% low bajó de {before.Low1PercentFps:F1} a {after.Low1PercentFps:F1} ({result.DeltaLow1Percent:+0.0;-0.0} FPS) — peor microstutter");

        if (result.DeltaPingMs < 0)
            lines.Add($"Ping mejoró de {before.AvgPingMs:F1} a {after.AvgPingMs:F1} ms ({result.DeltaPingMs:+0.0;-0.0} ms)");
        else if (result.DeltaPingMs > 0)
            lines.Add($"Ping empeoró de {before.AvgPingMs:F1} a {after.AvgPingMs:F1} ms ({result.DeltaPingMs:+0.0;-0.0} ms)");

        if (result.SystemStateChanged)
        {
            var beforeProfile = before.SystemState?.ProfileApplied ?? "(ninguno)";
            var afterProfile = after.SystemState?.ProfileApplied ?? "(ninguno)";
            lines.Add($"Estado del sistema cambió: \"{beforeProfile}\" → \"{afterProfile}\"");
        }

        result.Summary = string.Join("\n", lines);

        return result;
    }

    private static bool SessionsEqual(SystemStateSnapshot? a, SystemStateSnapshot? b)
    {
        if (a == null && b == null) return true;
        if (a == null || b == null) return false;
        return a.ProfileApplied == b.ProfileApplied &&
               a.ActiveTweaks.Count == b.ActiveTweaks.Count &&
               a.ActiveTweaks.OrderBy(x => x).SequenceEqual(b.ActiveTweaks.OrderBy(x => x));
    }
}
