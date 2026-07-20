using System.Diagnostics;
using System.Net.NetworkInformation;

namespace WinBenchmark.Capture;

public class PingJitterResult
{
    public double AvgPingMs { get; set; }
    public double JitterMs { get; set; }
    public double PacketLossPercent { get; set; }
}

public static class PingJitter
{
    public static readonly string[] DefaultTargets = { "1.1.1.1", "8.8.8.8", "9.9.9.9" };

    public static PingJitterResult RunPingTest(IEnumerable<string>? targets = null, int count = 5)
    {
        var hosts = (targets ?? DefaultTargets).ToList();
        var allRtts = new List<long>();
        int totalSent = 0;
        int totalLost = 0;

        foreach (var host in hosts)
        {
            using var ping = new Ping();
            for (int i = 0; i < count; i++)
            {
                totalSent++;
                try
                {
                    var reply = ping.Send(host, 2000);
                    if (reply is { Status: IPStatus.Success })
                        allRtts.Add(reply.RoundtripTime);
                    else
                        totalLost++;
                }
                catch { totalLost++; }
            }
        }

        int received = allRtts.Count;
        double packetLoss = totalSent > 0 ? (double)totalLost / totalSent * 100 : 0;
        double avg = received > 0 ? allRtts.Average() : 0;
        double jitter = received > 1 ? MAD(allRtts) : 0;

        return new PingJitterResult
        {
            AvgPingMs = Math.Round(avg, 1),
            JitterMs = Math.Round(jitter, 1),
            PacketLossPercent = Math.Round(packetLoss, 1),
        };
    }

    private static double MAD(List<long> values)
    {
        double mean = values.Average();
        return values.Sum(v => Math.Abs(v - mean)) / values.Count;
    }
}
