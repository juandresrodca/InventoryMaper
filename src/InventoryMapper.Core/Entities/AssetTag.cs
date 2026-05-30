namespace InventoryMapper.Core.Entities;

public class AssetTag : BaseEntity
{
    public Guid AssetId { get; set; }
    public Asset Asset { get; set; } = null!;

    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string? Color { get; set; } = "#3B82F6";
}
