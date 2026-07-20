using System.Text.Json;

namespace WinGameModeDaemon.Config;

/// <summary>
/// Loads and saves watched-games.json config file.
/// </summary>
public sealed class ConfigManager
{
    private readonly string _configPath;
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ConfigManager(string configDirectory)
    {
        _configPath = Path.Combine(configDirectory, "watched-games.json");
    }

    public string ConfigPath => _configPath;

    /// <summary>Loads the config from disk, or returns defaults if file doesn't exist.</summary>
    public WatchedGamesConfig Load()
    {
        if (!File.Exists(_configPath))
        {
            var defaults = CreateDefaults();
            Save(defaults);
            return defaults;
        }

        try
        {
            var json = File.ReadAllText(_configPath);
            return JsonSerializer.Deserialize<WatchedGamesConfig>(json, JsonOpts)
                   ?? CreateDefaults();
        }
        catch
        {
            return CreateDefaults();
        }
    }

    /// <summary>Saves the config to disk.</summary>
    public void Save(WatchedGamesConfig config)
    {
        var dir = Path.GetDirectoryName(_configPath);
        if (dir != null && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(config, JsonOpts);
        File.WriteAllText(_configPath, json, System.Text.Encoding.UTF8);
    }

    /// <summary>Auto-detects common game executables from Steam/typical install paths.</summary>
    public static List<string> DetectInstalledGames()
    {
        var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Common game install directories
        var searchPaths = new List<string>();

        // Steam default paths
        var steamPaths = new[]
        {
            @"C:\Program Files (x86)\Steam\steamapps\common",
            @"D:\Steam\steamapps\common",
            @"D:\SteamLibrary\steamapps\common",
            @"E:\SteamLibrary\steamapps\common",
            @"F:\SteamLibrary\steamapps\common"
        };
        foreach (var sp in steamPaths)
            if (Directory.Exists(sp)) searchPaths.Add(sp);

        // Epic Games default
        var epicPaths = new[]
        {
            @"C:\Program Files\Epic Games",
            @"D:\Epic Games"
        };
        foreach (var ep in epicPaths)
            if (Directory.Exists(ep)) searchPaths.Add(ep);

        // Riot Games
        var riotPaths = new[]
        {
            @"C:\Riot Games",
            @"D:\Riot Games"
        };
        foreach (var rp in riotPaths)
            if (Directory.Exists(rp)) searchPaths.Add(rp);

        // Xbox / MS Store default
        var xboxPaths = new[]
        {
            @"C:\Program Files\ModifiableWindowsApps"
        };
        foreach (var xp in xboxPaths)
            if (Directory.Exists(xp)) searchPaths.Add(xp);

        // Search for .exe files in these paths
        foreach (var basePath in searchPaths)
        {
            try
            {
                foreach (var exe in Directory.EnumerateFiles(basePath, "*.exe", SearchOption.AllDirectories))
                {
                    var name = Path.GetFileName(exe);
                    // Filter: skip common non-game executables
                    if (!IsCommonNonGameExe(name))
                        found.Add(name);
                }
            }
            catch { /* permission denied, etc. */ }
        }

        return found.ToList();
    }

    private static WatchedGamesConfig CreateDefaults()
    {
        return new WatchedGamesConfig
        {
            Executables = new List<string>
            {
                "VALORANT.exe",
                "VALORANT-Win64-Shipping.exe"
            },
            ProfileToApply = "gaming-competitivo-lowend",
            Enabled = true,
            NotificationsEnabled = true,
            StartWithWindows = false
        };
    }

    private static bool IsCommonNonGameExe(string name)
    {
        var skip = new[]
        {
            "uninstall", "setup", "install", "update", "launcher",
            "crashhandler", "redistributable", "dxsetup", "dotnet",
            "vcredist", "oalinst", "steamservice", "cef", "gpu",
            "overlay", "helper", "service", "agent", "report", "cleanup",
            "eac", "be", "battleye", "easyanticheat", "nographics"
        };
        var lower = name.ToLowerInvariant();
        return skip.Any(s => lower.Contains(s));
    }
}
