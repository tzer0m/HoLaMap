using Microsoft.Extensions.Options;

namespace HoLaMap.Cloudflare
{
    /// <summary>
    /// Background worker that reads the Cloudflare tunnels on an interval, if an account and token are set.
    /// </summary>
    /// <param name="scopeFactory">Factory for creating a scope per sync.</param>
    /// <param name="options">The Cloudflare settings.</param>
    /// <param name="logger">The logger.</param>
    public class CloudflareSyncWorker(IServiceScopeFactory scopeFactory, IOptions<CloudflareOptions> options, ILogger<CloudflareSyncWorker> logger) : BackgroundService
    {
        /// <summary>
        /// Runs a sync immediately and then on every interval tick.
        /// </summary>
        /// <param name="stoppingToken">Token signalled when the app is stopping.</param>
        /// <returns>A task that completes when the worker stops.</returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (string.IsNullOrEmpty(options.Value.AccountId) || string.IsNullOrEmpty(options.Value.ApiToken))
            {
                logger.LogInformation("Cloudflare sync is off because the account ID or API token isn't set");
                return;
            }
            using PeriodicTimer timer = new(TimeSpan.FromMinutes(options.Value.SyncIntervalMinutes));
            do
            {
                try
                {
                    using IServiceScope scope = scopeFactory.CreateScope();
                    CloudflareSyncService sync = scope.ServiceProvider.GetRequiredService<CloudflareSyncService>();
                    int count = await sync.SyncAsync(stoppingToken);
                    logger.LogInformation("Cloudflare sync completed with {Count} tunnels", count);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    logger.LogError(exception, "Cloudflare sync failed");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
    }
}