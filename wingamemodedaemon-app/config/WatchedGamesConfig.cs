namespace WinGameModeDaemon.Config;

/// <summary>
/// Serializable config for the list of games to watch.
/// </summary>
public sealed class WatchedGamesConfig
{
    /// <summary>List of executable filenames to monitor (case-insensitive).</summary>
    public List<string> Executables { get; set; } = new();

    /// <summary>Profile name to apply when a game is detected.</summary>
    public string ProfileToApply { get; set; } = "gaming-competitivo-lowend";

    /// <summary>Whether the daemon is globally enabled. Setting to false disables all auto-apply.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Whether to send Windows toast notifications on profile changes.</summary>
    public bool NotificationsEnabled { get; set; } = true;

    /// <summary>Whether to start the daemon on Windows login.</summary>
    public bool StartWithWindows { get; set; } = false;
}
