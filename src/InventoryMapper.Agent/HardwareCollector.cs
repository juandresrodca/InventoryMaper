using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.Versioning;

namespace InventoryMapper.Agent;

[SupportedOSPlatform("windows")]
public static class HardwareCollector
{
    public static DeviceInfo Collect()
    {
        var info = new DeviceInfo
        {
            Hostname = Environment.MachineName,
            OperatingSystem = Environment.OSVersion.Platform.ToString(),
            OsVersion = Environment.OSVersion.VersionString
        };

        // IP and MAC
        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces()
            .Where(n => n.OperationalStatus == OperationalStatus.Up &&
                        n.NetworkInterfaceType != NetworkInterfaceType.Loopback))
        {
            if (string.IsNullOrEmpty(info.MacAddress))
                info.MacAddress = string.Join(":", nic.GetPhysicalAddress().GetAddressBytes().Select(b => b.ToString("X2")));

            var ipProps = nic.GetIPProperties();
            foreach (var addr in ipProps.UnicastAddresses
                .Where(a => a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork))
            {
                info.IpAddress = addr.Address.ToString();
                break;
            }
        }

        // OS details via WMI
        try
        {
            using var mos = new ManagementObjectSearcher("SELECT Caption,Version FROM Win32_OperatingSystem");
            foreach (ManagementObject mo in mos.Get())
            {
                info.OperatingSystem = mo["Caption"]?.ToString() ?? info.OperatingSystem;
                info.OsVersion = mo["Version"]?.ToString() ?? info.OsVersion;
            }
        }
        catch { /* WMI unavailable */ }

        // OU from registry
        try
        {
            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Group Policy\State\Machine");
            if (key != null)
            {
                info.OrganizationalUnit = key.GetValue("Distinguished-Name")?.ToString();
            }
        }
        catch { /* Registry unavailable */ }

        return info;
    }
}

public record DeviceInfo(
    string Hostname = "",
    string IpAddress = "",
    string MacAddress = "",
    string OperatingSystem = "",
    string OsVersion = "",
    string OrganizationalUnit = ""
)
{
    public string Hostname { get; set; } = Hostname;
    public string IpAddress { get; set; } = IpAddress;
    public string MacAddress { get; set; } = MacAddress;
    public string OperatingSystem { get; set; } = OperatingSystem;
    public string OsVersion { get; set; } = OsVersion;
    public string? OrganizationalUnit { get; set; } = OrganizationalUnit;
}
