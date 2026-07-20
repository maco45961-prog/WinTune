using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WinOptimizer.UI;

public partial class LogView : UserControl
{
    public LogView()
    {
        InitializeComponent();
    }

    public void RefreshLog()
    {
        LogEntriesPanel.Children.Clear();
        var entries = MainWindow.SessionLog;

        EmptyText.Visibility = entries.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        LogScroll.Visibility = entries.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        UndoAllBtn.IsEnabled = entries.Any(e => e.Action == "apply" && e.Status == "success");

        foreach (var entry in entries.AsEnumerable().Reverse())
        {
            var row = CreateLogRow(entry);
            LogEntriesPanel.Children.Add(row);
        }
    }

    private Border CreateLogRow(SessionEntry entry)
    {
        // Time
        var timeText = new TextBlock
        {
            Text = entry.Time.ToString("HH:mm:ss"),
            FontSize = 12,
            Foreground = new SolidColorBrush(Color.FromRgb(0xa0, 0xa0, 0xb0)),
            VerticalAlignment = VerticalAlignment.Center
        };

        // Tweak name
        var nameText = new TextBlock
        {
            Text = entry.TweakName,
            FontSize = 12,
            Foreground = new SolidColorBrush(Color.FromRgb(0xe8, 0xe8, 0xe8)),
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        };

        // Action badge
        var actionBorder = new Border
        {
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(6, 2, 6, 2),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        var actionText = new TextBlock { FontSize = 10, VerticalAlignment = VerticalAlignment.Center };
        switch (entry.Action)
        {
            case "apply":
                actionBorder.Background = new SolidColorBrush(Color.FromRgb(0x1a, 0x2a, 0x3a));
                actionText.Foreground = new SolidColorBrush(Color.FromRgb(0x00, 0xd2, 0xa0));
                actionText.Text = "APLICAR";
                break;
            case "revert":
                actionBorder.Background = new SolidColorBrush(Color.FromRgb(0x3a, 0x2a, 0x1a));
                actionText.Foreground = new SolidColorBrush(Color.FromRgb(0xf5, 0xa6, 0x23));
                actionText.Text = "REVERTIR";
                break;
            default:
                actionBorder.Background = new SolidColorBrush(Color.FromRgb(0x2a, 0x2a, 0x2a));
                actionText.Foreground = new SolidColorBrush(Color.FromRgb(0xa0, 0xa0, 0xb0));
                actionText.Text = entry.Action.ToUpper();
                break;
        }
        actionBorder.Child = actionText;

        // Status badge
        var statusBorder = new Border
        {
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(6, 2, 6, 2),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        var statusText = new TextBlock { FontSize = 10, VerticalAlignment = VerticalAlignment.Center };
        switch (entry.Status)
        {
            case "success":
                statusBorder.Background = new SolidColorBrush(Color.FromRgb(0x1a, 0x3a, 0x2a));
                statusText.Foreground = new SolidColorBrush(Color.FromRgb(0x00, 0xd2, 0xa0));
                statusText.Text = "OK";
                break;
            case "error":
                statusBorder.Background = new SolidColorBrush(Color.FromRgb(0x3a, 0x1a, 0x1a));
                statusText.Foreground = new SolidColorBrush(Color.FromRgb(0xff, 0x47, 0x57));
                statusText.Text = "ERROR";
                break;
            default:
                statusBorder.Background = new SolidColorBrush(Color.FromRgb(0x2a, 0x2a, 0x2a));
                statusText.Foreground = new SolidColorBrush(Color.FromRgb(0xa0, 0xa0, 0xb0));
                statusText.Text = entry.Status.ToUpper();
                break;
        }
        statusBorder.Child = statusText;

        // Undo button
        var undoBtn = new Button
        {
            Content = "Deshacer",
            FontSize = 11,
            Foreground = new SolidColorBrush(Color.FromRgb(0xff, 0x47, 0x57)),
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0xff, 0x47, 0x57)),
            Padding = new Thickness(8, 2, 8, 2),
            Cursor = Cursors.Hand,
            Tag = entry,
            Visibility = entry.Action == "apply" && entry.Status == "success"
                ? Visibility.Visible
                : Visibility.Collapsed
        };
        undoBtn.Click += UndoTweak_Click;

        // Layout
        var grid = new Grid { Margin = new Thickness(16, 10, 16, 10) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });

        Grid.SetColumn(timeText, 0);
        Grid.SetColumn(nameText, 1);
        Grid.SetColumn(actionBorder, 2);
        Grid.SetColumn(statusBorder, 3);
        Grid.SetColumn(undoBtn, 4);

        grid.Children.Add(timeText);
        grid.Children.Add(nameText);
        grid.Children.Add(actionBorder);
        grid.Children.Add(statusBorder);
        grid.Children.Add(undoBtn);

        return new Border
        {
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x2a, 0x2a, 0x4a)),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Child = grid
        };
    }

    private void UndoTweak_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is SessionEntry entry)
        {
            try
            {
                var result = MainWindow.Core!.UndoTweak(entry.TweakId);
                if (result.Status == "success")
                {
                    MainWindow.AddSessionEntry(entry.TweakId, entry.TweakName, "revert", "success");
                    RefreshLog();
                }
                else
                {
                    MessageBox.Show($"Error al revertir: {result.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Exception: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void UndoAll_Click(object sender, RoutedEventArgs e)
    {
        var appliedEntries = MainWindow.SessionLog
            .Where(e => e.Action == "apply" && e.Status == "success")
            .ToList();

        if (appliedEntries.Count == 0) return;

        var confirm = MessageBox.Show(
            $"¿Deshacer {appliedEntries.Count} tweaks aplicados en esta sesión?",
            "Deshacer todo",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes) return;

        var success = 0;
        var errors = 0;

        foreach (var entry in appliedEntries)
        {
            try
            {
                var result = MainWindow.Core!.UndoTweak(entry.TweakId);
                if (result.Status == "success")
                {
                    success++;
                    MainWindow.AddSessionEntry(entry.TweakId, entry.TweakName, "revert", "success");
                }
                else
                {
                    errors++;
                    MainWindow.AddSessionEntry(entry.TweakId, entry.TweakName, "revert", "error");
                }
            }
            catch
            {
                errors++;
            }
        }

        RefreshLog();
        MessageBox.Show(
            $"Revertidos: {success}/{appliedEntries.Count}. Errores: {errors}",
            "Resultado",
            MessageBoxButton.OK,
            errors == 0 ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }
}
