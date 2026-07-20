using System.Windows;
using System.Windows.Controls;
using WinBenchmark.Sessions;

namespace WinBenchmark.UI.Views;

public partial class DashboardView : Page
{
    public DashboardView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        LoadSessions();
    }

    private void LoadSessions()
    {
        var sessions = SessionStore.ListSessions();
        RecentSessionsList.ItemsSource = sessions.Take(20).ToList();
        TotalSessionsText.Text = $"{sessions.Count} sesiones guardadas";

        var latest = sessions.FirstOrDefault();
        if (latest != null)
        {
            var profile = latest.SystemState?.ProfileApplied ?? "(ningún perfil)";
            LatestSessionText.Text = $"Última: {latest.StartTime:yyyy-MM-dd HH:mm} | {latest.AvgFps:F1} FPS avg | Perfil: {profile}";
        }
        else
        {
            LatestSessionText.Text = "No hay sesiones aún.";
            EmptyText.Visibility = Visibility.Visible;
        }
    }

    private void GoToActiveSession(object sender, RoutedEventArgs e)
    {
        var win = Window.GetWindow(this);
        if (win is MainWindow mw)
            mw.MainFrame.Navigate(new ActiveSessionView());
    }
}
