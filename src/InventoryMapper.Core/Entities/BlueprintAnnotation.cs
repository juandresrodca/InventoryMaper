namespace InventoryMapper.Core.Entities;

public class BlueprintAnnotation : BaseEntity
{
    public Guid BlueprintId { get; set; }
    public Blueprint Blueprint { get; set; } = null!;

    public string Label { get; set; } = string.Empty;
    public string? Color { get; set; } = "#FFD700";
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; } = 100;
    public double Height { get; set; } = 30;
    public int ZIndex { get; set; } = 10;
}

public class BlueprintZone : BaseEntity
{
    public Guid BlueprintId { get; set; }
    public Blueprint Blueprint { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; } = "#3B82F680";
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; } = 200;
    public double Height { get; set; } = 200;
    public int ZIndex { get; set; } = 1;
}
