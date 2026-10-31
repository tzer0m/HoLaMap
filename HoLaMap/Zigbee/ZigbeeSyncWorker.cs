using Microsoft.Extensions.Options;

namespace HoLaMap.Zigbee
{
    /// <summary>
    /// Background worker that runs the Zigbee sync on an interval, if Home Assistant has been set up.
    /// </summary>
    /// <param name="scopeFactory">Factory for creating a scope per sync.</param>
    /// <param name="options">The Home Assistant settings.</param>
    /// <param name="logger">The logger.</param>
    public class ZigbeeSyncWorker(IServiceScopeFactory scopeFactory, IOptions<HomeAssistantOptions> options, ILogger<ZigbeeSyncWorker> logger) : BackgroundService
    {
        /// <summary>
        /// Runs a sync immediately and then on every interval tick.
        /// </summary>
        /// <param name="stoppingToken">Token signalled when the app is stopping.</param>
        /// <returns>A task that completes when the worker stops.</returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (string.IsNullOrEmpty(options.Value.Url) || string.IsNullOrEmpty(options.Value.Token))
            {
                logger.LogInformation("Zigbee sync is off because Home Assistant isn't set up");
                return;
            }
            using PeriodicTimer timer = new(TimeSpan.FromMinutes(options.Value.SyncIntervalMinutes));
            do
            {
                try
                {
                    using IServiceScope scope = scopeFactory.CreateScope();
                    ZigbeeSyncService sync = scope.ServiceProvider.GetRequiredService<ZigbeeSyncService>();
                    await sync.SyncAsync(stoppingToken);
                    logger.LogInformation("Zigbee sync completed");
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    logger.LogError(exception, "Zigbee sync failed");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
    }
}