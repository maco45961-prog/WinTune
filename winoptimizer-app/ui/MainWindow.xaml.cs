using System.Windows;
using System.Windows.Controls;
using WinOptimizer.Integration;

namespace WinOptimizer.UI;

public partial class MainWindow : Window
{
    public static CoreClient? Core { get; private set; }
    public static bool IsVanguardInstalled { get; private set; }
    public static List<TweakDefinition> AllTweaks { get; private set; } = new();
    public static List<ProfileDefinition> AllProfiles { get; private set; } = new();
    public static List<SessionEntry> SessionLog { get; } = new();

    private readonly HomeView _homeView;
    private readonly CustomView _customView;
    private readonly LogView _logView;

    public MainWindow()
    {
        InitializeComponent();

        Core = new CoreClient(App.CoreEnginePath);

        // Load data from core
        try
        {
            AllTweaks = Core.GetAvailableTweaks();
            AllProfiles = Core.GetProfiles();
            IsVanguardInstalled = Core.IsVanguardInstalled();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load core data:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        // Show Vanguard banner if detected
        if (IsVanguardInstalled)
            VanguardBanner.Visibility = Visibility.Visible;

        // Create views
        _homeView = new HomeView();
        _customView = new CustomView();
        _logView = new LogView();

        ContentArea.Content = _homeView;
    }

    private void NavHome_Click(object sender, RoutedEventArgs e)
    {
        ContentArea.Content = _homeView;
        _homeView.RefreshProfiles();
    }

    private void NavCustom_Click(object sender, RoutedEventArgs e)
    {
        ContentArea.Content = _customView;
        _customView.RefreshTweaks();
    }

    private void NavLog_Click(object sender, RoutedEventArgs e)
    {
        ContentArea.Content = _logView;
        _logView.RefreshLog();
    }

    public static void AddSessionEntry(string tweakId, string tweakName, string action, string status)
    {
        SessionLog.Add(new SessionEntry
        {
            Time = DateTime.Now,
            TweakId = tweakId,
            TweakName = tweakName,
            Action = action,
            Status = status
        });
    }
}

public class SessionEntry
{
    public DateTime Time { get; set; }
    public string TweakId { get; set; } = "";
    public string TweakName { get; set; } = "";
    public string Action { get; set; } = "";
    public string Status { get; set; } = "";
}
