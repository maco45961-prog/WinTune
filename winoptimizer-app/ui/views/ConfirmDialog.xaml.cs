using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WinOptimizer.Integration;

namespace WinOptimizer.UI;

public partial class ConfirmDialog : Window
{
    private readonly List<string> _tweakIds;

    public ConfirmDialog(string profileName, string description, List<string> tweakIds, string riskLevel)
    {
        InitializeComponent();
        _tweakIds = tweakIds;

        ConfirmMessageText.Text = $"Vas a aplicar el perfil \"{profileName}\" con {tweakIds.Count} tweaks:\n\n{description}";

        // Populate tweak list
        foreach (var tweakId in tweakIds)
        {
            var tweak = MainWindow.AllTweaks.FirstOrDefault(t => t.Id == tweakId);
            if (tweak == null) continue;

            var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };

            // Risk badge
            var badge = new Border
            {
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 2, 6, 2),
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            var badgeText = new TextBlock { FontSize = 10, VerticalAlignment = VerticalAlignment.Center };
            switch (tweak.Risk)
            {
                case "low":
                    badge.Background = new SolidColorBrush(Color.FromRgb(0x1a, 0x3a, 0x2a));
                    badgeText.Foreground = new SolidColorBrush(Color.FromRgb(0x00, 0xd2, 0xa0));
                    badgeText.Text = "BAJO";
                    break;
                case "medium":
                    badge.Background = new SolidColorBrush(Color.FromRgb(0x3a, 0x3a, 0x1a));
                    badgeText.Foreground = new SolidColorBrush(Color.FromRgb(0xf5, 0xa6, 0x23));
                    badgeText.Text = "MEDIO";
                    break;
                case "high":
                    badge.Background = new SolidColorBrush(Color.FromRgb(0x3a, 0x1a, 0x1a));
                    badgeText.Foreground = new SolidColorBrush(Color.FromRgb(0xff, 0x47, 0x57));
                    badgeText.Text = "ALTO";
                    break;
            }
            badge.Child = badgeText;
            panel.Children.Add(badge);

            // Tweak name
            panel.Children.Add(new TextBlock
            {
                Text = tweak.Name,
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(0xe8, 0xe8, 0xe8)),
                VerticalAlignment = VerticalAlignment.Center
            });

            // Restart icon
            if (tweak.RequiresRestart)
            {
                panel.Children.Add(new TextBlock
                {
                    Text = " 🔄",
                    FontSize = 12,
                    VerticalAlignment = VerticalAlignment.Center
                });
            }

            TweakListPanel.Children.Add(panel);
        }

        // Show restart notice if any tweak requires it
        if (tweakIds.Any(id =>
        {
            var t = MainWindow.AllTweaks.FirstOrDefault(x => x.Id == id);
            return t?.RequiresRestart == true;
        }))
        {
            RestartNotice.Visibility = Visibility.Visible;
        }
    }

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
