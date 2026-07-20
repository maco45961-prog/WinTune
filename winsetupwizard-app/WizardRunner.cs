using System.Diagnostics;
using System.Text.Json;
using WinSetupWizard.Hardware;
using WinSetupWizard.Integration;

namespace WinSetupWizard;

public class WizardState
{
    public int CurrentStep { get; set; } = 0;
    public string SelectedProfile { get; set; } = "debloat-estandar";
    public List<string> SelectedApps { get; set; } = new();
    public bool InstallDriversEnabled { get; set; } = true;
    public bool ApplyProfileEnabled { get; set; } = true;
    public bool Completed { get; set; }
    public HardwareInfo? Hardware { get; set; }
    public string LastReport { get; set; } = "";

    // Winget progress
    public int AppsInstalled { get; set; }
    public int AppsFailed { get; set; }
    public int AppsTotal { get; set; }

    // Profile apply result
    public ApplyProfileResult? ProfileResult { get; set; }
}

public static class WizardRunner
{
    public static async Task<string> InstallWingetApp(string appId)
    {
        var psi = new ProcessStartInfo("winget")
        {
            Arguments = $"install --id {appId} --accept-source-agreements --accept-package-agreements --silent",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        try
        {
            using var proc = Process.Start(psi);
            if (proc == null) return "error: could not start winget";

            var output = await proc.StandardOutput.ReadToEndAsync();
            var error = await proc.StandardError.ReadToEndAsync();
            proc.WaitForExit(120_000);

            if (proc.ExitCode == 0)
                return "success";
            else
                return $"error: {error}";
        }
        catch (Exception ex)
        {
            return $"error: {ex.Message}";
        }
    }

    public static async Task<bool> CheckWingetAvailable()
    {
        try
        {
            var psi = new ProcessStartInfo("winget")
            {
                Arguments = "--version",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
            };
            using var proc = Process.Start(psi);
            if (proc == null) return false;
            var output = await proc.StandardOutput.ReadToEndAsync();
            proc.WaitForExit(3000);
            return proc.ExitCode == 0 && !string.IsNullOrEmpty(output);
        }
        catch { return false; }
    }

    public static string ApplyProfileViaPowerShell(string profileName, string corePath)
    {
        var script = $@"
$ErrorActionPreference = 'Stop'
Import-Module '{Path.Combine(corePath, "WinOptimizer.Core.psd1")}' -Force
$result = Apply-Profile -Name '{profileName}'
$result | ConvertTo-Json -Depth 5
";
        var psi = new ProcessStartInfo("powershell")
        {
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script.Replace("\"", "\\\"")}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            Verb = "runas", // Request elevation
        };

        try
        {
            using var proc = Process.Start(psi);
            if (proc == null) return "error: could not start PowerShell";

            var output = proc.StandardOutput.ReadToEnd();
            var error = proc.StandardError.ReadToEnd();
            proc.WaitForExit(120_000);

            if (proc.ExitCode != 0 && string.IsNullOrEmpty(output))
                return $"error: {error}";

            try
            {
                var result = JsonSerializer.Deserialize<ApplyProfileResult>(output,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (result != null)
                    return $"{result.Status}|{result.Applied}|{result.Errors}|{result.Total}|{result.Message}";
            }
            catch { }

            return $"success|0|0|0|{output}";
        }
        catch (Exception ex)
        {
            return $"error: {ex.Message}";
        }
    }
}
