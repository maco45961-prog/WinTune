using System.Diagnostics;

namespace WinNetTools.Diagnostics;

public class DnsResult
{
    public string Name { get; set; } = "";
    public string Server { get; set; } = "";
    public string TargetHost { get; set; } = "";
    public double ResolveTimeMs { get; set; }
    public bool Success { get; set; }
    public string? ResolvedIp { get; set; }
    public string? ErrorMessage { get; set; }
}

public static class DnsCompare
{
    private static readonly (string Name, string Server)[] DnsServers =
    {
        ("Cloudflare", "1.1.1.1"),
        ("Google", "8.8.8.8"),
        ("Quad9", "9.9.9.9"),
        ("Actual del sistema", ""),  // empty = use system DNS
    };

    public static DnsResult GetSystemDns()
    {
        return ResolveDns("Actual del sistema", "", "playvalorant.com");
    }

    public static List<DnsResult> CompareDns(string targetHost = "playvalorant.com")
    {
        var results = new List<DnsResult>();

        foreach (var (name, server) in DnsServers)
        {
            var result = ResolveDns(name, server, targetHost);
            results.Add(result);
        }

        return results;
    }

    private static DnsResult ResolveDns(string name, string server, string host)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            var args = string.IsNullOrEmpty(server)
                ? $"/nslookup {host}"
                : $"/nslookup {host} {server}";

            var psi = new ProcessStartInfo("powershell", $"-NoProfile -Command \"Resolve-DnsName -Name '{host}'{(string.IsNullOrEmpty(server) ? "" : $" -Server '{server}'")} | Select-Object -First 1 IPAddress | Format-Table -HideTableHeaders\"")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var proc = Process.Start(psi);
            if (proc == null)
            {
                return new DnsResult { Name = name, Server = server, TargetHost = host, Success = false, ErrorMessage = "Could not start process" };
            }

            var output = proc.StandardOutput.ReadToEnd().Trim();
            var error = proc.StandardError.ReadToEnd().Trim();
            proc.WaitForExit(5000);
            sw.Stop();

            bool success = proc.ExitCode == 0 && !string.IsNullOrWhiteSpace(output);
            return new DnsResult
            {
                Name = name,
                Server = string.IsNullOrEmpty(server) ? "Sistema" : server,
                TargetHost = host,
                ResolveTimeMs = Math.Round(sw.Elapsed.TotalMilliseconds, 1),
                Success = success,
                ResolvedIp = success ? output.Split('\n').FirstOrDefault()?.Trim() : null,
                ErrorMessage = success ? null : (string.IsNullOrEmpty(error) ? "No se pudo resolver" : error)
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new DnsResult
            {
                Name = name,
                Server = string.IsNullOrEmpty(server) ? "Sistema" : server,
                TargetHost = host,
                Success = false,
                ErrorMessage = ex.Message,
                ResolveTimeMs = Math.Round(sw.Elapsed.TotalMilliseconds, 1)
            };
        }
    }
}
