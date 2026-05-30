namespace InventoryMapper.Core.Entities;

public class AgentRegistration : BaseEntity
{
    public string AgentKey { get; set; } = string.Empty;
    public string Hostname { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? MacAddress { get; set; }
    public string? OrganizationalUnit { get; set; }
    public string AgentVersion { get; set; } = string.Empty;
    public DateTime LastHeartbeat { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsHandshakeValid { get; set; } = false;
    public string? HandshakeToken { get; set; }
    public DateTime? HandshakeExpiry { get; set; }
    public string? OperatingSystem { get; set; }
    public string? OsVersion { get; set; }

    public Guid? AssetId { get; set; }
    public Asset? Asset { get; set; }
}
