using Microsoft.Extensions.Options;

namespace HoLaMap.Semaphore
{
    /// <summary>
    /// Background worker that reads Semaphore's schedules on an interval, if an address and token are set.
    /// </summary>
    /// <param name="scopeFactory">Factory for creating a scope per sync.</param>
    /// <param name="options">The Semaphore settings.</param>
    /// <param name="logger">The logger.</param>
    public class SemaphoreSyncWorker(IServiceScopeFactory scopeFactory, IOptions<SemaphoreOptions> options, ILogger<SemaphoreSyncWorker> logger) : BackgroundService
    {
        /// <summary>
        /// Runs a sync immediately and then on every interval tick.
        /// </summary>
        /// <param name="stoppingToken">Token signalled when the app is stopping.</param>
        /// <returns>A task that completes when the worker stops.</returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (string.IsNullOrEmpty(options.Value.BaseUrl) || string.IsNullOrEmpty(options.Value.ApiToken))
            {
                logger.LogInformation("Semaphore sync is off because the address or API token isn't set");
                return;
            }
            using PeriodicTimer timer = new(TimeSpan.FromMinutes(options.Value.SyncIntervalMinutes));
            do
            {
                try
                {
                    using IServiceScope scope = scopeFactory.CreateScope();
                    SemaphoreSyncService sync = scope.ServiceProvider.GetRequiredService<SemaphoreSyncService>();
                    int count = await sync.SyncAsync(stoppingToken);
                    logger.LogInformation("Semaphore sync completed with {Count} schedules", count);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    logger.LogError(exception, "Semaphore sync failed");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
    }
}