using System.Windows;
using System.Windows.Controls;

namespace WinOptimizer.UI;

public partial class HomeView : UserControl
{
    public HomeView()
    {
        InitializeComponent();
    }

    public void RefreshProfiles() { }

    private void SelectProfile_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string profileName)
        {
            var profile = MainWindow.AllProfiles.FirstOrDefault(p => p.Name == profileName);
            if (profile == null) return;

            // Filter tweaks for Vanguard compatibility
            var compatibleTweaks = profile.TweakIds;
            if (MainWindow.IsVanguardInstalled)
            {
                compatibleTweaks = profile.TweakIds
                    .Where(id =>
                    {
                        var t = MainWindow.AllTweaks.FirstOrDefault(x => x.Id == id);
                        return t != null && t.CompatibleWithVanguard;
                    })
                    .ToList();
            }

            // Show confirmation dialog
            var dialog = new ConfirmDialog(profile.Name, profile.Description, compatibleTweaks, profile RiskLevel);
            dialog.Owner = Window.GetWindow(this);
            var result = dialog.ShowDialog();

            if (result == true)
            {
                // Apply profile
                ApplyProfile(profileName);
            }
        }
    }

    private void ApplyProfile(string profileName)
    {
        try
        {
            var result = MainWindow.Core!.ApplyProfile(profileName);

            if (result.Status == "success" || result.Status == "partial")
            {
                // Log each applied tweak
                var profile = MainWindow.AllProfiles.FirstOrDefault(p => p.Name == profileName);
                if (profile != null)
                {
                    foreach (var tweakId in profile.TweakIds)
                    {
                        var tweak = MainWindow.AllTweaks.FirstOrDefault(t => t.Id == tweakId);
                        MainWindow.AddSessionEntry(tweakId, tweak?.Name ?? tweakId, "apply", "success");
                    }
                }

                MessageBox.Show(result.Message, "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

                // Check restart requirement
                var profileDef = MainWindow.AllProfiles.FirstOrDefault(p => p.Name == profileName);
                if (profileDef?.EstimatedRestartRequired == true)
                {
                    var restart = MessageBox.Show(
                        "Los cambios requieren un reinicio para tomar efecto completo. ¿Deseas reiniciar ahora?",
                        "Reinicio requerido",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (restart == MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start("shutdown", "/r /t 10 /c \"WinOptimizer: Reinicio necesario para aplicar cambios\"");
                    }
                }
            }
            else
            {
                MessageBox.Show($"Error: {result.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Exception: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void GoCustom_Click(object sender, RoutedEventArgs e)
    {
        // Switch to custom view via parent window
        var mainWindow = Window.GetWindow(this) as MainWindow;
        mainWindow?.NavCustom_Click(sender, e);
    }
}
