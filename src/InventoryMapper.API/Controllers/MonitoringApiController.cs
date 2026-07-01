using InventoryMapper.API.Services;
using InventoryMapper.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryMapper.API.Controllers;

[ApiController]
[Route("api/v1/monitoring")]
<<<<<<< HEAD
public class MonitoringApiController(IMonitoringService monitoring, ISweepQueue sweepQueue) : ControllerBase
=======
public class MonitoringApiController(
    IMonitoringService monitoring,
    IServiceScopeFactory scopeFactory,
    IHostApplicationLifetime lifetime,
    ILogger<MonitoringApiController> logger) : ControllerBase
>>>>>>> ef0db94cbb07f234e070e8d6d6707728eb3951b6
{
    // Agents are not Identity users — this endpoint authenticates itself via the AgentKey/HandshakeToken
    // pair in the body (see MonitoringService.ProcessAgentHeartbeatAsync) rather than [Authorize].
    [HttpPost("heartbeat"), AllowAnonymous]
    public async Task<IActionResult> Heartbeat([FromBody] AgentHeartbeatDto dto, CancellationToken ct)
    {
        var result = await monitoring.ProcessAgentHeartbeatAsync(dto, ct);
        if (result.Outcome == HeartbeatOutcome.Rejected)
            return Unauthorized(new { accepted = false, reason = result.RejectionReason });

        return Ok(new { accepted = true, serverTime = DateTime.UtcNow, agentSecret = result.NewAgentSecret });
    }

    [HttpPost("enrollment-tokens"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateEnrollmentToken(CancellationToken ct)
    {
        var token = await monitoring.GenerateEnrollmentTokenAsync(User.Identity?.Name ?? "API", ct);
        return Ok(new { token.Token, token.ExpiresAt });
    }

    [HttpGet("alerts"), Authorize]
    public async Task<IActionResult> GetAlerts(CancellationToken ct)
        => Ok(await monitoring.GetActiveAlertsAsync(ct));

    [HttpPost("alerts/{id:guid}/resolve"), Authorize]
    public async Task<IActionResult> ResolveAlert(Guid id, CancellationToken ct)
    {
        await monitoring.ResolveAlertAsync(id, User.Identity?.Name ?? "API", ct);
        return NoContent();
    }

    [HttpGet("history/{assetId:guid}"), Authorize]
    public async Task<IActionResult> GetHistory(Guid assetId, [FromQuery] int count = 50, CancellationToken ct = default)
        => Ok(await monitoring.GetRecentHistoryAsync(assetId, count, ct));

<<<<<<< HEAD
    [HttpPost("sweep"), Authorize]
    public IActionResult TriggerSweep()
    {
        sweepQueue.QueueSweep();
        return Accepted(new { message = "Ping sweep queued" });
=======
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
>>>>>>> ef0db94cbb07f234e070e8d6d6707728eb3951b6
    }
}
