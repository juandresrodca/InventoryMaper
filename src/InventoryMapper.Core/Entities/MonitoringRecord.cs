using InventoryMapper.Core.Enums;

namespace InventoryMapper.Core.Entities;

public class MonitoringRecord : BaseEntity
{
    public Guid AssetId { get; set; }
    public Asset Asset { get; set; } = null!;

    public OnlineState State { get; set; }
    public MonitoringMethod Method { get; set; }
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
    public double? ResponseTimeMs { get; set; }
    public string? Details { get; set; }
    public bool Success { get; set; }
}
