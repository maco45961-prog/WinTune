using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using WinBenchmark.Capture;
using WinBenchmark.Sessions;

namespace WinBenchmark.UI.Views;

public partial class ActiveSessionView : Page
{
    private readonly BenchmarkRunner _runner = new();
    private BenchmarkSession? _lastSession;

    public ActiveSessionView()
    {
        InitializeComponent();
        _runner.OnMetricsUpdate += OnMetricsUpdate;
        _runner.OnSessionComplete += OnSessionComplete;
    }

    private void OnMetricsUpdate(SystemMetricsSnapshot snap)
    {
        Dispatcher.Invoke(() =>
        {
            LiveCpu.Text = $"{snap.CpuUsagePercent}%";
            LiveRam.Text = $"{snap.RamUsedGb:F1} GB";
            LivePing.Text = $"{snap.PingMs:F0} ms";
            LiveJitter.Text = $"{snap.JitterMs:F1} ms";
        });
    }

    private void OnSessionComplete(BenchmarkSession session)
    {
        Dispatcher.Invoke(() =>
        {
            _lastSession = session;
            ShowResults(session);
        });
    }

    private async void StartBenchmark(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(DurationInput.Text, out var duration) || duration < 5)
        {
            StatusText.Text = "La duración debe ser al menos 5 segundos.";
            return;
        }

        StartBtn.IsEnabled = false;
        StopBtn.IsEnabled = true;
        ResultsBox.Visibility = Visibility.Collapsed;
        StatusText.Text = "Iniciando benchmark...";

        var progress = new Progress<string>(msg =>
        {
            StatusText.Text = msg;
        });

        try
        {
            await _runner.RunSession(duration, ProcessInput.Text.Trim(), progress);
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error: {ex.Message}";
        }
        finally
        {
            StartBtn.IsEnabled = true;
            StopBtn.IsEnabled = false;
        }
    }

    private void StopBenchmark(object sender, RoutedEventArgs e)
    {
        _runner.Cancel();
        StatusText.Text = "Benchmark detenido por el usuario.";
        StartBtn.IsEnabled = true;
        StopBtn.IsEnabled = false;
    }

    private void ShowResults(BenchmarkSession session)
    {
        var liveBorder = (Border)((StackPanel)ResultsBox.Parent).Children[2];
        liveBorder.Visibility = Visibility.Visible;
        ResultsBox.Visibility = Visibility.Visible;

        ResultFps.Text = $"{session.AvgFps:F1}";
        ResultLow1.Text = $"{session.Low1PercentFps:F1}";
        ResultLow01.Text = $"{session.Low01PercentFps:F1}";
        ResultFrameTime.Text = $"{session.AvgFrameTimeMs:F2}";
        ResultCpu.Text = $"{session.AvgCpuPercent}% (max {session.MaxCpuPercent}%)";
        ResultRam.Text = $"{session.AvgRamGb:F2} GB";
        ResultPing.Text = $"{session.AvgPingMs:F1} ms";
        ResultJitter.Text = $"{session.AvgJitterMs:F1} ms";

        ResultSource.Text = $"Fuente frames: {session.FrameSource} | {session.TotalFrames} frames capturados en {session.DurationSeconds}s | Perfil activo: {session.SystemState?.ProfileApplied ?? "(ninguno)"}";
        StatusText.Text = "Benchmark completado.";
    }

    private void ExportSession(object sender, RoutedEventArgs e)
    {
        if (_lastSession == null) return;

        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WinBenchmark", "exports");
        Directory.CreateDirectory(appData);
        var path = Path.Combine(appData, $"session_{_lastSession.SessionId}.json");

        var json = JsonSerializer.Serialize(_lastSession, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);

        ExportStatus.Text = $"{FindResource("ReportSaved")} {path}";
        ExportStatus.Visibility = Visibility.Visible;
    }
}
