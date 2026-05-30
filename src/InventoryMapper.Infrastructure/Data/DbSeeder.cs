using InventoryMapper.Core.Entities;
using InventoryMapper.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InventoryMapper.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db, ILogger logger)
    {
        await db.Database.MigrateAsync();

        if (await db.Locations.AnyAsync()) return;

        logger.LogInformation("Seeding database with initial data...");

        var locations = new List<Location>
        {
            new() { Name = "Default Virtual Space", Description = "Holding area for unassigned assets", LocationType = LocationType.DefaultVirtualSpace, IsDefault = true },
            new() { Name = "Headquarters - Floor 1", LocationType = LocationType.Physical, Building = "HQ", Floor = "1", Site = "Main Campus" },
            new() { Name = "Headquarters - Server Room", LocationType = LocationType.Physical, Building = "HQ", Floor = "B1", Room = "SRV-001", Site = "Main Campus" },
            new() { Name = "Datacenter A", LocationType = LocationType.Physical, Building = "DC-A", Site = "Datacenter Campus" },
            new() { Name = "VMware Cluster - PROD", LocationType = LocationType.Virtual, VirtualEnvironment = "VMware vSphere", ClusterName = "PROD-CLUSTER-01" },
            new() { Name = "Azure Cloud - East US", LocationType = LocationType.Cloud, CloudProvider = "Microsoft Azure", CloudRegion = "eastus" },
        };

        db.Locations.AddRange(locations);
        await db.SaveChangesAsync();

        var hqFloor1 = locations.First(l => l.Name == "Headquarters - Floor 1");
        var serverRoom = locations.First(l => l.Name == "Headquarters - Server Room");
        var vmCluster = locations.First(l => l.Name == "VMware Cluster - PROD");
        var defaultVirtual = locations.First(l => l.IsDefault);

        var assets = new List<Asset>
        {
            new() { Hostname = "WS-HQ-001", IpAddress = "10.0.1.10", MacAddress = "AA:BB:CC:DD:EE:01", SerialNumber = "SN001", AssetType = AssetType.PhysicalDevice, Manufacturer = "Dell", Model = "OptiPlex 7090", OperatingSystem = "Windows 11 Pro", OrganizationalUnit = "OU=Workstations,DC=corp,DC=local", AssignedUser = "jsmith", Department = "IT", LocationId = hqFloor1.Id, Status = AssetStatus.Active, OnlineState = OnlineState.Online, LastCheckIn = DateTime.UtcNow.AddMinutes(-5), MonitoringMethod = MonitoringMethod.Ping },
            new() { Hostname = "WS-HQ-002", IpAddress = "10.0.1.11", MacAddress = "AA:BB:CC:DD:EE:02", SerialNumber = "SN002", AssetType = AssetType.PhysicalDevice, Manufacturer = "HP", Model = "EliteDesk 800 G6", OperatingSystem = "Windows 11 Pro", OrganizationalUnit = "OU=Workstations,DC=corp,DC=local", AssignedUser = "mjohnson", Department = "Finance", LocationId = hqFloor1.Id, Status = AssetStatus.Active, OnlineState = OnlineState.Online, LastCheckIn = DateTime.UtcNow.AddMinutes(-12) },
            new() { Hostname = "LT-HQ-001", IpAddress = "10.0.1.50", MacAddress = "AA:BB:CC:DD:EE:10", SerialNumber = "SN010", AssetType = AssetType.PhysicalDevice, Manufacturer = "Lenovo", Model = "ThinkPad T14s", OperatingSystem = "Windows 11 Pro", OrganizationalUnit = "OU=Laptops,DC=corp,DC=local", AssignedUser = "bwilliams", Department = "Engineering", LocationId = hqFloor1.Id, Status = AssetStatus.Active, OnlineState = OnlineState.Offline, LastCheckIn = DateTime.UtcNow.AddHours(-3) },
            new() { Hostname = "SRV-PROD-001", IpAddress = "10.0.2.10", MacAddress = "BB:CC:DD:EE:FF:01", SerialNumber = "SRV001", AssetType = AssetType.Server, Manufacturer = "Dell", Model = "PowerEdge R750", OperatingSystem = "Windows Server 2022", OrganizationalUnit = "OU=Servers,DC=corp,DC=local", Department = "IT", LocationId = serverRoom.Id, Status = AssetStatus.Active, OnlineState = OnlineState.Online, LastCheckIn = DateTime.UtcNow.AddMinutes(-1), MonitoringMethod = MonitoringMethod.AgentHeartbeat },
            new() { Hostname = "SRV-PROD-002", IpAddress = "10.0.2.11", MacAddress = "BB:CC:DD:EE:FF:02", SerialNumber = "SRV002", AssetType = AssetType.Server, Manufacturer = "HP", Model = "ProLiant DL380 Gen10", OperatingSystem = "Ubuntu Server 22.04", OrganizationalUnit = "OU=Servers,DC=corp,DC=local", Department = "IT", LocationId = serverRoom.Id, Status = AssetStatus.Active, OnlineState = OnlineState.Online, LastCheckIn = DateTime.UtcNow.AddMinutes(-2), MonitoringMethod = MonitoringMethod.Ping },
            new() { Hostname = "VM-APP-001", IpAddress = "10.0.3.10", AssetType = AssetType.VirtualMachine, Manufacturer = "VMware", Model = "Virtual Machine", OperatingSystem = "Windows Server 2019", OrganizationalUnit = "OU=VMs,DC=corp,DC=local", Department = "Development", LocationId = vmCluster.Id, Status = AssetStatus.Active, OnlineState = OnlineState.Online, LastCheckIn = DateTime.UtcNow.AddMinutes(-3) },
            new() { Hostname = "VM-DB-001", IpAddress = "10.0.3.20", AssetType = AssetType.VirtualMachine, Manufacturer = "VMware", Model = "Virtual Machine", OperatingSystem = "Ubuntu Server 22.04", Department = "Database", LocationId = vmCluster.Id, Status = AssetStatus.Active, OnlineState = OnlineState.Online, LastCheckIn = DateTime.UtcNow.AddMinutes(-4) },
            new() { Hostname = "SW-CORE-001", IpAddress = "10.0.0.1", MacAddress = "CC:DD:EE:FF:00:01", SerialNumber = "SW001", AssetType = AssetType.NetworkSwitch, Manufacturer = "Cisco", Model = "Catalyst 9300", Department = "Network", LocationId = serverRoom.Id, Status = AssetStatus.Active, OnlineState = OnlineState.Online, LastCheckIn = DateTime.UtcNow.AddMinutes(-1), MonitoringMethod = MonitoringMethod.Ping },
            new() { Hostname = "FW-EDGE-001", IpAddress = "192.168.1.1", MacAddress = "CC:DD:EE:FF:00:02", SerialNumber = "FW001", AssetType = AssetType.Firewall, Manufacturer = "Fortinet", Model = "FortiGate 600E", Department = "Network", LocationId = serverRoom.Id, Status = AssetStatus.Active, OnlineState = OnlineState.Online, LastCheckIn = DateTime.UtcNow.AddMinutes(-1) },
            new() { Hostname = "UNASSIGNED-001", IpAddress = "10.0.99.50", AssetType = AssetType.PhysicalDevice, Manufacturer = "Unknown", Model = "Unknown", Status = AssetStatus.Pending, OnlineState = OnlineState.Unknown, LocationId = defaultVirtual.Id },
        };

        db.Assets.AddRange(assets);
        await db.SaveChangesAsync();

        var tags = new List<AssetTag>
        {
            new() { AssetId = assets[3].Id, Key = "environment", Value = "production", Color = "#EF4444" },
            new() { AssetId = assets[3].Id, Key = "criticality", Value = "high", Color = "#F97316" },
            new() { AssetId = assets[4].Id, Key = "environment", Value = "production", Color = "#EF4444" },
            new() { AssetId = assets[5].Id, Key = "environment", Value = "development", Color = "#3B82F6" },
            new() { AssetId = assets[6].Id, Key = "environment", Value = "production", Color = "#EF4444" },
        };

        db.AssetTags.AddRange(tags);

        var alerts = new List<AlertNotification>
        {
            new() { AlertType = AlertType.DeviceOffline, Severity = AlertSeverity.Warning, Title = "Device Offline", Message = $"{assets[2].Hostname} has been offline for 3 hours", AssetId = assets[2].Id },
            new() { AlertType = AlertType.UnassignedDevice, Severity = AlertSeverity.Info, Title = "Unassigned Device", Message = $"{assets[9].Hostname} is not assigned to any user", AssetId = assets[9].Id },
        };

        db.Alerts.AddRange(alerts);
        await db.SaveChangesAsync();

        logger.LogInformation("Database seeding complete.");
    }
}
