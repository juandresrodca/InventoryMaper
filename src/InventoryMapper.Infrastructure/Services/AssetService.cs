using InventoryMapper.Core.DTOs;
using InventoryMapper.Core.Entities;
using InventoryMapper.Core.Enums;
using InventoryMapper.Core.Interfaces;
using InventoryMapper.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InventoryMapper.Infrastructure.Services;

public class AssetService(ApplicationDbContext db, ILogger<AssetService> logger) : IAssetService
{
    public async Task<PagedResult<Asset>> GetAssetsAsync(AssetFilterDto filter, CancellationToken ct = default)
    {
        var query = db.Assets
            .Include(a => a.Location)
            .Include(a => a.Tags)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var s = filter.Search.ToLower();
            query = query.Where(a =>
                a.Hostname.ToLower().Contains(s) ||
                (a.IpAddress != null && a.IpAddress.Contains(s)) ||
                (a.SerialNumber != null && a.SerialNumber.ToLower().Contains(s)) ||
                (a.AssignedUser != null && a.AssignedUser.ToLower().Contains(s)) ||
                (a.Department != null && a.Department.ToLower().Contains(s))
            );
        }

        if (filter.AssetType.HasValue)
            query = query.Where(a => a.AssetType == filter.AssetType.Value);

        if (filter.Status.HasValue)
            query = query.Where(a => a.Status == filter.Status.Value);

        if (filter.OnlineState.HasValue)
            query = query.Where(a => a.OnlineState == filter.OnlineState.Value);

        if (filter.LocationId.HasValue)
            query = query.Where(a => a.LocationId == filter.LocationId.Value);

        if (filter.BlueprintId.HasValue)
            query = query.Where(a => a.BlueprintId == filter.BlueprintId.Value);

        if (!string.IsNullOrWhiteSpace(filter.Department))
            query = query.Where(a => a.Department == filter.Department);

        if (filter.IsMonitored.HasValue)
            query = query.Where(a => a.IsMonitored == filter.IsMonitored.Value);

        query = filter.SortBy switch
        {
            "IpAddress" => filter.SortDescending ? query.OrderByDescending(a => a.IpAddress) : query.OrderBy(a => a.IpAddress),
            "Status" => filter.SortDescending ? query.OrderByDescending(a => a.Status) : query.OrderBy(a => a.Status),
            "AssetType" => filter.SortDescending ? query.OrderByDescending(a => a.AssetType) : query.OrderBy(a => a.AssetType),
            "LastCheckIn" => filter.SortDescending ? query.OrderByDescending(a => a.LastCheckIn) : query.OrderBy(a => a.LastCheckIn),
            _ => filter.SortDescending ? query.OrderByDescending(a => a.Hostname) : query.OrderBy(a => a.Hostname)
        };

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(ct);

        return new PagedResult<Asset>
        {
            Items = items,
            TotalCount = total,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<Asset?> GetAssetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.Assets
            .Include(a => a.Location)
            .Include(a => a.Tags)
            .Include(a => a.Blueprint)
            .Include(a => a.Agent)
            .Include(a => a.Alerts.Where(al => !al.IsResolved))
            .FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<Asset> CreateAssetAsync(CreateAssetDto dto, CancellationToken ct = default)
    {
        var asset = new Asset
        {
            Hostname = dto.Hostname,
            IpAddress = dto.IpAddress,
            MacAddress = dto.MacAddress,
            SerialNumber = dto.SerialNumber,
            AssetType = dto.AssetType,
            Manufacturer = dto.Manufacturer,
            Model = dto.Model,
            OperatingSystem = dto.OperatingSystem,
            OsVersion = dto.OsVersion,
            OrganizationalUnit = dto.OrganizationalUnit,
            AssignedUser = dto.AssignedUser,
            Department = dto.Department,
            Status = dto.Status,
            Notes = dto.Notes,
            LocationId = dto.LocationId,
            MonitoringMethod = dto.MonitoringMethod,
            IsMonitored = dto.IsMonitored,
            Tags = dto.Tags.Select(t => new AssetTag { Key = t.Key, Value = t.Value, Color = t.Color }).ToList()
        };

        db.Assets.Add(asset);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Created asset {Hostname} ({Id})", asset.Hostname, asset.Id);
        return asset;
    }

    public async Task<Asset> UpdateAssetAsync(Guid id, UpdateAssetDto dto, CancellationToken ct = default)
    {
        var asset = await db.Assets.Include(a => a.Tags).FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new KeyNotFoundException($"Asset {id} not found");

        asset.Hostname = dto.Hostname;
        asset.IpAddress = dto.IpAddress;
        asset.MacAddress = dto.MacAddress;
        asset.SerialNumber = dto.SerialNumber;
        asset.AssetType = dto.AssetType;
        asset.Manufacturer = dto.Manufacturer;
        asset.Model = dto.Model;
        asset.OperatingSystem = dto.OperatingSystem;
        asset.OsVersion = dto.OsVersion;
        asset.OrganizationalUnit = dto.OrganizationalUnit;
        asset.AssignedUser = dto.AssignedUser;
        asset.Department = dto.Department;
        asset.Status = dto.Status;
        asset.Notes = dto.Notes;
        asset.LocationId = dto.LocationId;
        asset.MonitoringMethod = dto.MonitoringMethod;
        asset.IsMonitored = dto.IsMonitored;
        asset.UpdatedAt = DateTime.UtcNow;

        db.AssetTags.RemoveRange(asset.Tags);
        asset.Tags = dto.Tags.Select(t => new AssetTag { AssetId = id, Key = t.Key, Value = t.Value, Color = t.Color }).ToList();

        await db.SaveChangesAsync(ct);
        return asset;
    }

    public async Task DeleteAssetAsync(Guid id, CancellationToken ct = default)
    {
        var asset = await db.Assets.FindAsync([id], ct)
            ?? throw new KeyNotFoundException($"Asset {id} not found");
        asset.IsDeleted = true;
        await db.SaveChangesAsync(ct);
    }

    public async Task<DashboardStatsDto> GetDashboardStatsAsync(CancellationToken ct = default)
    {
        var assets = await db.Assets.ToListAsync(ct);
        var alerts = await db.Alerts.Where(a => !a.IsResolved).ToListAsync(ct);
        var recentCheckIns = await db.Assets
            .Where(a => a.LastCheckIn != null)
            .OrderByDescending(a => a.LastCheckIn)
            .Take(10)
            .ToListAsync(ct);
        var recentAlerts = await db.Alerts
            .Include(a => a.Asset)
            .OrderByDescending(a => a.CreatedAt)
            .Take(5)
            .ToListAsync(ct);

        var virtualTypes = new[] { AssetType.VirtualMachine, AssetType.Container, AssetType.Hypervisor, AssetType.VirtualFarm, AssetType.CloudInstance };
        var networkTypes = new[] { AssetType.NetworkSwitch, AssetType.Router, AssetType.Firewall, AssetType.AccessPoint };

        var byType = assets
            .GroupBy(a => a.AssetType)
            .Select(g => new AssetTypeCountDto(g.Key.ToString(), g.Count(), GetTypeColor(g.Key)))
            .ToList();

        var history = await db.MonitoringRecords
            .Where(m => m.CheckedAt >= DateTime.UtcNow.AddHours(-24))
            .GroupBy(m => new { Hour = m.CheckedAt.Date.AddHours(m.CheckedAt.Hour) })
            .Select(g => new { Hour = g.Key.Hour, Online = g.Count(x => x.State == OnlineState.Online), Offline = g.Count(x => x.State == OnlineState.Offline) })
            .ToListAsync(ct);

        return new DashboardStatsDto
        {
            TotalAssets = assets.Count,
            OnlineAssets = assets.Count(a => a.OnlineState == OnlineState.Online),
            OfflineAssets = assets.Count(a => a.OnlineState == OnlineState.Offline),
            UnknownAssets = assets.Count(a => a.OnlineState == OnlineState.Unknown),
            PhysicalAssets = assets.Count(a => !virtualTypes.Contains(a.AssetType) && !networkTypes.Contains(a.AssetType)),
            VirtualAssets = assets.Count(a => virtualTypes.Contains(a.AssetType)),
            NetworkDevices = assets.Count(a => networkTypes.Contains(a.AssetType)),
            MissingAssets = assets.Count(a => a.Status == AssetStatus.Missing),
            UnassignedAssets = assets.Count(a => string.IsNullOrEmpty(a.AssignedUser) && a.AssetType == AssetType.PhysicalDevice),
            ActiveAlerts = alerts.Count,
            CriticalAlerts = alerts.Count(a => a.Severity == AlertSeverity.Critical),
            UnplacedAssets = assets.Count(a => a.BlueprintId == null),
            AssetsByType = byType,
            RecentCheckIns = recentCheckIns.Select(a => new RecentCheckInDto(a.Hostname, a.LastCheckIn!.Value, a.OnlineState.ToString(), a.AssetType.ToString())).ToList(),
            RecentAlerts = recentAlerts.Select(a => new AlertSummaryDto(a.Title, a.Severity.ToString(), a.CreatedAt, a.Asset?.Hostname)).ToList(),
            OnlineHistory = history.Select(h => new OnlineHistoryPointDto(DateTime.UtcNow.Date.AddHours(h.Hour), h.Online, h.Offline)).ToList()
        };
    }

    public async Task UpdateOnlineStateAsync(Guid id, OnlineState state, double? responseMs = null, CancellationToken ct = default)
    {
        var asset = await db.Assets.FindAsync([id], ct);
        if (asset == null) return;

        var previous = asset.OnlineState;
        asset.OnlineState = state;
        asset.LastPingAt = DateTime.UtcNow;
        if (state == OnlineState.Online || state == OnlineState.Offline)
            asset.LastCheckIn = DateTime.UtcNow;

        db.MonitoringRecords.Add(new MonitoringRecord
        {
            AssetId = id,
            State = state,
            Method = asset.MonitoringMethod,
            CheckedAt = DateTime.UtcNow,
            ResponseTimeMs = responseMs,
            Success = state == OnlineState.Online
        });

        if (previous == OnlineState.Online && state == OnlineState.Offline)
        {
            db.Alerts.Add(new AlertNotification
            {
                AlertType = AlertType.DeviceOffline,
                Severity = AlertSeverity.Warning,
                Title = "Device Offline",
                Message = $"{asset.Hostname} has gone offline",
                AssetId = id
            });
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task<IEnumerable<Asset>> GetUnplacedAssetsAsync(CancellationToken ct = default)
        => await db.Assets.Where(a => a.BlueprintId == null).OrderBy(a => a.Hostname).ToListAsync(ct);

    public async Task PlaceAssetOnBlueprintAsync(Guid assetId, Guid blueprintId, double x, double y, CancellationToken ct = default)
    {
        var asset = await db.Assets.FindAsync([assetId], ct)
            ?? throw new KeyNotFoundException($"Asset {assetId} not found");
        asset.BlueprintId = blueprintId;
        asset.BlueprintX = x;
        asset.BlueprintY = y;
        asset.BlueprintWidth ??= 48;
        asset.BlueprintHeight ??= 48;
        await db.SaveChangesAsync(ct);
    }

    public async Task RemoveAssetFromBlueprintAsync(Guid assetId, CancellationToken ct = default)
    {
        var asset = await db.Assets.FindAsync([assetId], ct)
            ?? throw new KeyNotFoundException($"Asset {assetId} not found");
        asset.BlueprintId = null;
        asset.BlueprintX = null;
        asset.BlueprintY = null;
        await db.SaveChangesAsync(ct);
    }

    private static string GetTypeColor(AssetType type) => type switch
    {
        AssetType.Server => "#EF4444",
        AssetType.VirtualMachine => "#8B5CF6",
        AssetType.NetworkSwitch => "#F59E0B",
        AssetType.Firewall => "#F97316",
        AssetType.Router => "#EAB308",
        AssetType.PhysicalDevice => "#3B82F6",
        AssetType.Container => "#06B6D4",
        AssetType.CloudInstance => "#10B981",
        _ => "#6B7280"
    };
}
