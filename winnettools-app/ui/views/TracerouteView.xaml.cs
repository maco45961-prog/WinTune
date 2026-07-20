using System.Windows;
using System.Windows.Controls;
using WinNetTools.Diagnostics;

namespace WinNetTools.UI.Views;

public partial class TracerouteView : Page
{
    public TracerouteView()
    {
        InitializeComponent();
    }

    private async void RunTrace(object sender, RoutedEventArgs e)
    {
        var target = TargetInput.Text.Trim();
        if (string.IsNullOrEmpty(target))
        {
            MessageBox.Show("Introduce un host o IP de destino.");
            return;
        }

        RunTraceBtn.IsEnabled = false;
        TraceLoading.Visibility = Visibility.Visible;

        var hops = await Task.Run(() => Traceroute.RunTraceroute(target));

        TraceList.ItemsSource = null;
        TraceList.ItemsSource = hops;

        TraceLoading.Visibility = Visibility.Collapsed;
        RunTraceBtn.IsEnabled = true;
    }
}
