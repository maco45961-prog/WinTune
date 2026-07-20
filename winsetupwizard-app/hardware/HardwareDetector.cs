using System.Management;

namespace WinSetupWizard.Hardware;

public class HardwareInfo
{
    public string CpuModel { get; set; } = "";
    public int CpuCoreCount { get; set; }
    public string GpuModel { get; set; } = "";
    public string GpuVendor { get; set; } = ""; // NVIDIA, AMD, Intel, Unknown
    public double TotalRamGb { get; set; }
    public bool IsSsd { get; set; }
    public string DiskType { get; set; } = "Unknown";
    public bool IsLaptop { get; set; }
    public string ChassisType { get; set; } = "Unknown";
    public string MotherboardManufacturer { get; set; } = "";
    public string MotherboardModel { get; set; } = "";
    public string CpuManufacturer { get; set; } = ""; // Intel, AMD

    public string RecommendedProfile { get; set; } = "debloat-estandar";
    public string RecommendedProfileDisplay { get; set; } = "Debloat Estándar";

    public string CpuDriverUrl { get; set; } = "";
    public string GpuDriverUrl { get; set; } = "";
    public string ChipsetDriverUrl { get; set; } = "";
}

public static class HardwareDetector
{
    public static HardwareInfo Detect()
    {
        var info = new HardwareInfo();

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
            foreach (var o in searcher.Get())
            {
                info.CpuModel = o["Name"]?.ToString()?.Trim() ?? "Unknown";
                info.CpuCoreCount = Convert.ToInt32(o["NumberOfCores"]);
                var manufacturer = o["Manufacturer"]?.ToString() ?? "";
                if (manufacturer.Contains("Intel", StringComparison.OrdinalIgnoreCase))
                    info.CpuManufacturer = "Intel";
                else if (manufacturer.Contains("AMD", StringComparison.OrdinalIgnoreCase))
                    info.CpuManufacturer = "AMD";
            }
        }
        catch { }

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");
            foreach (var o in searcher.Get())
            {
                var name = o["Name"]?.ToString() ?? "";
                var ram = o["AdapterRAM"] as uint?;

                if (name.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("GeForce", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("Quadro", StringComparison.OrdinalIgnoreCase))
                {
                    info.GpuVendor = "NVIDIA";
                }
                else if (name.Contains("AMD", StringComparison.OrdinalIgnoreCase) ||
                         name.Contains("Radeon", StringComparison.OrdinalIgnoreCase) ||
                         name.Contains("RDNA", StringComparison.OrdinalIgnoreCase))
                {
                    info.GpuVendor = "AMD";
                }
                else if (name.Contains("Intel", StringComparison.OrdinalIgnoreCase) ||
                         name.Contains("Arc", StringComparison.OrdinalIgnoreCase) ||
                         name.Contains("UHD Graphics", StringComparison.OrdinalIgnoreCase) ||
                         name.Contains("HD Graphics", StringComparison.OrdinalIgnoreCase) ||
                         name.Contains("Iris", StringComparison.OrdinalIgnoreCase))
                {
                    info.GpuVendor = "Intel";
                }

                // Prefer discrete GPU if multiple adapters
                if (info.GpuVendor == "NVIDIA" || info.GpuVendor == "AMD" || string.IsNullOrEmpty(info.GpuModel))
                {
                    info.GpuModel = name;
                }
            }
        }
        catch { }

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
            foreach (var o in searcher.Get())
                info.TotalRamGb = Math.Round(Convert.ToInt64(o["TotalPhysicalMemory"]) / (1024.0 * 1024 * 1024), 1);
        }
        catch { }

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");
            foreach (var o in searcher.Get())
            {
                var model = o["Model"]?.ToString() ?? "";
                var mediaType = o["MediaType"]?.ToString() ?? "";
                var interfaceType = o["InterfaceType"]?.ToString() ?? "";

                if (model.Contains("SSD", StringComparison.OrdinalIgnoreCase) ||
                    model.Contains("NVMe", StringComparison.OrdinalIgnoreCase) ||
                    mediaType.Contains("SSD", StringComparison.OrdinalIgnoreCase) ||
                    interfaceType.Contains("NVMe", StringComparison.OrdinalIgnoreCase))
                {
                    info.IsSsd = true;
                    info.DiskType = "SSD";
                    break;
                }
                else if (mediaType.Contains("HDD", StringComparison.OrdinalIgnoreCase) ||
                         interfaceType.Contains("IDE", StringComparison.OrdinalIgnoreCase) ||
                         interfaceType.Contains("SATA", StringComparison.OrdinalIgnoreCase))
                {
                    info.IsSsd = false;
                    info.DiskType = "HDD";
                }
            }
        }
        catch { }

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_SystemEnclosure");
            foreach (var o in searcher.Get())
            {
                var chassis = o["ChassisTypes"] as ushort[] ?? Array.Empty<ushort>();
                foreach (var c in chassis)
                {
                    // 8=laptop, 9=laptop, 10=laptop, 12=laptop, 14=laptop, 18=laptop
                    if (c is 8 or 9 or 10 or 12 or 14 or 18 or 21 or 30 or 31)
                    {
                        info.IsLaptop = true;
                        info.ChassisType = "Laptop";
                        break;
                    }
                    info.ChassisType = "Desktop";
                }
            }
        }
        catch { }

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_BaseBoard");
            foreach (var o in searcher.Get())
            {
                info.MotherboardManufacturer = o["Manufacturer"]?.ToString() ?? "";
                info.MotherboardModel = o["Product"]?.ToString() ?? "";
            }
        }
        catch { }

        // Determine recommended profile
        if (info.TotalRamGb <= 4 || (info.GpuVendor != "NVIDIA" && info.GpuVendor != "AMD" && info.TotalRamGb <= 8))
        {
            info.RecommendedProfile = "gaming-competitivo-lowend";
            info.RecommendedProfileDisplay = "Gaming Competitivo Low-End";
        }
        else if (info.IsLaptop)
        {
            info.RecommendedProfile = "debloat-estandar";
            info.RecommendedProfileDisplay = "Debloat Estándar";
        }
        else
        {
            info.RecommendedProfile = "debloat-agresivo";
            info.RecommendedProfileDisplay = "Debloat Agresivo";
        }

        // Official driver URLs
        if (info.GpuVendor == "NVIDIA")
            info.GpuDriverUrl = "https://www.nvidia.com/download/index.aspx";
        else if (info.GpuVendor == "AMD")
            info.GpuDriverUrl = "https://www.amd.com/en/support";
        else if (info.GpuVendor == "Intel")
            info.GpuDriverUrl = "https://www.intel.com/content/www/us/en/download-center/home.html";

        if (info.CpuManufacturer == "Intel")
            info.CpuDriverUrl = "https://www.intel.com/content/www/us/en/download-center/home.html";
        else if (info.CpuManufacturer == "AMD")
            info.CpuDriverUrl = "https://www.amd.com/en/support";

        // Chipset URL based on motherboard
        var mbMan = info.MotherboardManufacturer.ToLowerInvariant();
        if (mbMan.Contains("asus"))
            info.ChipsetDriverUrl = "https://www.asus.com/support/";
        else if (mbMan.Contains("msi"))
            info.ChipsetDriverUrl = "https://www.msi.com/support";
        else if (mbMan.Contains("gigabyte"))
            info.ChipsetDriverUrl = "https://www.gigabyte.com/support";
        else if (mbMan.Contains("asusrock") || mbMan.Contains("asrock"))
            info.ChipsetDriverUrl = "https://www.asrock.com/support/";
        else if (mbMan.Contains("lenovo"))
            info.ChipsetDriverUrl = "https://pcsupport.lenovo.com/";
        else if (mbMan.Contains("dell"))
            info.ChipsetDriverUrl = "https://www.dell.com/support";
        else if (mbMan.Contains("hp") || mbMan.Contains("hewlett"))
            info.ChipsetDriverUrl = "https://support.hp.com/";
        else
            info.ChipsetDriverUrl = "";

        return info;
    }
}
