namespace InventoryMapper.Core.Entities;

public class Blueprint : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileStoragePath { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }

    // Canvas dimensions (set when blueprint is configured)
    public double CanvasWidth { get; set; } = 1920;
    public double CanvasHeight { get; set; } = 1080;
    public double DefaultScale { get; set; } = 1.0;

    public Guid? LocationId { get; set; }
    public Location? Location { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Asset> PlacedAssets { get; set; } = [];
    public ICollection<BlueprintAnnotation> Annotations { get; set; } = [];
    public ICollection<BlueprintZone> Zones { get; set; } = [];
}
