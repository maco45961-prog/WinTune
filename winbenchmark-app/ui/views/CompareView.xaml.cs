using System.Windows;
using System.Windows.Controls;
using WinBenchmark.Sessions;

namespace WinBenchmark.UI.Views;

public partial class CompareView : Page
{
    private List<BenchmarkSession> _sessions = new();

    public CompareView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _sessions = SessionStore.ListSessions();

        BeforeCombo.ItemsSource = _sessions.Select(s => new
        {
            s.SessionId,
            Display = $"{s.StartTime:yyyy-MM-dd HH:mm} | {s.AvgFps:F1} FPS | {s.SystemState?.ProfileApplied ?? "sin perfil"}"
        }).ToList();
        BeforeCombo.DisplayMemberPath = "Display";
        BeforeCombo.SelectedValuePath = "SessionId";

        AfterCombo.ItemsSource = _sessions.Select(s => new
        {
            s.SessionId,
            Display = $"{s.StartTime:yyyy-MM-dd HH:mm} | {s.AvgFps:F1} FPS | {s.SystemState?.ProfileApplied ?? "sin perfil"}"
        }).ToList();
        AfterCombo.DisplayMemberPath = "Display";
        AfterCombo.SelectedValuePath = "SessionId";

        if (_sessions.Count >= 2)
        {
            AfterCombo.SelectedIndex = 0;
            BeforeCombo.SelectedIndex = _sessions.Count > 1 ? 1 : 0;
        }

        UpdateButtonState();
    }

    private void UpdateButtonState()
    {
        // Find button
        var btn = (Button)FindName("RunCompareBtn") ?? ParentOfType<Button>(this);
    }

    private void RunCompare(object sender, RoutedEventArgs e)
    {
        var beforeId = BeforeCombo.SelectedValue?.ToString();
        var afterId = AfterCombo.SelectedValue?.ToString();

        if (beforeId == null || afterId == null || beforeId == afterId)
        {
            MessageBox.Show("Selecciona dos sesiones distintas.");
            return;
        }

        var before = SessionStore.LoadSession(beforeId);
        var after = SessionStore.LoadSession(afterId);

        if (before == null || after == null) return;

        var result = SessionComparer.Compare(before, after);

        CompareResults.Visibility = Visibility.Visible;
        NoSelectionText.Visibility = Visibility.Collapsed;

        CompareSummary.Text = result.Summary;

        DfpsBefore.Text = $"{before.AvgFps:F1}";
        DfpsAfter.Text = $"{after.AvgFps:F1}";
        DfpsDelta.Text = $"{result.DeltaFps:+0.0;-0.0}";
        DfpsDelta.Foreground = result.DeltaFps >= 0
            ? (System.Windows.Media.Brush)FindResource("SuccessBrush")
            : (System.Windows.Media.Brush)FindResource("DangerBrush");

        Dlow1Before.Text = $"{before.Low1PercentFps:F1}";
        Dlow1After.Text = $"{after.Low1PercentFps:F1}";
        Dlow1Delta.Text = $"{result.DeltaLow1Percent:+0.0;-0.0}";
        Dlow1Delta.Foreground = result.DeltaLow1Percent >= 0
            ? (System.Windows.Media.Brush)FindResource("SuccessBrush")
            : (System.Windows.Media.Brush)FindResource("DangerBrush");

        Dlow01Before.Text = $"{before.Low01PercentFps:F1}";
        Dlow01After.Text = $"{after.Low01PercentFps:F1}";
        Dlow01Delta.Text = $"{result.DeltaLow01Percent:+0.0;-0.0}";
        Dlow01Delta.Foreground = result.DeltaLow01Percent >= 0
            ? (System.Windows.Media.Brush)FindResource("SuccessBrush")
            : (System.Windows.Media.Brush)FindResource("DangerBrush");

        DftBefore.Text = $"{before.AvgFrameTimeMs:F2}";
        DftAfter.Text = $"{after.AvgFrameTimeMs:F2}";
        DftDelta.Text = $"{result.DeltaFrameTimeMs:+0.00;-0.00}";
        DftDelta.Foreground = result.DeltaFrameTimeMs <= 0
            ? (System.Windows.Media.Brush)FindResource("SuccessBrush")
            : (System.Windows.Media.Brush)FindResource("DangerBrush");

        DpingBefore.Text = $"{before.AvgPingMs:F1}";
        DpingAfter.Text = $"{after.AvgPingMs:F1}";
        DpingDelta.Text = $"{result.DeltaPingMs:+0.0;-0.0}";
        DpingDelta.Foreground = result.DeltaPingMs <= 0
            ? (System.Windows.Media.Brush)FindResource("SuccessBrush")
            : (System.Windows.Media.Brush)FindResource("DangerBrush");

        DcpuBefore.Text = $"{before.AvgCpuPercent}%";
        DcpuAfter.Text = $"{after.AvgCpuPercent}%";
        DcpuDelta.Text = $"{result.DeltaCpuPercent:+0.0;-0.0}%";

        var beforeProfile = before.SystemState?.ProfileApplied ?? "(ninguno)";
        var afterProfile = after.SystemState?.ProfileApplied ?? "(ninguno)";
        StateChangeText.Text = $"Perfil activo: \"{beforeProfile}\" → \"{afterProfile}\"";
    }

    private static T? ParentOfType<T>(DependencyObject child) where T : DependencyObject
    {
        var parent = System.Windows.Media.VisualTreeHelper.GetParent(child);
        while (parent != null && parent is not T)
            parent = System.Windows.Media.VisualTreeHelper.GetParent(parent);
        return parent as T;
    }
}
