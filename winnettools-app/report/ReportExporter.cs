using System.Text;
using WinNetTools.Diagnostics;
using WinNetTools.Integration;

namespace WinNetTools.Report;

public static class ReportExporter
{
    public static string GenerateReport(
        List<PingJitterResult> pingResults,
        List<HopInfo> tracerouteHops,
        List<DnsResult> dnsResults,
        ConnectionInfo connectionInfo,
        List<NetworkProcessInfo> activeProcesses,
        bool deliveryOptimizationActive,
        SystemState? systemState)
    {
        var sb = new StringBuilder();
        sb.AppendLine("===========================================");
        sb.AppendLine("  WinNetTools — Reporte de Diagnóstico");
        sb.AppendLine("  WinNetTools — Diagnostic Report");
        sb.AppendLine("===========================================");
        sb.AppendLine($"Generado: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();

        // Connection type
        sb.AppendLine("--- TIPO DE CONEXIÓN / CONNECTION TYPE ---");
        sb.AppendLine($"Tipo: {connectionInfo.Type}");
        sb.AppendLine($"Adaptador: {connectionInfo.AdapterName ?? "N/A"}");
        sb.AppendLine($"Velocidad: {connectionInfo.LinkSpeedMbps?.ToString() ?? "N/A"} Mbps");
        sb.AppendLine($"IPs: {string.Join(", ", connectionInfo.IpAddresses)}");
        sb.AppendLine();

        // Ping results
        sb.AppendLine("--- PING / JITTER / PACKET LOSS ---");
        foreach (var r in pingResults)
        {
            sb.AppendLine($"{r.Target}: prom={r.AvgLatencyMs}ms, mín={r.MinLatencyMs}ms, máx={r.MaxLatencyMs}ms, jitter={r.JitterMs}ms, pérdida={r.PacketLossPercent}% ({r.Lost}/{r.Sent})");
        }
        sb.AppendLine();

        // Traceroute
        sb.AppendLine("--- TRACEROUTE ---");
        foreach (var h in tracerouteHops)
        {
            var slow = h.IsSlowest ? " <-- MÁS LENTO / SLOWEST" : "";
            var to = h.TimedOut ? "*" : $"{h.LatencyMs}ms";
            sb.AppendLine($"  {h.Hop,2}. {h.Address,-15} {to,8}{slow}");
        }
        sb.AppendLine();

        // DNS
        sb.AppendLine("--- COMPARACIÓN DNS / DNS COMPARISON ---");
        foreach (var d in dnsResults)
        {
            var ok = d.Success ? "OK" : "FALLO";
            sb.AppendLine($"  {d.Name,-12} ({d.Server}): {d.ResolveTimeMs,7:F1}ms [{ok}] -> {d.ResolvedIp ?? d.ErrorMessage}");
        }
        sb.AppendLine();

        // Bandwidth hogs
        sb.AppendLine("--- PROCESOS DE RED / NETWORK PROCESSES ---");
        if (activeProcesses.Count == 0)
            sb.AppendLine("  (ninguno detectado / none detected)");
        else
        {
            foreach (var p in activeProcesses)
                sb.AppendLine($"  PID {p.Pid,-6} {p.ProcessName,-30} {p.WindowTitle ?? ""}");
        }
        sb.AppendLine($"Delivery Optimization activo: {deliveryOptimizationActive}");
        sb.AppendLine();

        // System state
        sb.AppendLine("--- ESTADO DEL SISTEMA / SYSTEM STATE ---");
        if (systemState != null)
        {
            sb.AppendLine($"Hostname: {systemState.SystemSnapshot?.Hostname ?? "N/A"}");
            sb.AppendLine($"OS: {systemState.SystemSnapshot?.OsVersion ?? "N/A"}");
            sb.AppendLine($"RAM: {systemState.SystemSnapshot?.TotalRamGb?.ToString("F1") ?? "N/A"} GB");
            sb.AppendLine($"CPU: {systemState.SystemSnapshot?.CpuName ?? "N/A"}");
            sb.AppendLine($"Vanguard: {(systemState.SystemSnapshot?.VanguardInstalled == true ? "SÍ / YES" : "No")}");

            var applied = CoreStateReader.GetAppliedNetworkTweaks(systemState);
            var missing = CoreStateReader.GetMissingNetworkTweaks(systemState);

            sb.AppendLine();
            sb.AppendLine("Tweaks de red aplicados:");
            sb.AppendLine(string.Join(", ", applied.Count > 0 ? applied : new[] { "(ninguno / none)" }));

            sb.AppendLine("Tweaks de red NO aplicados (sugeridos):");
            sb.AppendLine(string.Join(", ", missing.Count > 0 ? missing : new[] { "(todos aplicados / all applied)" }));
        }
        else
        {
            sb.AppendLine("  (no se pudo leer system-state.json / could not read system-state.json)");
        }
        sb.AppendLine();
        sb.AppendLine("===========================================");
        sb.AppendLine("  Fin del reporte / End of report");
        sb.AppendLine("===========================================");

        return sb.ToString();
    }

    public static string SaveReport(string content)
    {
        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WinNetTools", "reports");

        Directory.CreateDirectory(appData);
        var filename = $"WinNetTools-report-{DateTime.Now:yyyyMMdd-HHmmss}.txt";
        var path = Path.Combine(appData, filename);
        File.WriteAllText(path, content, Encoding.UTF8);
        return path;
    }
}
