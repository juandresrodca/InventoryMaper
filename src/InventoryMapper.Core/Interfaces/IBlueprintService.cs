using InventoryMapper.Core.DTOs;
using InventoryMapper.Core.Entities;

namespace InventoryMapper.Core.Interfaces;

public interface IBlueprintService
{
    Task<IEnumerable<Blueprint>> GetAllBlueprintsAsync(CancellationToken ct = default);
    Task<Blueprint?> GetBlueprintByIdAsync(Guid id, CancellationToken ct = default);
    Task<Blueprint> CreateBlueprintAsync(CreateBlueprintDto dto, Stream fileStream, string fileName, string mimeType, CancellationToken ct = default);
    Task DeleteBlueprintAsync(Guid id, CancellationToken ct = default);
    Task<BlueprintLayoutDto> GetBlueprintLayoutAsync(Guid id, CancellationToken ct = default);
    Task SaveBlueprintLayoutAsync(Guid id, BlueprintLayoutDto layout, CancellationToken ct = default);
    Task<BlueprintAnnotation> AddAnnotationAsync(Guid blueprintId, CreateAnnotationDto dto, CancellationToken ct = default);
    Task<BlueprintZone> AddZoneAsync(Guid blueprintId, CreateZoneDto dto, CancellationToken ct = default);
    Task DeleteAnnotationAsync(Guid annotationId, CancellationToken ct = default);
    Task DeleteZoneAsync(Guid zoneId, CancellationToken ct = default);
}
