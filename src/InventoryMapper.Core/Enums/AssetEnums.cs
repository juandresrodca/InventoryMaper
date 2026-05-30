namespace InventoryMapper.Core.Enums;

public enum AssetType
{
    PhysicalDevice,
    VirtualMachine,
    Container,
    Hypervisor,
    VirtualFarm,
    CloudInstance,
    NetworkSwitch,
    Router,
    Firewall,
    AccessPoint,
    Server,
    Printer,
    ThinClient,
    IoTDevice,
    Unknown
}

public enum AssetStatus
{
    Active,
    Inactive,
    Maintenance,
    Decommissioned,
    Missing,
    Pending
}

public enum OnlineState
{
    Online,
    Offline,
    Unknown,
    Unreachable
}

public enum LocationType
{
    Physical,
    Virtual,
    Cloud,
    DefaultVirtualSpace
}

public enum MonitoringMethod
{
    AgentHeartbeat,
    Ping,
    SNMP,
    SSH,
    API,
    None
}

public enum ImportStatus
{
    Pending,
    Processing,
    Completed,
    CompletedWithErrors,
    Failed
}

public enum AlertSeverity
{
    Info,
    Warning,
    Critical
}

public enum AlertType
{
    DeviceOffline,
    OUMismatch,
    AgentHandshakeFailed,
    UnassignedDevice,
    MissingDevice,
    ImportError,
    StatusChange
}
