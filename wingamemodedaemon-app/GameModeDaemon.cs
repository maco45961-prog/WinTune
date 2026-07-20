using System.Diagnostics;
using WinGameModeDaemon.Config;
using WinGameModeDaemon.Detection;
using WinGameModeDaemon.Integration;
using WinGameModeDaemon.Tray;

namespace WinGameModeDaemon;

/// <summary>
/// Central orchestrator. Ties process detection, core integration, tray, and config together.
/// Contains zero optimization logic — only calls to the core engine.
/// </summary>
public sealed class GameModeDaemon : IDisposable
{
    private readonly CoreClient _core;
    private readonly ProcessWatcher _watcher;
    private readonly ConfigManager _configManager;
    private readonly TrayIconManager _tray;
    private readonly Localizer _loc;
    private readonly string _logDir;

    private WatchedGamesConfig _config;
    private readonly object _stateLock = new();
    private bool _disposed;

    // State tracking
    private DaemonState _currentState = DaemonState.Inactive;
    private string? _activeGameName;
    private int _activeGamePid;
    private List<string> _appliedTweakIds = new();

    public GameModeDaemon(string coreEnginePath, string configDir)
    {
        var lang = System.Globalization.CultureInfo.CurrentUITwoLetterISOLanguageName;
        _loc = new Localizer(lang == "en" ? "en" : "es");

        _core = new CoreClient(coreEnginePath);
        _configManager = new ConfigManager(configDir);
        _config = _configManager.Load();

        _tray = new TrayIconManager(_loc);
        _tray.OnToggleEnabled += HandleToggleEnabled;
        _tray.OnOpenConfig += HandleOpenConfig;
        _tray.OnOpenLog += HandleOpenLog;
        _tray.OnOpenConfigFile += HandleOpenConfigFile;
        _tray.OnExit += HandleExit;

        _watcher = new ProcessWatcher(_config.Executables);
        _watcher.ProcessStarted += HandleGameStarted;
        _watcher.ProcessStopped += HandleGameStopped;

        _logDir = Path.Combine(coreEnginePath, "logs");
        if (!Directory.Exists(_logDir))
            Directory.CreateDirectory(_logDir);
    }

    public DaemonState CurrentState => _currentState;
    public string? ActiveGameName => _activeGameName;

    // ─── Lifecycle ──────────────────────────────────────────

    /// <summary>Starts the daemon: tray icon + process watcher. Call on UI thread.</summary>
    public void Start()
    {
        _tray.Create();
        _tray.SetEnabledState(_config.Enabled);

        if (_config.Enabled)
        {
            _tray.SetState(DaemonState.Inactive);
            _watcher.Start();
            Log("Daemon started. Watching: " + string.Join(", ", _config.Executables));
        }
        else
        {
            _tray.SetState(DaemonState.Disabled);
            Log("Daemon started in DISABLED state.");
        }
    }

    /// <summary>Stops the daemon gracefully. Reverts any active gaming profile.</summary>
    public void Stop()
    {
        RevertIfActive("daemon shutdown");
        _watcher.Stop();
        _tray.Dispose();
        Log("Daemon stopped.");
    }

    // ─── Process Events ─────────────────────────────────────

    private void HandleGameStarted(string processName, int pid)
    {
        lock (_stateLock)
        {
            if (!_config.Enabled) return;
            if (_currentState != DaemonState.Inactive) return;

            Log($"Game detected: {processName} (PID {pid})");

            _activeGameName = processName;
            _activeGamePid = pid;
            _currentState = DaemonState.Gaming;

            // Vanguard check
            if (_core.IsVanguardInstalled())
            {
                Log("Vanguard detected — skipping incompatible tweaks.");
                if (_config.NotificationsEnabled)
                    _tray.ShowNotification(
                        _loc["notif.vanguard blocked"],
                        _loc["notif.vanguard message"],
                        System.Windows.Forms.ToolTipIcon.Warning);
            }

            // Apply profile
            var (status, appliedIds, message) = _core.ApplyProfile(_config.ProfileToApply);

            if (status == "success" || status == "partial")
            {
                _appliedTweakIds = appliedIds;
                _tray.SetState(DaemonState.Gaming, processName);

                if (_config.NotificationsEnabled)
                {
                    var title = _loc["notif.gaming Activated"];
                    var msg = string.Format(_loc["notif.gaming message"], _config.ProfileToApply, processName);
                    _tray.ShowNotification(title, msg);
                }

                Log($"Profile '{_config.ProfileToApply}' applied. Tweaks: {string.Join(", ", appliedIds)}");
            }
            else
            {
                Log($"ERROR applying profile: {message}");
                _currentState = DaemonState.Inactive;
                _activeGameName = null;
                _appliedTweakIds.Clear();

                if (_config.NotificationsEnabled)
                    _tray.ShowNotification(_loc["notif.error"], message, System.Windows.Forms.ToolTipIcon.Error);
            }
        }
    }

    private void HandleGameStopped(string processName, int pid)
    {
        lock (_stateLock)
        {
            if (_currentState != DaemonState.Gaming) return;
            if (_activeGamePid != pid && _activeGameName != processName) return;

            Log($"Game closed: {processName} (PID {pid})");
            RevertProfile();
        }
    }

    // ─── Revert Logic ───────────────────────────────────────

    private void RevertProfile()
    {
        _currentState = DaemonState.Reverting;
        _tray.SetState(DaemonState.Reverting);

        var errors = new List<string>();

        // Revert ONLY the tweaks we applied (not user's manual tweaks)
        foreach (var tweakId in _appliedTweakIds)
        {
            try
            {
                var (status, message) = _core.UndoTweak(tweakId);
                if (status != "success")
                {
                    Log($"Warning: Could not revert tweak '{tweakId}': {message}");
                    errors.Add($"{tweakId}: {message}");
                }
                else
                {
                    Log($"Reverted tweak: {tweakId}");
                }
            }
            catch (Exception ex)
            {
                Log($"Error reverting tweak '{tweakId}': {ex.Message}");
                errors.Add($"{tweakId}: {ex.Message}");
            }
        }

        _appliedTweakIds.Clear();
        _activeGameName = null;
        _activeGamePid = 0;
        _currentState = DaemonState.Inactive;
        _tray.SetState(DaemonState.Inactive);

        if (_config.NotificationsEnabled)
        {
            if (errors.Count == 0)
            {
                _tray.ShowNotification(_loc["notif.reverted"], _loc["notif.reverted message"]);
            }
            else
            {
                _tray.ShowNotification(
                    _loc["notif.error"],
                    $"Reverted with {errors.Count} error(s). Check log.",
                    System.Windows.Forms.ToolTipIcon.Warning);
            }
        }

        Log($"Profile reverted. Errors: {errors.Count}");
    }

    private void RevertIfActive(string reason)
    {
        lock (_stateLock)
        {
            if (_currentState == DaemonState.Gaming || _currentState == DaemonState.Reverting)
            {
                Log($"Reverting due to: {reason}");
                RevertProfile();
            }
        }
    }

    // ─── Tray Menu Handlers ─────────────────────────────────

    private void HandleToggleEnabled()
    {
        lock (_stateLock)
        {
            _config.Enabled = !_config.Enabled;
            _configManager.Save(_config);

            if (_config.Enabled)
            {
                _tray.SetEnabledState(true);
                _tray.SetState(DaemonState.Inactive);
                _watcher.Start();
                Log("Daemon ENABLED by user.");
            }
            else
            {
                RevertIfActive("user disabled daemon");
                _watcher.Stop();
                _tray.SetEnabledState(false);
                _tray.SetState(DaemonState.Disabled);
                Log("Daemon DISABLED by user.");
            }
        }
    }

    private void HandleOpenConfig()
    {
        var form = new ConfigForm(_config, _loc, (newConfig) =>
        {
            lock (_stateLock)
            {
                _config = newConfig;
                _configManager.Save(_config);
                _watcher.UpdateWatchedNames(_config.Executables);
                _tray.SetEnabledState(_config.Enabled);
                Log($"Config updated. Watching: {string.Join(", ", _config.Executables)}");
            }
        });
        form.ShowDialog();
    }

    private void HandleOpenConfigFile()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = _configManager.ConfigPath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Log($"Could not open config file: {ex.Message}");
        }
    }

    private void HandleOpenLog()
    {
        try
        {
            var logFiles = Directory.GetFiles(_logDir, "winoptimizer-*.log")
                .OrderByDescending(f => f)
                .ToList();

            if (logFiles.Count > 0)
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = logFiles[0],
                    UseShellExecute = true
                });
            }
        }
        catch (Exception ex)
        {
            Log($"Could not open log: {ex.Message}");
        }
    }

    private void HandleExit()
    {
        Stop();
        Application.Exit();
    }

    // ─── Logging ────────────────────────────────────────────

    private void Log(string message)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var line = $"[{timestamp}] [GameModeDaemon] {message}";

        try
        {
            var logFile = Path.Combine(_logDir, $"wingamemode-{DateTime.Now:yyyyMMdd}.log");
            File.AppendAllText(logFile, line + Environment.NewLine);
        }
        catch { /* best effort */ }

        Debug.WriteLine(line);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
        _core.Dispose();
        _watcher.Dispose();
    }
}
