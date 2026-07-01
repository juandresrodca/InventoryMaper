using InventoryMapper.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace InventoryMapper.API.Controllers;

[ApiController]
[Route("api/v1/monitoring")]
public class MonitoringApiController(
    IMonitoringService monitoring,
    IServiceScopeFactory scopeFactory,
    IHostApplicationLifetime lifetime,
    ILogger<MonitoringApiController> logger) : ControllerBase
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
    public IActionResult TriggerSweep()
    {
        // Run on a fresh scope: the request-scoped service (and its DbContext) is
        // disposed as soon as this response returns, and the request token is cancelled.
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var sweepService = scope.ServiceProvider.GetRequiredService<IMonitoringService>();
                await sweepService.RunPingSweepAsync(lifetime.ApplicationStopping);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Background ping sweep failed");
            }
        });
        return Accepted(new { message = "Ping sweep started" });
    }
}
