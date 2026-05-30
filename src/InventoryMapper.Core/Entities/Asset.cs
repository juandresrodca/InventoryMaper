using InventoryMapper.Core.Enums;

namespace InventoryMapper.Core.Entities;

public class Asset : BaseEntity
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
    public OnlineState OnlineState { get; set; } = OnlineState.Unknown;
    public DateTime? LastCheckIn { get; set; }
    public string? Notes { get; set; }

    // Location
    public Guid? LocationId { get; set; }
    public Location? Location { get; set; }

    // Blueprint placement (null if not placed)
    public Guid? BlueprintId { get; set; }
    public Blueprint? Blueprint { get; set; }
    public double? BlueprintX { get; set; }
    public double? BlueprintY { get; set; }
    public double? BlueprintWidth { get; set; }
    public double? BlueprintHeight { get; set; }
    public int BlueprintZIndex { get; set; } = 0;

    // Monitoring
    public MonitoringMethod MonitoringMethod { get; set; } = MonitoringMethod.Ping;
    public DateTime? LastPingAt { get; set; }
    public bool IsMonitored { get; set; } = true;

    // Agent
    public Guid? AgentId { get; set; }
    public AgentRegistration? Agent { get; set; }

    // Navigation
    public ICollection<AssetTag> Tags { get; set; } = [];
    public ICollection<MonitoringRecord> MonitoringHistory { get; set; } = [];
    public ICollection<AlertNotification> Alerts { get; set; } = [];
}
