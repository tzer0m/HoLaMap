using Microsoft.Extensions.Options;

namespace HoLaMap.UniFi
{
    /// <summary>
    /// Background worker that runs the UniFi sync on an interval.
    /// </summary>
    /// <param name="scopeFactory">Factory for creating a scope per sync.</param>
    /// <param name="options">The UniFi settings.</param>
    /// <param name="logger">The logger.</param>
    public class UniFiSyncWorker(IServiceScopeFactory scopeFactory, IOptions<UniFiOptions> options, ILogger<UniFiSyncWorker> logger) : BackgroundService
    {
        /// <summary>
        /// Runs a sync immediately and then on every interval tick.
        /// </summary>
        /// <param name="stoppingToken">Token signalled when the app is stopping.</param>
        /// <returns>A task that completes when the worker stops.</returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using PeriodicTimer timer = new(TimeSpan.FromMinutes(options.Value.SyncIntervalMinutes));
            do
            {
                try
                {
                    using IServiceScope scope = scopeFactory.CreateScope();
                    UniFiSyncService sync = scope.ServiceProvider.GetRequiredService<UniFiSyncService>();
                    await sync.SyncAsync(stoppingToken);
                    logger.LogInformation("UniFi sync completed");
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    logger.LogError(exception, "UniFi sync failed");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
    }
}