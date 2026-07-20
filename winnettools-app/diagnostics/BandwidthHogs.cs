using System.Diagnostics;
using System.ServiceProcess;

namespace WinNetTools.Diagnostics;

public class NetworkProcessInfo
{
    public int Pid { get; set; }
    public string ProcessName { get; set; } = "";
    public string? WindowTitle { get; set; }
    public long BytesSent { get; set; }
    public long BytesReceived { get; set; }
    public double BandwidthMbps => Math.Round((BytesSent + BytesReceived) / 125_000.0, 2);
    public bool IsDeliveryOptimization => ProcessName.Contains("DeliveryOptimization", StringComparison.OrdinalIgnoreCase) ||
                                          ProcessName.Contains("dosvc", StringComparison.OrdinalIgnoreCase);
}

public static class BandwidthHogs
{
    public static List<NetworkProcessInfo> GetActiveProcesses()
    {
        var processes = new List<NetworkProcessInfo>();

        try
        {
            var psi = new ProcessStartInfo("powershell", @"-NoProfile -Command "
                + "\"Get-NetTCPConnection -State Established | Group-Object -Property OwningProcess | "
                + "ForEach-Object { $proc = Get-Process -Id $_.Name -ErrorAction SilentlyContinue; "
                + "[PSCustomObject]@{ Pid = $_.Name; ProcessName = $proc.ProcessName; Count = $_.Count } } "
                + "| Where-Object { $_.Count -gt 1 } | Sort-Object Count -Descending | "
                + "Select-Object Pid, ProcessName, Count | ConvertTo-Json\"")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var proc = Process.Start(psi);
            if (proc == null) return processes;

            var output = proc.StandardOutput.ReadToEnd().Trim();
            proc.WaitForExit(5000);
            if (string.IsNullOrEmpty(output)) return processes;

            var json = System.Text.Json.JsonDocument.Parse(output);
            var root = json.RootElement;

            var items = root.ValueKind == System.Text.Json.JsonValueKind.Array
                ? root.EnumerateArray().ToList()
                : new List<System.Text.Json.JsonElement> { root };

            foreach (var item in items)
            {
                try
                {
                    var np = new NetworkProcessInfo
                    {
                        Pid = item.GetProperty("Pid").GetInt32(),
                        ProcessName = item.GetProperty("ProcessName").GetString() ?? "unknown",
                    };

                    try
                    {
                        var p = System.Diagnostics.Process.GetProcessById(np.Pid);
                        np.WindowTitle = p.MainWindowTitle;
                    }
                    catch { }

                    processes.Add(np);
                }
                catch { }
            }
        }
        catch { }

        return processes;
    }

    public static bool IsDeliveryOptimizationActive()
    {
        try
        {
            using var svc = new ServiceController("DoSvc");
            return svc.Status == ServiceControllerStatus.Running;
        }
        catch { return false; }
    }
}
