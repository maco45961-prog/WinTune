using System.Windows;
using System.Windows.Controls;
using WinBenchmark.UI.Views;

namespace WinBenchmark;

public partial class MainWindow : Window
{
    private bool _isSpanish = true;

    public MainWindow()
    {
        InitializeComponent();
        MainFrame.Navigate(new DashboardView());
    }

    private void NavDashboard(object sender, RoutedEventArgs e) => MainFrame.Navigate(new DashboardView());
    private void NavActiveSession(object sender, RoutedEventArgs e) => MainFrame.Navigate(new ActiveSessionView());
    private void NavCompare(object sender, RoutedEventArgs e) => MainFrame.Navigate(new CompareView());
    private void NavHistory(object sender, RoutedEventArgs e) => MainFrame.Navigate(new HistoryView());

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

        Title = FindResource("AppTitle") as string ?? "WinBenchmark";

        if (MainFrame.Content is Page page)
        {
            var type = page.GetType();
            MainFrame.Navigate(Activator.CreateInstance(type)!);
        }
    }
}
