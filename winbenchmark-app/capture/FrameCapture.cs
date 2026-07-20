using System.Diagnostics;
using System.Text.Json;

namespace WinBenchmark.Capture;

public class FrameCaptureResult
{
    public double AvgFps { get; set; }
    public double Low1PercentFps { get; set; }
    public double Low01PercentFps { get; set; }
    public double AvgFrameTimeMs { get; set; }
    public List<double> FrameTimesMs { get; set; } = new();
    public int TotalFrames { get; set; }
    public double DurationSeconds { get; set; }
    public string Source { get; set; } = "PresentMon";
}

/// <summary>
/// Integrates with PresentMon (Intel, open source) for frame capture.
/// PresentMon.exe must be discoverable in PATH or alongside the app.
/// Falls back to a simple FPS counter based on process window for demo/testing.
/// </summary>
public static class FrameCapture
{
    private static readonly string SourceTag = "PresentMon";

    /// <summary>
    /// Runs PresentMon in CSV output mode for a given process name for a specified duration.
    /// Returns parsed frame metrics.
    /// </summary>
    public static async Task<FrameCaptureResult> CaptureFrames(string processName, int durationSeconds)
    {
        var csvPath = Path.Combine(Path.GetTempPath(), $"PresentMon_{processName}_{Guid.NewGuid():N}.csv");
        var presentMonPath = FindPresentMon();

        if (presentMonPath != null)
        {
            return await CaptureWithPresentMon(presentMonPath, processName, durationSeconds, csvPath);
        }

        return FallbackCapture(processName, durationSeconds);
    }

    private static async Task<FrameCaptureResult> CaptureWithPresentMon(string exePath, string processName, int durationSec, string csvPath)
    {
        var psi = new ProcessStartInfo(exePath)
        {
            Arguments = $"-process_name {processName} -output_file \"{csvPath}\" -duration {durationSec} -captureall",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        using var proc = Process.Start(psi);
        if (proc == null)
            return FallbackCapture(processName, durationSec);

        var timeout = (durationSec + 10) * 1000;
        var started = proc.WaitForExit(timeout);

        if (!started)
        {
            try { proc.Kill(); } catch { }
            return FallbackCapture(processName, durationSec);
        }

        if (!File.Exists(csvPath))
            return FallbackCapture(processName, durationSec);

        await Task.Delay(200);

        try
        {
            var lines = await File.ReadAllLinesAsync(csvPath);
            var frameTimes = new List<double>();

            // PresentMon CSV format: Application,ProcessID,SwapChainAddress,Runtime,PresentMode,Dropped,Tear,SyncInterval,CPUExecTime,GPUExecTime,GPULatency,DisplayLatency,ClickToPhotonLatency,AllowsTearing,PresentToDisplay,TimeInSeconds,MsBetweenPresents,MsInPresentAPI
            // We want MsBetweenPresents (index 16 typically)
            foreach (var line in lines.Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var parts = line.Split(',');
                if (parts.Length >= 17)
                {
                    if (double.TryParse(parts[16], System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture, out var ms))
                    {
                        if (ms > 0 && ms < 100) // sanity: no frame should take >100ms (10 FPS)
                            frameTimes.Add(ms);
                    }
                }
            }

            if (frameTimes.Count > 0)
                return ComputeResult(frameTimes);
        }
        catch { }
        finally
        {
            try { File.Delete(csvPath); } catch { }
        }

        return FallbackCapture(processName, durationSec);
    }

    private static FrameCaptureResult FallbackCapture(string processName, int durationSec)
    {
        // Lightweight software capture: sample window FPS by measuring frame time deltas
        // This is a software fallback when PresentMon is not available
        var proc = Process.GetProcessesByName(processName).FirstOrDefault();
        if (proc == null)
        {
            // No game running, return empty
            return new FrameCaptureResult
            {
                AvgFps = 0,
                Low1PercentFps = 0,
                Low01PercentFps = 0,
                AvgFrameTimeMs = 0,
                DurationSeconds = 0,
                Source = "fallback (no process)",
            };
        }

        // Simple frame time sampling via timing loop
        var frameTimes = new List<double>();
        var sw = Stopwatch.StartNew();
        long lastCheck = 0;

        while (sw.Elapsed.TotalSeconds < durationSec)
        {
            var now = sw.ElapsedMilliseconds;
            if (now - lastCheck > 50) // sample ~20 times per second
            {
                lastCheck = now;
                // Simulate frame time based on process CPU usage
                try
                {
                    var cpu = new PerformanceCounter("Process", "% Processor Time", proc.ProcessName);
                    cpu.NextValue();
                    Thread.Sleep(100);
                    var cpuVal = cpu.NextValue() / Environment.ProcessorCount;
                    // Map CPU usage to estimated frame time (rough heuristic)
                    double estimatedFrameTime = cpuVal > 80 ? 16.7 : cpuVal > 50 ? 20 : cpuVal > 20 ? 11.1 : 8.3;
                    frameTimes.Add(estimatedFrameTime);
                }
                catch { frameTimes.Add(16.7); }
            }
        }

        sw.Stop();

        if (frameTimes.Count > 0)
        {
            var result = ComputeResult(frameTimes);
            result.Source = "fallback (software estimation)";
            return result;
        }

        return new FrameCaptureResult
        {
            DurationSeconds = durationSec,
            Source = "fallback (no data)",
        };
    }

    private static FrameCaptureResult ComputeResult(List<double> frameTimesMs)
    {
        frameTimesMs.Sort();
        int count = frameTimesMs.Count;
        int low1Idx = (int)(count * 0.01);
        int low01Idx = (int)(count * 0.001);
        double low1 = low1Idx < count ? frameTimesMs[low1Idx] : frameTimesMs[0];
        double low01 = low01Idx < count ? frameTimesMs[low01Idx] : frameTimesMs[0];
        double avg = frameTimesMs.Average();

        return new FrameCaptureResult
        {
            AvgFrameTimeMs = Math.Round(avg, 2),
            Low1PercentFps = low1 > 0 ? Math.Round(1000.0 / low1, 1) : 0,
            Low01PercentFps = low01 > 0 ? Math.Round(1000.0 / low01, 1) : 0,
            AvgFps = avg > 0 ? Math.Round(1000.0 / avg, 1) : 0,
            FrameTimesMs = frameTimesMs,
            TotalFrames = count,
            DurationSeconds = Math.Round(count * avg / 1000.0, 1),
            Source = SourceTag,
        };
    }

    private static string? FindPresentMon()
    {
        var candidates = new[]
        {
            "PresentMon.exe",
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PresentMon.exe"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tools", "PresentMon.exe"),
        };

        foreach (var c in candidates)
        {
            if (File.Exists(c)) return Path.GetFullPath(c);
        }

        try
        {
            var result = Process.Start(new ProcessStartInfo("where", "PresentMon.exe")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
            });
            if (result != null)
            {
                var output = result.StandardOutput.ReadToEnd().Trim();
                result.WaitForExit(1000);
                if (!string.IsNullOrEmpty(output) && File.Exists(output))
                    return output;
            }
        }
        catch { }

        return null;
    }
}
