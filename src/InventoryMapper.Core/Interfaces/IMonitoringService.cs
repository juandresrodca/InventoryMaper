using InventoryMapper.Core.Entities;

namespace InventoryMapper.Core.Interfaces;

public interface IMonitoringService
{
    Task RunPingSweepAsync(CancellationToken ct = default);
    Task<bool> PingAssetAsync(Guid assetId, CancellationToken ct = default);
    Task ProcessAgentHeartbeatAsync(AgentHeartbeatDto dto, CancellationToken ct = default);
    Task<IEnumerable<MonitoringRecord>> GetRecentHistoryAsync(Guid assetId, int count = 50, CancellationToken ct = default);
    Task<IEnumerable<AlertNotification>> GetActiveAlertsAsync(CancellationToken ct = default);
    Task ResolveAlertAsync(Guid alertId, string resolvedBy, CancellationToken ct = default);
}

public record AgentHeartbeatDto(
    string AgentKey,
    string Hostname,
    string IpAddress,
    string MacAddress,
    string OrganizationalUnit,
    string AgentVersion,
    string OperatingSystem,
    string OsVersion,
    string HandshakeToken
);
