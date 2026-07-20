using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using WinNetTools.Diagnostics;
using WinNetTools.Integration;

namespace WinNetTools.UI.Views;

public partial class DashboardView : Page
{
    private List<PingJitterResult> _lastResults = new();

    public DashboardView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await LoadConnectionInfo();
        await LoadPingData();
        CheckSystemState();
        CheckDeliveryOptimization();
    }

    private async Task LoadConnectionInfo()
    {
        var info = await Task.Run(ConnectionType.DetectConnection);
        ConnectionTypeText.Text = info.Type;
        ConnectionDetailText.Text = $"{info.AdapterName ?? ""} | {info.LinkSpeedMbps?.ToString() ?? "?"} Mbps | {string.Join(", ", info.IpAddresses)}";

        if (info.IsWiFi)
        {
            WifiWarning.Visibility = Visibility.Visible;
        }
    }

    private async Task LoadPingData()
    {
        RunPingBtn.IsEnabled = false;
        LoadingText.Visibility = Visibility.Visible;

        _lastResults = await Task.Run(() => PingJitter.RunPingTest());

        PingResultsList.ItemsSource = null;
        PingResultsList.ItemsSource = _lastResults;

        LoadingText.Visibility = Visibility.Collapsed;
        RunPingBtn.IsEnabled = true;

        UpdateHealthIndicator(_lastResults);
    }

    private void UpdateHealthIndicator(List<PingJitterResult> results)
    {
        if (results.Count == 0) return;

        var worst = results.OrderByDescending(r =>
            r.Status == "critical" ? 4 :
            r.Status == "poor" ? 3 :
            r.Status == "fair" ? 2 : 1)
            .First();

        string color;
        string textKey;

        switch (worst.Status)
        {
            case "good":
                color = "#4CAF50";
                textKey = "Good";
                break;
            case "fair":
                color = "#FF9800";
                textKey = "Fair";
                break;
            case "poor":
                color = "#F44336";
                textKey = "Poor";
                break;
            default:
                color = "#880000";
                textKey = "Critical";
                break;
        }

        HealthIndicator.Background = new System.Windows.Media.SolidColorBrush(
            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(color));
        HealthText.Text = FindResource(textKey) as string ?? textKey;
    }

    private void CheckSystemState()
    {
        try
        {
            var state = CoreStateReader.ReadState();
            var missing = CoreStateReader.GetMissingNetworkTweaks(state);

            if (missing.Contains("disable-nagle-algorithm"))
                NagleSuggestionBox.Visibility = Visibility.Visible;

            var dnsApplied = missing.Contains("optimize-dns");
            // DNS suggestion is handled in the DNS view
        }
        catch { }
    }

    private void CheckDeliveryOptimization()
    {
        try
        {
            if (BandwidthHogs.IsDeliveryOptimizationActive())
                DoWarning.Visibility = Visibility.Visible;
        }
        catch { }
    }

    private async void RunPingTest(object sender, RoutedEventArgs e)
    {
        await LoadPingData();
    }

    private void OpenOptimizerNagle(object sender, RoutedEventArgs e)
    {
        try
        {
            var optimizerPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..",
                "winoptimizer-app", "bin", "Debug", "net8.0-windows", "WinOptimizer.exe");
            if (File.Exists(optimizerPath))
                Process.Start(optimizerPath);
            else
                MessageBox.Show("WinOptimizer no encontrado. Búscalo en la carpeta winoptimizer-app.", "Abrir Optimizer");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"No se pudo abrir WinOptimizer: {ex.Message}", "Error");
        }
    }
}
