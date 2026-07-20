using System.Management.Automation;
using System.Text.Json;

namespace WinGameModeDaemon.Integration;

/// <summary>
/// Thin integration layer that calls WinOptimizer Core Engine PowerShell functions.
/// No business logic lives here — this is purely a passthrough to the core.
/// Thread-safe: all PowerShell calls are serialized via _lock.
/// </summary>
public sealed class CoreClient : IDisposable
{
    private readonly string _corePath;
    private readonly PowerShell _ps;
    private readonly object _lock = new();

    public CoreClient(string coreEnginePath)
    {
        _corePath = coreEnginePath;
        _ps = PowerShell.Create();

        var modulePath = Path.Combine(coreEnginePath, "WinOptimizer.Core.psd1");
        _ps.AddCommand("Import-Module").AddArgument(modulePath).AddParameter("Force");
        _ps.Invoke();
        _ps.Commands.Clear();
    }

    public string CorePath => _corePath;

    // ─── Profile Apply ──────────────────────────────────────

    /// <summary>
    /// Applies a profile by name. Returns the list of successfully applied tweak IDs
    /// so the daemon can track what to revert later.
    /// </summary>
    public (string Status, List<string> AppliedTweakIds, string Message) ApplyProfile(string profileName)
    {
        lock (_lock)
        {
            _ps.Commands.Clear();
            _ps.AddCommand("Apply-Profile").AddParameter("Name", profileName);
            var results = _ps.Invoke<PSObject>();
            _ps.Commands.Clear();

            if (results.Count == 0)
                return ("error", new(), "No result from core.");

            var obj = results[0].Properties.ToDictionary(p => p.Name, p => p.Value);
            var status = obj["Status"]?.ToString() ?? "error";
            var message = obj["Message"]?.ToString() ?? "";

            var appliedIds = new List<string>();
            if (obj["Results"] is PSObject pr && pr.BaseObject is object[] resultArray)
            {
                foreach (var r in resultArray)
                {
                    if (r is PSObject rObj)
                    {
                        var rProps = rObj.Properties.ToDictionary(p => p.Name, p => p.Value);
                        if (rProps["Status"]?.ToString() == "success" && rProps["TweakId"] != null)
                            appliedIds.Add(rProps["TweakId"]!.ToString()!);
                    }
                }
            }

            return (status, appliedIds, message);
        }
    }

    // ─── Revert ─────────────────────────────────────────────

    /// <summary>
    /// Reverts a single tweak by ID. Used by the daemon to undo only the tweaks it applied.
    /// </summary>
    public (string Status, string Message) UndoTweak(string tweakId)
    {
        lock (_lock)
        {
            _ps.Commands.Clear();
            _ps.AddCommand("Undo-Tweak").AddParameter("Id", tweakId);
            var results = _ps.Invoke<PSObject>();
            _ps.Commands.Clear();

            if (results.Count == 0)
                return ("error", "No result from core.");

            var obj = results[0].Properties.ToDictionary(p => p.Name, p => p.Value);
            return (obj["Status"]?.ToString() ?? "error", obj["Message"]?.ToString() ?? "");
        }
    }

    // ─── Vanguard / VBS Checks ──────────────────────────────

    /// <summary>Returns true if Riot Vanguard anti-cheat is installed.</summary>
    public bool IsVanguardInstalled()
    {
        lock (_lock)
        {
            _ps.Commands.Clear();
            _ps.AddCommand("Test-VanguardInstalled");
            var results = _ps.Invoke<PSObject>();
            _ps.Commands.Clear();

            return results.Count > 0 && results[0].BaseObject is bool b && b;
        }
    }

    /// <summary>Checks if a tweak is compatible with VBS/Vanguard.</summary>
    public (bool? Compatible, string? Risk) TestVbsCompatibleTweak(string tweakId)
    {
        lock (_lock)
        {
            _ps.Commands.Clear();
            _ps.AddCommand("Test-VBSCompatibleTweak").AddParameter("Id", tweakId);
            var results = _ps.Invoke<PSObject>();
            _ps.Commands.Clear();

            if (results.Count == 0)
                return (null, null);

            var obj = results[0].Properties.ToDictionary(p => p.Name, p => p.Value);
            bool? compatible = obj["Compatible"] is PSObject pc && pc.Value is bool pb ? pb : null;
            string? risk = obj["Risk"]?.ToString();
            return (compatible, risk);
        }
    }

    // ─── System State ───────────────────────────────────────

    /// <summary>Reads the current system-state.json and returns active tweak IDs.</summary>
    public List<string> GetActiveTweaks()
    {
        var stateFile = Path.Combine(_corePath, "shared", "system-state.json");
        if (!File.Exists(stateFile))
            return new List<string>();

        try
        {
            var json = File.ReadAllText(stateFile);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("active_tweaks", out var arr))
            {
                return arr.EnumerateArray().Select(e => e.GetString() ?? "").ToList();
            }
        }
        catch { }
        return new List<string>();
    }

    public void Dispose()
    {
        _ps?.Dispose();
    }
}
