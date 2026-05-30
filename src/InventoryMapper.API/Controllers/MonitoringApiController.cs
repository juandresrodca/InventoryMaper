using InventoryMapper.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace InventoryMapper.API.Controllers;

[ApiController]
[Route("api/v1/monitoring")]
public class MonitoringApiController(IMonitoringService monitoring) : ControllerBase
{
    [HttpPost("heartbeat")]
    public async Task<IActionResult> Heartbeat([FromBody] AgentHeartbeatDto dto, CancellationToken ct)
    {
        await monitoring.ProcessAgentHeartbeatAsync(dto, ct);
        return Ok(new { accepted = true, serverTime = DateTime.UtcNow });
    }

    [HttpGet("alerts")]
    public async Task<IActionResult> GetAlerts(CancellationToken ct)
        => Ok(await monitoring.GetActiveAlertsAsync(ct));

    [HttpPost("alerts/{id:guid}/resolve")]
    public async Task<IActionResult> ResolveAlert(Guid id, CancellationToken ct)
    {
        await monitoring.ResolveAlertAsync(id, User.Identity?.Name ?? "API", ct);
        return NoContent();
    }

    [HttpGet("history/{assetId:guid}")]
    public async Task<IActionResult> GetHistory(Guid assetId, [FromQuery] int count = 50, CancellationToken ct = default)
        => Ok(await monitoring.GetRecentHistoryAsync(assetId, count, ct));

    [HttpPost("sweep")]
    public async Task<IActionResult> TriggerSweep(CancellationToken ct)
    {
        _ = Task.Run(() => monitoring.RunPingSweepAsync(ct), ct);
        return Accepted(new { message = "Ping sweep started" });
    }
}
