using InventoryMapper.Core.Enums;

namespace InventoryMapper.Core.DTOs;

public class AssetFilterDto
{
    public string? Search { get; set; }
    public AssetType? AssetType { get; set; }
    public AssetStatus? Status { get; set; }
    public OnlineState? OnlineState { get; set; }
    public Guid? LocationId { get; set; }
    public Guid? BlueprintId { get; set; }
    public string? Department { get; set; }
    public bool? IsMonitored { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public string SortBy { get; set; } = "Hostname";
    public bool SortDescending { get; set; } = false;
}

public class CreateAssetDto
{
    public string Hostname { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? MacAddress { get; set; }
    public string? SerialNumber { get; set; }
    public AssetType AssetType { get; set; }
    public string? Manufacturer { get; set; }
    public string? Model { get; set; }
    public string? OperatingSystem { get; set; }
    public string? OsVersion { get; set; }
    public string? OrganizationalUnit { get; set; }
    public string? AssignedUser { get; set; }
    public string? Department { get; set; }
    public AssetStatus Status { get; set; } = AssetStatus.Active;
    public string? Notes { get; set; }
    public Guid? LocationId { get; set; }
    public MonitoringMethod MonitoringMethod { get; set; } = MonitoringMethod.Ping;
    public bool IsMonitored { get; set; } = true;
    public List<TagDto> Tags { get; set; } = [];
}

public class UpdateAssetDto : CreateAssetDto { }

public class TagDto
{
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string? Color { get; set; }
}

public class DashboardStatsDto
{
    public int TotalAssets { get; set; }
    public int OnlineAssets { get; set; }
    public int OfflineAssets { get; set; }
    public int UnknownAssets { get; set; }
    public int PhysicalAssets { get; set; }
    public int VirtualAssets { get; set; }
    public int NetworkDevices { get; set; }
    public int MissingAssets { get; set; }
    public int UnassignedAssets { get; set; }
    public int ActiveAlerts { get; set; }
    public int CriticalAlerts { get; set; }
    public int UnplacedAssets { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public List<AssetTypeCountDto> AssetsByType { get; set; } = [];
    public List<RecentCheckInDto> RecentCheckIns { get; set; } = [];
    public List<AlertSummaryDto> RecentAlerts { get; set; } = [];
    public List<OnlineHistoryPointDto> OnlineHistory { get; set; } = [];
}

public record AssetTypeCountDto(string Type, int Count, string Color);
public record RecentCheckInDto(string Hostname, DateTime CheckIn, string State, string AssetType);
public record AlertSummaryDto(string Title, string Severity, DateTime CreatedAt, string? Hostname);
public record OnlineHistoryPointDto(DateTime Timestamp, int Online, int Offline);
