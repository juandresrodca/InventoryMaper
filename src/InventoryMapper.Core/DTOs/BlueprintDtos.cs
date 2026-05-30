namespace InventoryMapper.Core.DTOs;

public class CreateBlueprintDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? LocationId { get; set; }
    public double CanvasWidth { get; set; } = 1920;
    public double CanvasHeight { get; set; } = 1080;
}

public class BlueprintLayoutDto
{
    public Guid BlueprintId { get; set; }
    public List<AssetPlacementDto> AssetPlacements { get; set; } = [];
    public List<AnnotationDto> Annotations { get; set; } = [];
    public List<ZoneDto> Zones { get; set; } = [];
}

public class AssetPlacementDto
{
    public Guid AssetId { get; set; }
    public string Hostname { get; set; } = string.Empty;
    public string AssetType { get; set; } = string.Empty;
    public string OnlineState { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; } = 48;
    public double Height { get; set; } = 48;
    public int ZIndex { get; set; } = 5;
}

public class AnnotationDto
{
    public Guid Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Color { get; set; } = "#FFD700";
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; } = 100;
    public double Height { get; set; } = 30;
    public int ZIndex { get; set; } = 10;
}

public class ZoneDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#3B82F680";
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; } = 200;
    public double Height { get; set; } = 200;
    public int ZIndex { get; set; } = 1;
}

public class CreateAnnotationDto
{
    public string Label { get; set; } = string.Empty;
    public string? Color { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
}

public class CreateZoneDto
{
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; } = 200;
    public double Height { get; set; } = 200;
}
