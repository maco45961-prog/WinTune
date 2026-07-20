using System.Windows;
using System.Windows.Controls;
using WinNetTools.UI.Views;

namespace WinNetTools;

public partial class MainWindow : Window
{
    private bool _isSpanish = true;

    public MainWindow()
    {
        InitializeComponent();
        MainFrame.Navigate(new DashboardView());
    }

    private void NavDashboard(object sender, RoutedEventArgs e) => MainFrame.Navigate(new DashboardView());
    private void NavTraceroute(object sender, RoutedEventArgs e) => MainFrame.Navigate(new TracerouteView());
    private void NavDns(object sender, RoutedEventArgs e) => MainFrame.Navigate(new DnsView());
    private void NavProcesses(object sender, RoutedEventArgs e) => MainFrame.Navigate(new ProcessesView());
    private void NavReport(object sender, RoutedEventArgs e) => MainFrame.Navigate(new ReportView());

    private void ToggleLanguage(object sender, RoutedEventArgs e)
    {
        _isSpanish = !_isSpanish;
        var dict = new ResourceDictionary();
        dict.Source = _isSpanish
            ? new Uri("ui/i18n/es.xaml", UriKind.Relative)
            : new Uri("ui/i18n/en.xaml", UriKind.Relative);

        Application.Current.Resources.MergedDictionaries.Clear();
        Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary
            { Source = new Uri("ui/Themes/DarkTheme.xaml", UriKind.Relative) });
        Application.Current.Resources.MergedDictionaries.Add(dict);

        Title = FindResource("AppTitle") as string ?? "WinNetTools";

        // Refresh current page
        if (MainFrame.Content is Page page)
        {
            var type = page.GetType();
            MainFrame.Navigate(Activator.CreateInstance(type)!);
        }
    }
}
