using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using WinBenchmark.Sessions;

namespace WinBenchmark.UI.Views;

public partial class HistoryView : Page
{
    public HistoryView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var sessions = SessionStore.ListSessions();
        HistoryList.ItemsSource = sessions;
        EmptyText.Visibility = sessions.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ExportAll(object sender, RoutedEventArgs e)
    {
        var sessions = SessionStore.ListSessions();

        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WinBenchmark", "exports");
        Directory.CreateDirectory(appData);
        var path = Path.Combine(appData, $"all_sessions_{DateTime.Now:yyyyMMdd-HHmmss}.json");

        var json = JsonSerializer.Serialize(sessions, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);

        ExportStatus.Text = $"{FindResource("ReportSaved")} {path}";
        ExportStatus.Visibility = Visibility.Visible;
    }
}
