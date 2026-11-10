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
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(options.Value.IntervalSeconds));

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
                    logger.LogError(ex, "Sync error");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            
        }
    }
}
