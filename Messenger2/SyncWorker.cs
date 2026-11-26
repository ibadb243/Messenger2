using Microsoft.Extensions.Options;

namespace Messenger2;

public class SyncWorker(
    IServiceScopeFactory scopes,
    IOptions<SyncOptions> options,
    ILogger<SyncWorker> logger) 
        : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(options.Value.IntervalSeconds);
        using var timer = new PeriodicTimer(interval);
        logger.LogInformation("Sync worker started, interval {Interval}", interval);

        try
        {
            do
            {
                try
                {
                    await using var scope = scopes.CreateAsyncScope();
                    var sync = scope.ServiceProvider.GetRequiredService<Synchronizer>();
                    await sync.RunOnceAsync(stoppingToken);
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    logger.LogError(ex, "Sync failed, retrying in {Interval}", interval);
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Sync worker stopped");
        }
    }
}
