using System.Diagnostics;
using System.Windows.Forms;

namespace WinGameModeDaemon;

internal static class Program
{
    private const string MutexName = "Global\\WinGameModeDaemon_SingleInstance";

    [STAThread]
    static void Main(string[] args)
    {
        // Single instance check
        using var mutex = new Mutex(true, MutexName, out bool createdNew);
        if (!createdNew)
        {
            MessageBox.Show("WinGameModeDaemon is already running.", "WinGameModeDaemon",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // Determine core engine path
        var corePath = ResolveCorePath();
        if (corePath == null)
        {
            MessageBox.Show(
                "Could not locate winoptimizer-core.\n\n" +
                "Set the WINOPTIMIZER_CORE_PATH environment variable or place the daemon next to the core.",
                "WinGameModeDaemon",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        var configDir = Path.Combine(AppContext.BaseDirectory, "config");

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetHighDpiMode(HighDpiMode.SystemAware);

        var daemon = new GameModeDaemon(corePath, configDir);

        // Graceful shutdown handlers
        Application.ApplicationExit += (_, _) => daemon.Dispose();
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            // Revert any active profile before Windows shuts down
            daemon.Stop();
        };

        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            daemon.Stop();
            Environment.Exit(0);
        };

        // Session ending (user logs off / shuts down Windows)
        Microsoft.Win32.SystemEvents.SessionEnding += (_, _) => daemon.Stop();

        // Start the daemon (creates tray icon, starts watcher)
        daemon.Start();

        // WinForms message loop (blocks until Application.Exit)
        Application.Run();
    }

    /// <summary>
    /// Resolves the path to winoptimizer-core. Priority:
    /// 1. WINOPTIMIZER_CORE_PATH env var
    /// 2. ../winoptimizer-core relative to the daemon executable
    /// 3. ../winoptimizer-core relative to the working directory
    /// </summary>
    private static string? ResolveCorePath()
    {
        // 1. Environment variable
        var envPath = Environment.GetEnvironmentVariable("WINOPTIMIZER_CORE_PATH");
        if (!string.IsNullOrEmpty(envPath) && Directory.Exists(envPath))
            return envPath;

        // 2. Relative to exe directory
        var exeDir = AppContext.BaseDirectory;
        var relative = Path.Combine(exeDir, "..", "winoptimizer-core");
        var resolved = Path.GetFullPath(relative);
        if (Directory.Exists(resolved) && File.Exists(Path.Combine(resolved, "WinOptimizer.Core.psd1")))
            return resolved;

        // 3. Relative to working directory
        var cwdRelative = Path.Combine(Directory.GetCurrentDirectory(), "winoptimizer-core");
        if (Directory.Exists(cwdRelative) && File.Exists(Path.Combine(cwdRelative, "WinOptimizer.Core.psd1")))
            return cwdRelative;

        return null;
    }
}
