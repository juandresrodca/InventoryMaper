using System.Threading.Channels;
using InventoryMapper.Core.Interfaces;

namespace InventoryMapper.API.Services;

public interface ISweepQueue
{
    void QueueSweep();
}

/// <summary>
/// Single-slot signal channel: a queued sweep request coalesces with any sweep already pending,
/// since running the same sweep twice back-to-back has no added value.
/// </summary>
public class SweepQueue : ISweepQueue
{
    private readonly Channel<bool> _channel = Channel.CreateBounded<bool>(
        new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropWrite });

    public void QueueSweep() => _channel.Writer.TryWrite(true);

    public IAsyncEnumerable<bool> ReadAllAsync(CancellationToken ct) => _channel.Reader.ReadAllAsync(ct);
}

/// <summary>
/// Runs ping sweeps requested via the API outside the HTTP request lifetime: its own service scope
/// (fresh DbContext) and the host's lifetime cancellation token, not the request's.
/// </summary>
public class SweepBackgroundService(
    SweepQueue queue, IServiceScopeFactory scopeFactory, ILogger<SweepBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var _ in queue.ReadAllAsync(stoppingToken))
        {
            using var scope = scopeFactory.CreateScope();
            var monitoring = scope.ServiceProvider.GetRequiredService<IMonitoringService>();
            try
            {
                await monitoring.RunPingSweepAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Queued ping sweep failed");
            }
        }
    }
}
