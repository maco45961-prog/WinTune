using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Text.Json;

namespace WinOptimizer.Integration;

/// <summary>
/// Thin integration layer that calls WinOptimizer Core Engine PowerShell functions.
/// No business logic lives here — this is purely a passthrough to the core.
/// </summary>
public class CoreClient : IDisposable
{
    private readonly string _corePath;
    private readonly PowerShell _ps;

    public CoreClient(string coreEnginePath)
    {
        _corePath = coreEnginePath;
        _ps = PowerShell.Create();

        // Import the core module
        var modulePath = Path.Combine(corePath, "WinOptimizer.Core.psd1");
        _ps.AddCommand("Import-Module").AddArgument(modulePath).AddParameter("Force");
        _ps.Invoke();
        _ps.Commands.Clear();
    }

    public string CorePath => _corePath;

    // ─── Discover ────────────────────────────────────────────

    /// <summary>Returns all available tweaks from the core.</summary>
    public List<TweakDefinition> GetAvailableTweaks()
    {
        _ps.Commands.Clear();
        _ps.AddCommand("Get-AvailableTweaks");
        var results = _ps.Invoke<PSObject>();
        _ps.Commands.Clear();

        return results.Select(r =>
        {
            var obj = r.Properties.ToDictionary(p => p.Name, p => p.Value);
            return new TweakDefinition
            {
                Id = obj["id"]?.ToString() ?? "",
                Name = obj["name"]?.ToString() ?? "",
                Description = obj["description"]?.ToString() ?? "",
                Category = obj["category"]?.ToString() ?? "",
                Risk = obj["risk"]?.ToString() ?? "low",
                Reversible = obj["reversible"] is PSObject po && po.Value is bool b ? b : true,
                CompatibleWithVanguard = obj["compatible_with_vanguard"] is PSObject pv && pv.Value is bool bv ? bv : true,
                RequiresRestart = obj["requires_restart"] is PSObject pr && pr.Value is bool br ? br : false,
                Tags = obj["tags"] is PSObject pt && pt.BaseObject is object[] arr
                    ? arr.Select(x => x.ToString()!).ToList()
                    : new List<string>()
            };
        }).ToList();
    }

    // ─── Profiles ────────────────────────────────────────────

    /// <summary>Returns all profile definitions from the profiles/ directory.</summary>
    public List<ProfileDefinition> GetProfiles()
    {
        var profilesDir = Path.Combine(_corePath, "profiles");
        if (!Directory.Exists(profilesDir)) return new List<ProfileDefinition>();

        return Directory.GetFiles(profilesDir, "*.json")
            .Select(f => JsonSerializer.Deserialize<ProfileDefinition>(
                File.ReadAllText(f),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!)
            .Where(p => p != null)
            .ToList()!;
    }

    // ─── Apply / Revert ─────────────────────────────────────

    /// <summary>Applies a single tweak by ID.</summary>
    public TweakResult InvokeTweak(string tweakId, bool force = false)
    {
        _ps.Commands.Clear();
        var cmd = _ps.AddCommand("Invoke-Tweak").AddParameter("Id", tweakId);
        if (force) cmd.AddParameter("Force");
        var results = _ps.Invoke<PSObject>();
        _ps.Commands.Clear();

        return ParseTweakResult(results);
    }

    /// <summary>Reverts a single tweak by ID.</summary>
    public TweakResult UndoTweak(string tweakId)
    {
        _ps.Commands.Clear();
        _ps.AddCommand("Undo-Tweak").AddParameter("Id", tweakId);
        var results = _ps.Invoke<PSObject>();
        _ps.Commands.Clear();

        return ParseTweakResult(results);
    }

    /// <summary>Applies a profile by name.</summary>
    public ProfileResult ApplyProfile(string profileName, bool force = false)
    {
        _ps.Commands.Clear();
        var cmd = _ps.AddCommand("Apply-Profile").AddParameter("Name", profileName);
        if (force) cmd.AddParameter("Force");
        var results = _ps.Invoke<PSObject>();
        _ps.Commands.Clear();

        if (results.Count == 0)
            return new ProfileResult { Status = "error", Message = "No result from core." };

        var obj = results[0].Properties.ToDictionary(p => p.Name, p => p.Value);
        return new ProfileResult
        {
            Status = obj["Status"]?.ToString() ?? "error",
            Message = obj["Message"]?.ToString() ?? "",
            Applied = obj["Applied"] is PSObject pa && pa.Value is int pi ? pi : 0,
            Errors = obj["Errors"] is PSObject pe && pe.Value is int ei ? ei : 0,
            Total = obj["Total"] is PSObject pt2 && pt2.Value is int ti ? ti : 0
        };
    }

    // ─── Backup ──────────────────────────────────────────────

    /// <summary>Creates a system restore point + registry snapshot.</summary>
    public BackupResult CreateBackup(string tweakId)
    {
        _ps.Commands.Clear();
        _ps.AddCommand("New-SystemBackup").AddParameter("TweakId", tweakId);
        var results = _ps.Invoke<PSObject>();
        _ps.Commands.Clear();

        if (results.Count == 0)
            return new BackupResult { Status = "error", Message = "No result from core." };

        var obj = results[0].Properties.ToDictionary(p => p.Name, p => p.Value);
        return new BackupResult
        {
            Status = obj["Status"]?.ToString() ?? "error",
            BackupFile = obj["BackupFile"]?.ToString() ?? "",
            RestorePointCreated = obj["RestorePointCreated"] is PSObject pr && pr.Value is bool pb ? pb : false,
            Message = obj["Message"]?.ToString() ?? ""
        };
    }

    // ─── Vanguard Detection ──────────────────────────────────

    /// <summary>Returns true if Riot Vanguard anti-cheat is installed.</summary>
    public bool IsVanguardInstalled()
    {
        _ps.Commands.Clear();
        _ps.AddCommand("Test-VanguardInstalled");
        var results = _ps.Invoke<PSObject>();
        _ps.Commands.Clear();

        if (results.Count > 0 && results[0].BaseObject is bool b)
            return b;

        return false;
    }

    // ─── Helpers ─────────────────────────────────────────────

    private static TweakResult ParseTweakResult(List<PSObject> results)
    {
        if (results.Count == 0)
            return new TweakResult { Status = "error", Message = "No result from core." };

        var obj = results[0].Properties.ToDictionary(p => p.Name, p => p.Value);
        return new TweakResult
        {
            Status = obj["Status"]?.ToString() ?? "error",
            TweakId = obj["TweakId"]?.ToString() ?? obj["Id"]?.ToString() ?? "",
            Name = obj["Name"]?.ToString(),
            Message = obj["Message"]?.ToString() ?? "",
            RequiresRestart = obj["RequiresRestart"] is PSObject pr && pr.Value is bool pb ? pb : false
        };
    }

    public void Dispose()
    {
        _ps?.Dispose();
    }
}

// ─── Data Models ────────────────────────────────────────────

public class TweakDefinition
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Category { get; set; } = "";
    public string Risk { get; set; } = "low";
    public bool Reversible { get; set; } = true;
    public bool CompatibleWithVanguard { get; set; } = true;
    public bool RequiresRestart { get; set; } = false;
    public List<string> Tags { get; set; } = new();
}

public class ProfileDefinition
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string TargetAudience { get; set; } = "";
    public string RiskLevel { get; set; } = "low";
    public List<string> TweakIds { get; set; } = new();
    public bool EstimatedRestartRequired { get; set; } = false;
    public bool VanguardCompatible { get; set; } = true;
}

public class TweakResult
{
    public string Status { get; set; } = "";
    public string TweakId { get; set; } = "";
    public string? Name { get; set; }
    public string Message { get; set; } = "";
    public bool RequiresRestart { get; set; }
}

public class ProfileResult
{
    public string Status { get; set; } = "";
    public string Message { get; set; } = "";
    public int Applied { get; set; }
    public int Errors { get; set; }
    public int Total { get; set; }
}

public class BackupResult
{
    public string Status { get; set; } = "";
    public string BackupFile { get; set; } = "";
    public bool RestorePointCreated { get; set; }
    public string Message { get; set; } = "";
}
