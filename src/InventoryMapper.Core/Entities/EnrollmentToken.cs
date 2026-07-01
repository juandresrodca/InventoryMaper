namespace InventoryMapper.Core.Entities;

/// <summary>
/// Single-use, short-lived credential an admin issues so a new agent machine can complete its
/// first heartbeat and receive a persistent per-agent secret. Not a JWT — agents are not Identity users.
/// </summary>
public class EnrollmentToken : BaseEntity
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsConsumed { get; set; }
    public DateTime? ConsumedAt { get; set; }
    public string? ConsumedByAgentKey { get; set; }
}
