using InventoryMapper.Core.Enums;

namespace InventoryMapper.Core.Entities;

public class Location : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public LocationType LocationType { get; set; }

    // Physical location fields
    public string? Building { get; set; }
    public string? Floor { get; set; }
    public string? Room { get; set; }
    public string? Rack { get; set; }
    public string? Site { get; set; }
    public string? Address { get; set; }

    // Virtual location fields
    public string? VirtualEnvironment { get; set; }
    public string? ClusterName { get; set; }
    public string? CloudProvider { get; set; }
    public string? CloudRegion { get; set; }

    public bool IsDefault { get; set; } = false;

    public ICollection<Asset> Assets { get; set; } = [];
    public ICollection<Blueprint> Blueprints { get; set; } = [];
}
