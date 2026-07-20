using System.Diagnostics;
using System.Text.Json;

namespace WinSetupWizard.Integration;

public class ProfileDefinition
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string TargetAudience { get; set; } = "";
    public string RiskLevel { get; set; } = "low";
    public List<string> TweakIds { get; set; } = new();
    public bool EstimatedRestartRequired { get; set; }
    public bool VanguardCompatible { get; set; }
}

public class ApplyProfileResult
{
    public string Status { get; set; } = "";
    public string Profile { get; set; } = "";
    public int Applied { get; set; }
    public int Errors { get; set; }
    public int Total { get; set; }
    public string Message { get; set; } = "";
}

public class CuratedApp
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Category { get; set; } = "";
    public bool Recommended { get; set; }
}

public class CuratedAppList
{
    public List<CuratedApp> Apps { get; set; } = new();
}

public static class CoreClient
{
    private static string? _corePath;

    public static void Initialize(string corePath)
    {
        _corePath = corePath;
    }

    public static string? CorePath => _corePath;

    public static List<ProfileDefinition> GetProfiles()
    {
        if (_corePath == null) return new List<ProfileDefinition>();

        var profilesDir = Path.Combine(_corePath, "profiles");
        if (!Directory.Exists(profilesDir)) return new List<ProfileDefinition>();

        return Directory.GetFiles(profilesDir, "*.json")
            .Select(f => JsonSerializer.Deserialize<ProfileDefinition>(
                File.ReadAllText(f), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }))
            .Where(p => p != null)
            .Cast<ProfileDefinition>()
            .ToList();
    }

    public static string GetCorePathForApply()
    {
        return _corePath ?? "";
    }

    public static List<CuratedApp> LoadCuratedApps()
    {
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        var path = Path.Combine(appDir, "..", "..", "..", "..", "apps-catalog", "curated-apps.json");
        if (!File.Exists(path))
        {
            // Fallback: next to the assembly
            path = Path.Combine(appDir, "apps-catalog", "curated-apps.json");
        }
        if (!File.Exists(path)) return new List<CuratedApp>();

        try
        {
            var json = File.ReadAllText(path);
            var list = JsonSerializer.Deserialize<CuratedAppList>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return list?.Apps ?? new List<CuratedApp>();
        }
        catch { return new List<CuratedApp>(); }
    }
}
