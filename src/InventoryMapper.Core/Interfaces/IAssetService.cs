using InventoryMapper.Core.DTOs;
using InventoryMapper.Core.Entities;
using InventoryMapper.Core.Enums;

namespace InventoryMapper.Core.Interfaces;

public interface IAssetService
{
    Task<PagedResult<Asset>> GetAssetsAsync(AssetFilterDto filter, CancellationToken ct = default);
    Task<Asset?> GetAssetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Asset> CreateAssetAsync(CreateAssetDto dto, CancellationToken ct = default);
    Task<Asset> UpdateAssetAsync(Guid id, UpdateAssetDto dto, CancellationToken ct = default);
    Task DeleteAssetAsync(Guid id, CancellationToken ct = default);
    Task<DashboardStatsDto> GetDashboardStatsAsync(CancellationToken ct = default);
    Task UpdateOnlineStateAsync(Guid id, OnlineState state, double? responseMs = null, CancellationToken ct = default);
    Task<IEnumerable<Asset>> GetUnplacedAssetsAsync(CancellationToken ct = default);
    Task PlaceAssetOnBlueprintAsync(Guid assetId, Guid blueprintId, double x, double y, CancellationToken ct = default);
    Task RemoveAssetFromBlueprintAsync(Guid assetId, CancellationToken ct = default);
}
