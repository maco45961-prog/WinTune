using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WinOptimizer.Integration;

namespace WinOptimizer.UI;

public partial class CustomView : UserControl
{
    private readonly List<TweakCheckBox> _allCheckBoxes = new();

    public CustomView()
    {
        InitializeComponent();
    }

    public void RefreshTweaks()
    {
        TweaksPanel.Children.Clear();
        _allCheckBoxes.Clear();

        var categories = MainWindow.AllTweaks
            .GroupBy(t => t.Category)
            .OrderBy(g => g.Key);

        foreach (var cat in categories)
        {
            // Category header
            var header = new TextBlock
            {
                Text = FormatCategory(cat.Key),
                FontSize = 16,
                FontWeight = FontWeight.FromOpenTypeWeight(600),
                Foreground = new SolidColorBrush(Color.FromRgb(0xe8, 0xe8, 0xe8)),
                Margin = new Thickness(0, 16, 0, 8)
            };
            TweaksPanel.Children.Add(header);

            foreach (var tweak in cat.OrderBy(t => t.Name))
            {
                var row = CreateTweakRow(tweak);
                TweaksPanel.Children.Add(row);
            }
        }

        UpdateSelectedCount();
    }

    private Border CreateTweakRow(TweakDefinition tweak)
    {
        var cb = new CheckBox
        {
            Tag = tweak,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 12, 0)
        };
        cb.Checked += TweakCheckBox_Changed;
        cb.Unchecked += TweakCheckBox_Changed;

        // Name
        var nameBlock = new TextBlock
        {
            Text = tweak.Name,
            FontSize = 13,
            Foreground = new SolidColorBrush(Color.FromRgb(0xe8, 0xe8, 0xe8)),
            VerticalAlignment = VerticalAlignment.Center
        };

        // Description
        var descBlock = new TextBlock
        {
            Text = tweak.Description,
            FontSize = 11,
            Foreground = new SolidColorBrush(Color.FromRgb(0xa0, 0xa0, 0xb0)),
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 500,
            VerticalAlignment = VerticalAlignment.Center
        };

        // Risk badge
        var riskBorder = new Border
        {
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(6, 2, 6, 2),
            Margin = new Thickness(8, 0, 4, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        var riskText = new TextBlock { FontSize = 10, VerticalAlignment = VerticalAlignment.Center };
        switch (tweak.Risk)
        {
            case "low":
                riskBorder.Background = new SolidColorBrush(Color.FromRgb(0x1a, 0x3a, 0x2a));
                riskText.Foreground = new SolidColorBrush(Color.FromRgb(0x00, 0xd2, 0xa0));
                riskText.Text = "BAJO";
                break;
            case "medium":
                riskBorder.Background = new SolidColorBrush(Color.FromRgb(0x3a, 0x3a, 0x1a));
                riskText.Foreground = new SolidColorBrush(Color.FromRgb(0xf5, 0xa6, 0x23));
                riskText.Text = "MEDIO";
                break;
            case "high":
                riskBorder.Background = new SolidColorBrush(Color.FromRgb(0x3a, 0x1a, 0x1a));
                riskText.Foreground = new SolidColorBrush(Color.FromRgb(0xff, 0x47, 0x57));
                riskText.Text = "ALTO";
                break;
        }
        riskBorder.Child = riskText;

        // Reversible badge
        var revBorder = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0x1a, 0x2a, 0x1a)),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(6, 2, 6, 2),
            Margin = new Thickness(4, 0, 4, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        revBorder.Child = new TextBlock
        {
            Text = "Reversible",
            FontSize = 10,
            Foreground = new SolidColorBrush(Color.FromRgb(0x00, 0xd2, 0xa0)),
            VerticalAlignment = VerticalAlignment.Center
        };

        // Restart badge
        var restartBadge = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0x3a, 0x3a, 0x1a)),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(6, 2, 6, 2),
            Margin = new Thickness(4, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Visibility = tweak.RequiresRestart ? Visibility.Visible : Visibility.Collapsed
        };
        restartBadge.Child = new TextBlock
        {
            Text = "Reinicio",
            FontSize = 10,
            Foreground = new SolidColorBrush(Color.FromRgb(0xf5, 0xa6, 0x23)),
            VerticalAlignment = VerticalAlignment.Center
        };

        // Badges panel
        var badgesPanel = new StackPanel { Orientation = Orientation.Horizontal };
        badgesPanel.Children.Add(riskBorder);
        badgesPanel.Children.Add(revBorder);
        badgesPanel.Children.Add(restartBadge);

        // Content panel
        var contentPanel = new StackPanel { Margin = new Thickness(0, 2, 0, 2) };
        var topRow = new StackPanel { Orientation = Orientation.Horizontal };
        topRow.Children.Add(nameBlock);
        topRow.Children.Add(badgesPanel);
        contentPanel.Children.Add(topRow);
        contentPanel.Children.Add(descBlock);

        // Main row
        var mainPanel = new StackPanel { Orientation = Orientation.Horizontal };
        mainPanel.Children.Add(cb);
        mainPanel.Children.Add(contentPanel);

        _allCheckBoxes.Add(new TweakCheckBox { CheckBox = cb, Tweak = tweak });

        return new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0x0f, 0x34, 0x60)),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(14, 10, 14, 10),
            Margin = new Thickness(0, 0, 0, 6),
            Child = mainPanel
        };
    }

    private void TweakCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdateSelectedCount();
    }

    private void UpdateSelectedCount()
    {
        var count = _allCheckBoxes.Count(cb => cb.CheckBox.IsChecked == true);
        SelectedCountText.Text = $"{count} seleccionados";
        ApplyBtn.IsEnabled = count > 0;
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var query = SearchBox.Text.Trim().ToLowerInvariant();
        SearchPlaceholder.Visibility = string.IsNullOrEmpty(query) ? Visibility.Visible : Visibility.Collapsed;

        foreach (var item in _allCheckBoxes)
        {
            var match = string.IsNullOrEmpty(query) ||
                item.Tweak.Name.ToLowerInvariant().Contains(query) ||
                item.Tweak.Description.ToLowerInvariant().Contains(query) ||
                item.Tweak.Id.ToLowerInvariant().Contains(query) ||
                item.Tweak.Category.ToLowerInvariant().Contains(query);

            // Find parent row (the Border containing this checkbox)
            var parent = item.CheckBox.Parent as StackPanel;
            var rowBorder = parent?.Parent as FrameworkElement;
            if (rowBorder != null)
                rowBorder.Visibility = match ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void SelectAll_Click(object sender, RoutedEventArgs e)
    {
        foreach (var item in _allCheckBoxes)
        {
            if (item.CheckBox.Parent is StackPanel sp && sp.Parent is FrameworkElement fe && fe.Visibility == Visibility.Visible)
                item.CheckBox.IsChecked = true;
        }
    }

    private void DeselectAll_Click(object sender, RoutedEventArgs e)
    {
        foreach (var item in _allCheckBoxes)
            item.CheckBox.IsChecked = false;
    }

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        var selectedIds = _allCheckBoxes
            .Where(cb => cb.CheckBox.IsChecked == true)
            .Select(cb => cb.Tweak.Id)
            .ToList();

        if (selectedIds.Count == 0) return;

        var dialog = new ConfirmDialog("Personalizado", "Tweaks seleccionados manualmente", selectedIds, "mixed");
        dialog.Owner = Window.GetWindow(this);
        var result = dialog.ShowDialog();

        if (result == true)
        {
            ApplySelectedTweaks(selectedIds);
        }
    }

    private void ApplySelectedTweaks(List<string> tweakIds)
    {
        var applied = 0;
        var errors = 0;

        foreach (var tweakId in tweakIds)
        {
            try
            {
                var result = MainWindow.Core!.InvokeTweak(tweakId);
                var tweak = MainWindow.AllTweaks.FirstOrDefault(t => t.Id == tweakId);

                if (result.Status == "success")
                {
                    applied++;
                    MainWindow.AddSessionEntry(tweakId, tweak?.Name ?? tweakId, "apply", "success");
                }
                else
                {
                    errors++;
                    MainWindow.AddSessionEntry(tweakId, tweak?.Name ?? tweakId, "apply", "error");
                }
            }
            catch (Exception ex)
            {
                errors++;
                MainWindow.AddSessionEntry(tweakId, tweakId, "apply", "error");
            }
        }

        MessageBox.Show(
            $"Aplicados: {applied}/{tweakIds.Count}. Errores: {errors}",
            "Resultado",
            MessageBoxButton.OK,
            errors == 0 ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }

    private static string FormatCategory(string cat) => cat switch
    {
        "privacy" => "Privacidad",
        "services" => "Servicios",
        "network" => "Red",
        "gaming" => "Gaming",
        "appearance" => "Apariencia",
        "startmenu-taskbar" => "Inicio / Taskbar",
        "explorer" => "Explorer",
        _ => cat
    };
}

internal class TweakCheckBox
{
    public CheckBox CheckBox { get; set; } = null!;
    public TweakDefinition Tweak { get; set; } = null!;
}
