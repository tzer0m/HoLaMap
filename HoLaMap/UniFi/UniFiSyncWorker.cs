using Microsoft.Extensions.Options;

namespace HoLaMap.UniFi
{
    /// <summary>
    /// Background worker that runs the UniFi sync on an interval.
    /// </summary>
    public class UniFiSyncWorker : BackgroundService
    {
        /// <summary>
        /// Factory for creating a scope per sync.
        /// </summary>
        private readonly IServiceScopeFactory ScopeFactory;

        /// <summary>
        /// The UniFi settings.
        /// </summary>
        private readonly UniFiOptions Options;

        /// <summary>
        /// The logger.
        /// </summary>
        private readonly ILogger<UniFiSyncWorker> Logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="UniFiSyncWorker"/> class.
        /// </summary>
        /// <param name="scopeFactory">Factory for creating a scope per sync.</param>
        /// <param name="options">The UniFi settings.</param>
        /// <param name="logger">The logger.</param>
        public UniFiSyncWorker(IServiceScopeFactory scopeFactory, IOptions<UniFiOptions> options, ILogger<UniFiSyncWorker> logger)
        {
            ScopeFactory = scopeFactory;
            Options = options.Value;
            Logger = logger;
        }

        /// <summary>
        /// Runs a sync immediately and then on every interval tick.
        /// </summary>
        /// <param name="stoppingToken">Token signalled when the app is stopping.</param>
        /// <returns>A task that completes when the worker stops.</returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using PeriodicTimer timer = new PeriodicTimer(TimeSpan.FromMinutes(Options.SyncIntervalMinutes));
            do
            {
                try
                {
                    using IServiceScope scope = ScopeFactory.CreateScope();
                    UniFiSyncService sync = scope.ServiceProvider.GetRequiredService<UniFiSyncService>();
                    await sync.SyncAsync(stoppingToken);
                    Logger.LogInformation("UniFi sync completed");
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    Logger.LogError(exception, "UniFi sync failed");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
    }
}