using System.Management;
using System.Text;

namespace WinGameModeDaemon.Detection;

/// <summary>
/// Monitors process creation/termination using native WMI events (no polling).
/// Subscribes to Win32_ProcessStartTrace / Win32_ProcessStopTrace and fires
/// callbacks only when a watched executable name appears or disappears.
/// </summary>
public sealed class ProcessWatcher : IDisposable
{
    private ManagementEventWatcher? _startWatcher;
    private ManagementEventWatcher? _stopWatcher;
    private readonly HashSet<string> _watchedNames;
    private readonly object _lock = new();
    private bool _disposed;

    /// <summary>Fires when a watched process starts. Args: (processName, processId).</summary>
    public event Action<string, int>? ProcessStarted;

    /// <summary>Fires when a watched process stops. Args: (processName, processId).</summary>
    public event Action<string, int>? ProcessStopped;

    public ProcessWatcher(IEnumerable<string> watchedExecutableNames)
    {
        _watchedNames = new HashSet<string>(
            watchedExecutableNames.Select(n => n.Trim().ToLowerInvariant()),
            StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Update the set of watched executable names at runtime (e.g., after config change).</summary>
    public void UpdateWatchedNames(IEnumerable<string> names)
    {
        lock (_lock)
        {
            _watchedNames.Clear();
            foreach (var n in names)
                _watchedNames.Add(n.Trim().ToLowerInvariant());
        }
    }

    /// <summary>Returns true if the given process name is in the watched set.</summary>
    public bool IsWatched(string processName)
    {
        lock (_lock)
        {
            return _watchedNames.Contains(processName.Trim().ToLowerInvariant());
        }
    }

    /// <summary>Returns a snapshot of currently watched names.</summary>
    public IReadOnlyCollection<string> GetWatchedNames()
    {
        lock (_lock)
        {
            return _watchedNames.ToList().AsReadOnly();
        }
    }

    /// <summary>Starts WMI event subscriptions. Safe to call multiple times.</summary>
    public void Start()
    {
        if (_startWatcher != null) return;

        try
        {
            // Process start events
            var startQuery = new WqlEventQuery(
                "SELECT * FROM Win32_ProcessStartTrace");
            _startWatcher = new ManagementEventWatcher(startQuery);
            _startWatcher.EventArrived += OnProcessStarted;
            _startWatcher.Start();

            // Process stop events
            var stopQuery = new WqlEventQuery(
                "SELECT * FROM Win32_ProcessStopTrace");
            _stopWatcher = new ManagementEventWatcher(stopQuery);
            _stopWatcher.EventArrived += OnProcessStopped;
            _stopWatcher.Start();
        }
        catch
        {
            Stop();
            throw;
        }
    }

    /// <summary>Stops all WMI subscriptions and releases watchers.</summary>
    public void Stop()
    {
        if (_startWatcher != null)
        {
            _startWatcher.Stop();
            _startWatcher.Dispose();
            _startWatcher = null;
        }
        if (_stopWatcher != null)
        {
            _stopWatcher.Stop();
            _stopWatcher.Dispose();
            _stopWatcher = null;
        }
    }

    private void OnProcessStarted(object sender, EventArrivedEventArgs e)
    {
        var name = GetProcessName(e);
        var pid = GetProcessId(e);

        if (name != null && IsWatched(name))
            ProcessStarted?.Invoke(name, pid);
    }

    private void OnProcessStopped(object sender, EventArrivedEventArgs e)
    {
        var name = GetProcessName(e);
        var pid = GetProcessId(e);

        if (name != null && IsWatched(name))
            ProcessStopped?.Invoke(name, pid);
    }

    private static string? GetProcessName(EventArrivedEventArgs e)
    {
        return e.NewEvent.Properties["ProcessName"]?.Value?.ToString();
    }

    private static int GetProcessId(EventArrivedEventArgs e)
    {
        var val = e.NewEvent.Properties["ProcessID"]?.Value;
        return val is uint u ? (int)u : 0;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
    }
}
