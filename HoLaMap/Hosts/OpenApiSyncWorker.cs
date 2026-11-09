using Microsoft.Extensions.Options;

namespace HoLaMap.Hosts
{
    /// <summary>
    /// Background worker that reads API routes on an interval, if any APIs are set up.
    /// </summary>
    /// <param name="scopeFactory">Factory for creating a scope per sync.</param>
    /// <param name="options">The OpenAPI settings.</param>
    /// <param name="logger">The logger.</param>
    public class OpenApiSyncWorker(IServiceScopeFactory scopeFactory, IOptions<OpenApiOptions> options, ILogger<OpenApiSyncWorker> logger) : BackgroundService
    {
        /// <summary>
        /// Runs a sync immediately and then on every interval tick.
        /// </summary>
        /// <param name="stoppingToken">Token signalled when the app is stopping.</param>
        /// <returns>A task that completes when the worker stops.</returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (options.Value.Sources.Count == 0)
            {
                logger.LogInformation("API route sync is off because no APIs are set up");
                return;
            }
            using PeriodicTimer timer = new(TimeSpan.FromMinutes(options.Value.SyncIntervalMinutes));
            do
            {
                try
                {
                    using IServiceScope scope = scopeFactory.CreateScope();
                    OpenApiSyncService sync = scope.ServiceProvider.GetRequiredService<OpenApiSyncService>();
                    await sync.SyncAsync(stoppingToken);
                    logger.LogInformation("API route sync completed");
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    logger.LogError(exception, "API route sync failed");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
    }
}