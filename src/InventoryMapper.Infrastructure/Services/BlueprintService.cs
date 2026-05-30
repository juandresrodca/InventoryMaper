using InventoryMapper.Core.DTOs;
using InventoryMapper.Core.Entities;
using InventoryMapper.Core.Interfaces;
using InventoryMapper.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace InventoryMapper.Infrastructure.Services;

public class BlueprintService(ApplicationDbContext db, IHostEnvironment env) : IBlueprintService
{
    private string StoragePath => Path.Combine(env.ContentRootPath, "wwwroot", "blueprints");

    public async Task<IEnumerable<Blueprint>> GetAllBlueprintsAsync(CancellationToken ct = default)
        => await db.Blueprints
            .Include(b => b.Location)
            .Include(b => b.PlacedAssets)
            .OrderBy(b => b.Name)
            .ToListAsync(ct);

    public async Task<Blueprint?> GetBlueprintByIdAsync(Guid id, CancellationToken ct = default)
        => await db.Blueprints
            .Include(b => b.Location)
            .Include(b => b.PlacedAssets).ThenInclude(a => a.Tags)
            .Include(b => b.Annotations)
            .Include(b => b.Zones)
            .FirstOrDefaultAsync(b => b.Id == id, ct);

    public async Task<Blueprint> CreateBlueprintAsync(CreateBlueprintDto dto, Stream fileStream, string fileName, string mimeType, CancellationToken ct = default)
    {
        Directory.CreateDirectory(StoragePath);
        var storedName = $"{Guid.NewGuid()}{Path.GetExtension(fileName)}";
        var fullPath = Path.Combine(StoragePath, storedName);

        await using var fs = File.Create(fullPath);
        await fileStream.CopyToAsync(fs, ct);
        var fileSize = fs.Length;

        var blueprint = new Blueprint
        {
            Name = dto.Name,
            Description = dto.Description,
            LocationId = dto.LocationId,
            FileName = storedName,
            FileStoragePath = $"/blueprints/{storedName}",
            MimeType = mimeType,
            FileSizeBytes = fileSize,
            CanvasWidth = dto.CanvasWidth,
            CanvasHeight = dto.CanvasHeight
        };

        db.Blueprints.Add(blueprint);
        await db.SaveChangesAsync(ct);
        return blueprint;
    }

    public async Task DeleteBlueprintAsync(Guid id, CancellationToken ct = default)
    {
        var bp = await db.Blueprints.FindAsync([id], ct)
            ?? throw new KeyNotFoundException($"Blueprint {id} not found");

        var fullPath = Path.Combine(StoragePath, bp.FileName);
        if (File.Exists(fullPath)) File.Delete(fullPath);

        bp.IsDeleted = true;
        await db.SaveChangesAsync(ct);
    }

    public async Task<BlueprintLayoutDto> GetBlueprintLayoutAsync(Guid id, CancellationToken ct = default)
    {
        var bp = await db.Blueprints
            .Include(b => b.PlacedAssets)
            .Include(b => b.Annotations)
            .Include(b => b.Zones)
            .FirstOrDefaultAsync(b => b.Id == id, ct)
            ?? throw new KeyNotFoundException($"Blueprint {id} not found");

        return new BlueprintLayoutDto
        {
            BlueprintId = id,
            AssetPlacements = bp.PlacedAssets.Select(a => new AssetPlacementDto
            {
                AssetId = a.Id,
                Hostname = a.Hostname,
                AssetType = a.AssetType.ToString(),
                OnlineState = a.OnlineState.ToString(),
                X = a.BlueprintX ?? 0,
                Y = a.BlueprintY ?? 0,
                Width = a.BlueprintWidth ?? 48,
                Height = a.BlueprintHeight ?? 48,
                ZIndex = a.BlueprintZIndex
            }).ToList(),
            Annotations = bp.Annotations.Select(a => new AnnotationDto
            {
                Id = a.Id, Label = a.Label, Color = a.Color ?? "#FFD700",
                X = a.X, Y = a.Y, Width = a.Width, Height = a.Height, ZIndex = a.ZIndex
            }).ToList(),
            Zones = bp.Zones.Select(z => new ZoneDto
            {
                Id = z.Id, Name = z.Name, Color = z.Color ?? "#3B82F680",
                X = z.X, Y = z.Y, Width = z.Width, Height = z.Height, ZIndex = z.ZIndex
            }).ToList()
        };
    }

    public async Task SaveBlueprintLayoutAsync(Guid id, BlueprintLayoutDto layout, CancellationToken ct = default)
    {
        foreach (var placement in layout.AssetPlacements)
        {
            var asset = await db.Assets.FindAsync([placement.AssetId], ct);
            if (asset == null) continue;
            asset.BlueprintId = id;
            asset.BlueprintX = placement.X;
            asset.BlueprintY = placement.Y;
            asset.BlueprintWidth = placement.Width;
            asset.BlueprintHeight = placement.Height;
            asset.BlueprintZIndex = placement.ZIndex;
        }
        await db.SaveChangesAsync(ct);
    }

    public async Task<BlueprintAnnotation> AddAnnotationAsync(Guid blueprintId, CreateAnnotationDto dto, CancellationToken ct = default)
    {
        var annotation = new BlueprintAnnotation
        {
            BlueprintId = blueprintId,
            Label = dto.Label,
            Color = dto.Color,
            X = dto.X,
            Y = dto.Y
        };
        db.BlueprintAnnotations.Add(annotation);
        await db.SaveChangesAsync(ct);
        return annotation;
    }

    public async Task<BlueprintZone> AddZoneAsync(Guid blueprintId, CreateZoneDto dto, CancellationToken ct = default)
    {
        var zone = new BlueprintZone
        {
            BlueprintId = blueprintId,
            Name = dto.Name,
            Color = dto.Color,
            X = dto.X,
            Y = dto.Y,
            Width = dto.Width,
            Height = dto.Height
        };
        db.BlueprintZones.Add(zone);
        await db.SaveChangesAsync(ct);
        return zone;
    }

    public async Task DeleteAnnotationAsync(Guid annotationId, CancellationToken ct = default)
    {
        var a = await db.BlueprintAnnotations.FindAsync([annotationId], ct);
        if (a != null) { db.BlueprintAnnotations.Remove(a); await db.SaveChangesAsync(ct); }
    }

    public async Task DeleteZoneAsync(Guid zoneId, CancellationToken ct = default)
    {
        var z = await db.BlueprintZones.FindAsync([zoneId], ct);
        if (z != null) { db.BlueprintZones.Remove(z); await db.SaveChangesAsync(ct); }
    }
}
