using System.Diagnostics;
using System.Net.NetworkInformation;

namespace WinNetTools.Diagnostics;

public class PingJitterResult
{
    public string Target { get; set; } = "";
    public long MinLatencyMs { get; set; }
    public long MaxLatencyMs { get; set; }
    public double AvgLatencyMs { get; set; }
    public double JitterMs { get; set; }
    public double PacketLossPercent { get; set; }
    public int Sent { get; set; }
    public int Received { get; set; }
    public int Lost { get; set; }
    public string Status => PacketLossPercent >= 20 ? "critical" :
                            PacketLossPercent >= 5 ? "poor" :
                            AvgLatencyMs > 120 ? "poor" :
                            AvgLatencyMs > 60 ? "fair" : "good";
}

public static class PingJitter
{
    private static readonly string[] DefaultTargets = { "1.1.1.1", "8.8.8.8", "9.9.9.9" };

    public static List<PingJitterResult> RunPingTest(IEnumerable<string>? targets = null, int count = 10)
    {
        var hosts = (targets ?? DefaultTargets).ToList();
        var results = new List<PingJitterResult>();

        foreach (var host in hosts)
        {
            var result = PingTarget(host, count);
            results.Add(result);
        }

        return results;
    }

    public static PingJitterResult PingTarget(string target, int count = 10)
    {
        using var ping = new Ping();
        var rtts = new List<long>();
        int lost = 0;
        int sent = 0;

        for (int i = 0; i < count; i++)
        {
            sent++;
            try
            {
                var reply = ping.Send(target, 3000);
                if (reply is { Status: IPStatus.Success })
                {
                    rtts.Add(reply.RoundtripTime);
                }
                else
                {
                    lost++;
                }
            }
            catch
            {
                lost++;
            }
        }

        int received = rtts.Count;
        double packetLoss = sent > 0 ? (double)lost / sent * 100 : 0;
        double avg = received > 0 ? rtts.Average() : 0;
        long min = received > 0 ? rtts.Min() : 0;
        long max = received > 0 ? rtts.Max() : 0;
        double jitter = received > 1 ? MAD(rtts) : 0;

        return new PingJitterResult
        {
            Target = target,
            MinLatencyMs = min,
            MaxLatencyMs = max,
            AvgLatencyMs = Math.Round(avg, 1),
            JitterMs = Math.Round(jitter, 1),
            PacketLossPercent = Math.Round(packetLoss, 1),
            Sent = sent,
            Received = received,
            Lost = lost
        };
    }

    private static double MAD(List<long> values)
    {
        double mean = values.Average();
        return values.Sum(v => Math.Abs(v - mean)) / values.Count;
    }
}
