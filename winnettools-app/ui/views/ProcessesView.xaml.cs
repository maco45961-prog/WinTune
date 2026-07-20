using System.Windows;
using System.Windows.Controls;
using WinNetTools.Diagnostics;

namespace WinNetTools.UI.Views;

public partial class ProcessesView : Page
{
    public ProcessesView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await LoadProcesses();
    }

    private async Task LoadProcesses()
    {
        RefreshBtn.IsEnabled = false;
        ProcessLoading.Visibility = Visibility.Visible;

        var processes = await Task.Run(BandwidthHogs.GetActiveProcesses);

        ProcessList.ItemsSource = null;
        ProcessList.ItemsSource = processes;
        EmptyText.Visibility = processes.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        ProcessLoading.Visibility = Visibility.Collapsed;
        RefreshBtn.IsEnabled = true;

        try
        {
            if (BandwidthHogs.IsDeliveryOptimizationActive())
                DoWarning.Visibility = Visibility.Visible;
        }
        catch { }
    }

    private async void RefreshProcessList(object sender, RoutedEventArgs e)
    {
        await LoadProcesses();
    }
}
