using InventoryMapper.Core.Enums;

namespace InventoryMapper.Core.Entities;

public class AlertNotification : BaseEntity
{
    public AlertType AlertType { get; set; }
    public AlertSeverity Severity { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; } = false;
    public bool IsResolved { get; set; } = false;
    public DateTime? ResolvedAt { get; set; }
    public string? ResolvedBy { get; set; }

    public Guid? AssetId { get; set; }
    public Asset? Asset { get; set; }
}
