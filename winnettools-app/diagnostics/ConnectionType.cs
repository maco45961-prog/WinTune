using System.Management;

namespace WinNetTools.Diagnostics;

public class ConnectionInfo
{
    public string Type { get; set; } = "Desconocido";
    public bool IsWiFi { get; set; }
    public string? Ssid { get; set; }
    public long? LinkSpeedMbps { get; set; }
    public string? AdapterName { get; set; }
    public List<string> IpAddresses { get; set; } = new();
}

public static class ConnectionType
{
    public static ConnectionInfo DetectConnection()
    {
        var info = new ConnectionInfo();

        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT * FROM Win32_NetworkAdapter WHERE NetEnabled = true AND AdapterTypeId = 0");
            var adapters = searcher.Get().Cast<ManagementObject>().ToList();

            foreach (var adapter in adapters)
            {
                var name = adapter["Name"]?.ToString() ?? "";
                var speed = adapter["Speed"] as ulong?;
                var mac = adapter["MacAddress"]?.ToString();

                if (name.Contains("Wi-Fi", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("Wireless", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("WLAN", StringComparison.OrdinalIgnoreCase))
                {
                    info.Type = "Wi-Fi";
                    info.IsWiFi = true;
                    info.AdapterName = name;
                    info.LinkSpeedMbps = speed.HasValue ? (long?)(speed.Value / 1_000_000) : null;
                }
                else if (name.Contains("Ethernet", StringComparison.OrdinalIgnoreCase) ||
                         name.Contains("Realtek", StringComparison.OrdinalIgnoreCase) ||
                         name.Contains("Intel", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrEmpty(info.AdapterName))
                    {
                        info.Type = "Ethernet";
                        info.IsWiFi = false;
                        info.AdapterName = name;
                        info.LinkSpeedMbps = speed.HasValue ? (long?)(speed.Value / 1_000_000) : null;
                    }
                }
            }

            try
            {
                using var ipSearcher = new ManagementObjectSearcher(
                    "SELECT * FROM Win32_NetworkAdapterConfiguration WHERE IPEnabled = true");
                foreach (var config in ipSearcher.Get().Cast<ManagementObject>())
                {
                    var ips = config["IPAddress"] as string[];
                    if (ips != null)
                    {
                        foreach (var ip in ips)
                        {
                            if (!ip.Contains(':') && ip != "0.0.0.0")
                                info.IpAddresses.Add(ip);
                        }
                    }

                    if (string.IsNullOrEmpty(info.AdapterName))
                    {
                        var desc = config["Description"]?.ToString();
                        if (desc != null)
                        {
                            if (desc.Contains("Wi-Fi", StringComparison.OrdinalIgnoreCase) ||
                                desc.Contains("Wireless", StringComparison.OrdinalIgnoreCase))
                            {
                                info.Type = "Wi-Fi";
                                info.IsWiFi = true;
                                info.AdapterName = desc;
                            }
                            else if (desc.Contains("Ethernet", StringComparison.OrdinalIgnoreCase) ||
                                     desc.Contains("PCIe", StringComparison.OrdinalIgnoreCase))
                            {
                                info.Type = "Ethernet";
                                info.IsWiFi = false;
                                info.AdapterName = desc;
                            }
                        }
                    }
                }
            }
            catch { }
        }
        catch { }

        return info;
    }
}
