using InventoryMapper.Core.Interfaces;
using InventoryMapper.Web.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace InventoryMapper.Web.Workers;

public class MonitoringWorker(
    IServiceScopeFactory scopeFactory,
    IHubContext<MonitoringHub> hub,
    IConfiguration config,
    ILogger<MonitoringWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalMinutes = config.GetValue("Monitoring:PingSweepIntervalMinutes", 5);
        logger.LogInformation("Monitoring worker started. Ping sweep every {Interval} minutes.", intervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunSweepAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Monitoring sweep failed");
            }

            await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), stoppingToken);
        }
    }

    private async Task RunSweepAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var monitoring = scope.ServiceProvider.GetRequiredService<IMonitoringService>();
        var assetService = scope.ServiceProvider.GetRequiredService<IAssetService>();

        await monitoring.RunPingSweepAsync(ct);

        var stats = await assetService.GetDashboardStatsAsync(ct);
        var alerts = await monitoring.GetActiveAlertsAsync(ct);

        await hub.Clients.Group("dashboard").SendAsync("StatsUpdated", new
        {
            stats.TotalAssets,
            stats.OnlineAssets,
            stats.OfflineAssets,
            stats.ActiveAlerts,
            stats.CriticalAlerts,
            Timestamp = DateTime.UtcNow
        }, ct);

        if (alerts.Any())
        {
            await hub.Clients.Group("dashboard").SendAsync("AlertsUpdated", new
            {
                Count = alerts.Count(),
                Critical = alerts.Count(a => a.Severity == Core.Enums.AlertSeverity.Critical)
            }, ct);
        }
    }
}
