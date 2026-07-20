using WinBenchmark.Capture;
using WinBenchmark.Integration;
using WinBenchmark.Sessions;

namespace WinBenchmark;

public delegate void MetricsUpdateHandler(SystemMetricsSnapshot snapshot);

public class BenchmarkRunner
{
    private CancellationTokenSource? _cts;
    private bool _isRunning;

    public event MetricsUpdateHandler? OnMetricsUpdate;
    public event Action<BenchmarkSession>? OnSessionComplete;

    public bool IsRunning => _isRunning;

    public async Task<BenchmarkSession> RunSession(
        int durationSeconds,
        string? targetProcess = null,
        IProgress<string>? progress = null)
    {
        _cts = new CancellationTokenSource();
        _isRunning = true;
        var token = _cts.Token;

        var session = new BenchmarkSession
        {
            DurationSeconds = durationSeconds,
            TargetProcess = targetProcess,
            StartTime = DateTime.UtcNow,
            SystemState = CoreStateReader.ReadState(),
        };

        progress?.Report("Inicializando métricas...");
        SystemMetrics.Initialize();

        var allSnapshots = new List<SystemMetricsSnapshot>();
        var pingTimer = Stopwatch.StartNew();
        var lastPingResult = new PingJitterResult();

        progress?.Report("Capturando métricas del sistema...");
        var sw = Stopwatch.StartNew();

        while (sw.Elapsed.TotalSeconds < durationSeconds && !token.IsCancellationRequested)
        {
            // Ping every 3 seconds
            if (pingTimer.Elapsed.TotalSeconds >= 3)
            {
                lastPingResult = await Task.Run(() => PingJitter.RunPingTest(), token);
                pingTimer.Restart();
            }

            var snap = SystemMetrics.CaptureSnapshot(
                lastPingResult.AvgPingMs,
                lastPingResult.JitterMs,
                lastPingResult.PacketLossPercent);

            allSnapshots.Add(snap);
            OnMetricsUpdate?.Invoke(snap);
            progress?.Report($"Capturando... {sw.Elapsed.TotalSeconds:F0}s / {durationSeconds}s");

            await Task.Delay(1000, token);
        }

        sw.Stop();

        progress?.Report("Capturando frames...");
        var frameResult = await FrameCapture.CaptureFrames(targetProcess ?? "notepad", durationSeconds);

        session.EndTime = DateTime.UtcNow;
        session.MetricSnapshots = allSnapshots;
        session.FrameTimesMs = frameResult.FrameTimesMs;
        session.FrameSource = frameResult.Source;

        // Aggregate
        session.AvgCpuPercent = Math.Round(allSnapshots.Average(s => s.CpuUsagePercent), 1);
        session.MaxCpuPercent = Math.Round(allSnapshots.Max(s => s.CpuUsagePercent), 1);
        session.AvgRamGb = Math.Round(allSnapshots.Average(s => s.RamUsedGb), 2);
        session.AvgPagefileGb = Math.Round(allSnapshots.Average(s => s.PagefileUsedGb), 2);
        session.AvgCpuTempC = allSnapshots.Any(s => s.CpuTempCelsius.HasValue)
            ? Math.Round(allSnapshots.Where(s => s.CpuTempCelsius.HasValue).Average(s => s.CpuTempCelsius!.Value), 1)
            : null;
        session.AvgGpuTempC = allSnapshots.Any(s => s.GpuTempCelsius.HasValue)
            ? Math.Round(allSnapshots.Where(s => s.GpuTempCelsius.HasValue).Average(s => s.GpuTempCelsius!.Value), 1)
            : null;
        session.AvgPingMs = Math.Round(allSnapshots.Average(s => s.PingMs), 1);
        session.AvgJitterMs = Math.Round(allSnapshots.Average(s => s.JitterMs), 1);
        session.PacketLossPercent = Math.Round(allSnapshots.Average(s => s.PacketLossPercent), 1);
        session.AvgFps = frameResult.AvgFps;
        session.Low1PercentFps = frameResult.Low1PercentFps;
        session.Low01PercentFps = frameResult.Low01PercentFps;
        session.AvgFrameTimeMs = frameResult.AvgFrameTimeMs;
        session.TotalFrames = frameResult.TotalFrames;

        if (allSnapshots.Count > 0)
        {
            session.PerCoreAvgPercent = new double[allSnapshots[0].CpuPerCorePercent.Length];
            for (int i = 0; i < session.PerCoreAvgPercent.Length; i++)
                session.PerCoreAvgPercent[i] = Math.Round(allSnapshots.Average(s =>
                    i < s.CpuPerCorePercent.Length ? s.CpuPerCorePercent[i] : 0), 1);
        }

        if (token.IsCancellationRequested)
        {
            session.DurationSeconds = (int)sw.Elapsed.TotalSeconds;
        }

        _isRunning = false;
        SessionStore.SaveSession(session);
        OnSessionComplete?.Invoke(session);

        return session;
    }

    public void Cancel()
    {
        _cts?.Cancel();
        _isRunning = false;
    }
}
