using System.Windows;
using System.Windows.Controls;
using WinNetTools.Diagnostics;
using WinNetTools.Integration;

namespace WinNetTools.UI.Views;

public partial class DnsView : Page
{
    public DnsView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await RunDnsComparison();
    }

    private async Task RunDnsComparison()
    {
        var target = DnsTargetInput.Text.Trim();
        if (string.IsNullOrEmpty(target)) target = "playvalorant.com";

        RunDnsBtn.IsEnabled = false;
        DnsLoading.Visibility = Visibility.Visible;

        var results = await Task.Run(() => DnsCompare.CompareDns(target));

        DnsResultsList.ItemsSource = null;
        DnsResultsList.ItemsSource = results;

        DnsLoading.Visibility = Visibility.Collapsed;
        RunDnsBtn.IsEnabled = true;

        CheckDnsSuggestion(results);
    }

    private void CheckDnsSuggestion(List<DnsResult> results)
    {
        var systemDns = results.FirstOrDefault(r => r.Name == "Actual del sistema");
        if (systemDns == null || !systemDns.Success) return;

        var fastestExternal = results
            .Where(r => r.Name != "Actual del sistema" && r.Success)
            .OrderBy(r => r.ResolveTimeMs)
            .FirstOrDefault();

        if (fastestExternal != null && fastestExternal.ResolveTimeMs < systemDns.ResolveTimeMs * 0.8)
        {
            DnsSuggestionBox.Visibility = Visibility.Visible;
        }
    }

    private async void RunDnsTest(object sender, RoutedEventArgs e)
    {
        await RunDnsComparison();
    }

    private void OpenOptimizerDns(object sender, RoutedEventArgs e)
    {
        try
        {
            var optimizerPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..",
                "winoptimizer-app", "bin", "Debug", "net8.0-windows", "WinOptimizer.exe");
            if (File.Exists(optimizerPath))
                Process.Start(optimizerPath);
            else
                MessageBox.Show("WinOptimizer no encontrado.", "Abrir Optimizer");
        }
        catch { }
    }
}
