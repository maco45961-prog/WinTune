using System.Windows;

namespace WinOptimizer;

public partial class App : Application
{
    public static string CoreEnginePath { get; private set; } = "";

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Resolve core engine path: look relative to app, then fallback to known location
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        var relativePath = Path.Combine(appDir, "..", "..", "..", "..", "winoptimizer-core");
        var fallbackPath = @"D:\win\winoptimizer-core";

        if (Directory.Exists(Path.GetFullPath(relativePath)))
            CoreEnginePath = Path.GetFullPath(relativePath);
        else if (Directory.Exists(fallbackPath))
            CoreEnginePath = fallbackPath;
        else
        {
            MessageBox.Show(
                "Could not find WinOptimizer Core Engine.\n\nExpected at:\n" + relativePath,
                "WinOptimizer",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
        }
    }
}
