using System.Diagnostics;
using System.Net.NetworkInformation;

namespace WinNetTools.Diagnostics;

public class HopInfo
{
    public int Hop { get; set; }
    public string Address { get; set; } = "*";
    public long LatencyMs { get; set; }
    public string Hostname { get; set; } = "";
    public bool TimedOut { get; set; }
    public bool IsSlowest { get; set; }
}

public static class Traceroute
{
    public static List<HopInfo> RunTraceroute(string destination, int maxHops = 30)
    {
        var hops = new List<HopInfo>();

        using var ping = new Ping();
        var options = new PingOptions(1, true);

        for (int ttl = 1; ttl <= maxHops; ttl++)
        {
            options.Ttl = ttl;
            var sw = Stopwatch.StartNew();

            try
            {
                var reply = ping.Send(destination, 3000, new byte[32], options);
                sw.Stop();

                var hop = new HopInfo
                {
                    Hop = ttl,
                    TimedOut = reply.Status != IPStatus.Success,
                    LatencyMs = reply.Status == IPStatus.Success ? reply.RoundtripTime : 0,
                    Address = reply.Status == IPStatus.Success ? reply.Address?.ToString() ?? "*" : "*"
                };

                if (reply.Status == IPStatus.Success)
                {
                    try
                    {
                        var hostEntry = System.Net.Dns.GetHostEntry(reply.Address);
                        hop.Hostname = hostEntry.HostName;
                    }
                    catch { hop.Hostname = hop.Address; }
                }

                hops.Add(hop);

                if (reply.Status == IPStatus.Success && reply.Address?.ToString() == destination)
                    break;

                if (reply.Status == IPStatus.TtlExpired)
                    continue;
            }
            catch
            {
                hops.Add(new HopInfo { Hop = ttl, TimedOut = true, Address = "*" });
            }
        }

        if (hops.Any(h => !h.TimedOut))
        {
            var maxLatency = hops.Where(h => !h.TimedOut).MaxBy(h => h.LatencyMs);
            if (maxLatency != null)
                maxLatency.IsSlowest = true;
        }

        return hops;
    }
}
