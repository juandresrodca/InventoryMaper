using InventoryMapper.Core.Entities;

namespace InventoryMapper.Core.Interfaces;

public interface IMonitoringService
{
    Task RunPingSweepAsync(CancellationToken ct = default);
    Task<bool> PingAssetAsync(Guid assetId, CancellationToken ct = default);
    Task<HeartbeatResult> ProcessAgentHeartbeatAsync(AgentHeartbeatDto dto, CancellationToken ct = default);
    Task<EnrollmentToken> GenerateEnrollmentTokenAsync(string createdBy, CancellationToken ct = default);
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

public enum HeartbeatOutcome { Accepted, Rejected }

/// <summary>
/// <see cref="NewAgentSecret"/> is populated only on the enrollment heartbeat (first contact using a
/// one-time enrollment token) — the agent must persist it and send it as HandshakeToken from then on.
/// </summary>
public record HeartbeatResult(HeartbeatOutcome Outcome, string? RejectionReason = null, string? NewAgentSecret = null)
{
    public static HeartbeatResult Accepted(string? newAgentSecret = null) => new(HeartbeatOutcome.Accepted, null, newAgentSecret);
    public static HeartbeatResult Rejected(string reason) => new(HeartbeatOutcome.Rejected, reason);
}
