using System.Windows;
using System.Windows.Controls;
using WinNetTools.Diagnostics;
using WinNetTools.Integration;
using WinNetTools.Report;

namespace WinNetTools.UI.Views;

public partial class ReportView : Page
{
    public ReportView()
    {
        InitializeComponent();
    }

    private async void GenerateReport(object sender, RoutedEventArgs e)
    {
        GenerateBtn.IsEnabled = false;
        StatusText.Visibility = Visibility.Collapsed;
        ReportPreview.Text = "Generando reporte...";

        try
        {
            var pingResults = await Task.Run(() => PingJitter.RunPingTest());
            var tracerouteHops = await Task.Run(() => Traceroute.RunTraceroute("playvalorant.com"));
            var dnsResults = await Task.Run(() => DnsCompare.CompareDns());
            var connectionInfo = await Task.Run(ConnectionType.DetectConnection);
            var processes = await Task.Run(BandwidthHogs.GetActiveProcesses);
            var doActive = await Task.Run(BandwidthHogs.IsDeliveryOptimizationActive);
            var systemState = await Task.Run(() => CoreStateReader.ReadState());

            var report = ReportExporter.GenerateReport(
                pingResults, tracerouteHops, dnsResults,
                connectionInfo, processes, doActive, systemState);

            ReportPreview.Text = report;

            var path = ReportExporter.SaveReport(report);
            StatusText.Text = $"{FindResource("ReportSaved")} {path}";
            StatusText.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            ReportPreview.Text = $"Error generando reporte: {ex.Message}";
        }
        finally
        {
            GenerateBtn.IsEnabled = true;
        }
    }
}
