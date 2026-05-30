using System.Net.NetworkInformation;
using InventoryMapper.Core.Entities;
using InventoryMapper.Core.Enums;
using InventoryMapper.Core.Interfaces;
using InventoryMapper.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InventoryMapper.Infrastructure.Services;

public class MonitoringService(ApplicationDbContext db, ILogger<MonitoringService> logger) : IMonitoringService
{
    public async Task RunPingSweepAsync(CancellationToken ct = default)
    {
        var assets = await db.Assets
            .Where(a => a.IsMonitored && !string.IsNullOrEmpty(a.IpAddress))
            .ToListAsync(ct);

        // Ping all assets concurrently (pure network I/O — no DbContext involvement)
        var pingResults = await Task.WhenAll(assets.Select(a => PingOnlyAsync(a, ct)));

        // Write results sequentially — DbContext is not thread-safe
        for (int i = 0; i < assets.Count; i++)
        {
            var (state, responseMs) = pingResults[i];
            await UpdateStateAsync(assets[i], state, MonitoringMethod.Ping, responseMs, ct);
        }

        logger.LogInformation("Ping sweep complete. Checked {Count} assets.", assets.Count);
    }

    public async Task<bool> PingAssetAsync(Guid assetId, CancellationToken ct = default)
    {
        var asset = await db.Assets.FindAsync([assetId], ct);
        if (asset == null) return false;
        var (state, responseMs) = await PingOnlyAsync(asset, ct);
        await UpdateStateAsync(asset, state, MonitoringMethod.Ping, responseMs, ct);
        return state == OnlineState.Online;
    }

    private async Task<(OnlineState State, double? ResponseMs)> PingOnlyAsync(Asset asset, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(asset.IpAddress)) return (OnlineState.Unknown, null);
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(asset.IpAddress, 2000);
            var success = reply.Status == IPStatus.Success;
            return (success ? OnlineState.Online : OnlineState.Offline, success ? (double?)reply.RoundtripTime : null);
        }
        catch (Exception ex)
        {
            logger.LogWarning("Ping failed for {Hostname} ({IP}): {Msg}", asset.Hostname, asset.IpAddress, ex.Message);
            return (OnlineState.Unreachable, null);
        }
    }

    private async Task UpdateStateAsync(Asset asset, OnlineState state, MonitoringMethod method, double? responseMs, CancellationToken ct)
    {
        var previous = asset.OnlineState;
        asset.OnlineState = state;
        asset.LastPingAt = DateTime.UtcNow;
        if (state is OnlineState.Online or OnlineState.Offline)
            asset.LastCheckIn = DateTime.UtcNow;

        db.MonitoringRecords.Add(new MonitoringRecord
        {
            AssetId = asset.Id,
            State = state,
            Method = method,
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
                Message = $"{asset.Hostname} ({asset.IpAddress}) has gone offline",
                AssetId = asset.Id
            });
        }
        else if (previous == OnlineState.Offline && state == OnlineState.Online)
        {
            var activeAlert = await db.Alerts
                .FirstOrDefaultAsync(a => a.AssetId == asset.Id && a.AlertType == AlertType.DeviceOffline && !a.IsResolved, ct);
            if (activeAlert != null)
            {
                activeAlert.IsResolved = true;
                activeAlert.ResolvedAt = DateTime.UtcNow;
                activeAlert.ResolvedBy = "System";
            }
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task ProcessAgentHeartbeatAsync(AgentHeartbeatDto dto, CancellationToken ct = default)
    {
        var registration = await db.AgentRegistrations
            .FirstOrDefaultAsync(r => r.AgentKey == dto.AgentKey, ct);

        if (registration == null)
        {
            registration = new AgentRegistration
            {
                AgentKey = dto.AgentKey,
                Hostname = dto.Hostname,
                IpAddress = dto.IpAddress,
                MacAddress = dto.MacAddress,
                OrganizationalUnit = dto.OrganizationalUnit,
                AgentVersion = dto.AgentVersion,
                OperatingSystem = dto.OperatingSystem,
                OsVersion = dto.OsVersion,
                LastHeartbeat = DateTime.UtcNow
            };
            db.AgentRegistrations.Add(registration);
        }
        else
        {
            registration.LastHeartbeat = DateTime.UtcNow;
            registration.IpAddress = dto.IpAddress;
            registration.OrganizationalUnit = dto.OrganizationalUnit;
            registration.AgentVersion = dto.AgentVersion;
        }

        // Validate handshake token
        registration.IsHandshakeValid = !string.IsNullOrEmpty(dto.HandshakeToken) &&
            registration.HandshakeToken == dto.HandshakeToken &&
            registration.HandshakeExpiry > DateTime.UtcNow;

        // Find or update linked asset
        var asset = await db.Assets
            .FirstOrDefaultAsync(a => a.Hostname == dto.Hostname || a.MacAddress == dto.MacAddress, ct);

        if (asset != null)
        {
            asset.OnlineState = OnlineState.Online;
            asset.LastCheckIn = DateTime.UtcNow;
            asset.IpAddress = dto.IpAddress;
            asset.MacAddress = dto.MacAddress;
            asset.OperatingSystem = dto.OperatingSystem;
            asset.OsVersion = dto.OsVersion;
            asset.OrganizationalUnit = dto.OrganizationalUnit;
            asset.AgentId = registration.Id;

            db.MonitoringRecords.Add(new MonitoringRecord
            {
                AssetId = asset.Id,
                State = OnlineState.Online,
                Method = MonitoringMethod.AgentHeartbeat,
                CheckedAt = DateTime.UtcNow,
                Success = true,
                Details = $"Agent v{dto.AgentVersion}"
            });

            if (!registration.IsHandshakeValid && asset.OrganizationalUnit != dto.OrganizationalUnit)
            {
                db.Alerts.Add(new AlertNotification
                {
                    AlertType = AlertType.OUMismatch,
                    Severity = AlertSeverity.Warning,
                    Title = "OU Mismatch Detected",
                    Message = $"{dto.Hostname}: expected OU '{asset.OrganizationalUnit}' but got '{dto.OrganizationalUnit}'",
                    AssetId = asset.Id
                });
            }
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task<IEnumerable<MonitoringRecord>> GetRecentHistoryAsync(Guid assetId, int count = 50, CancellationToken ct = default)
        => await db.MonitoringRecords
            .Where(m => m.AssetId == assetId)
            .OrderByDescending(m => m.CheckedAt)
            .Take(count)
            .ToListAsync(ct);

    public async Task<IEnumerable<AlertNotification>> GetActiveAlertsAsync(CancellationToken ct = default)
        => await db.Alerts
            .Include(a => a.Asset)
            .Where(a => !a.IsResolved)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);

    public async Task ResolveAlertAsync(Guid alertId, string resolvedBy, CancellationToken ct = default)
    {
        var alert = await db.Alerts.FindAsync([alertId], ct);
        if (alert != null)
        {
            alert.IsResolved = true;
            alert.ResolvedAt = DateTime.UtcNow;
            alert.ResolvedBy = resolvedBy;
            await db.SaveChangesAsync(ct);
        }
    }
}
