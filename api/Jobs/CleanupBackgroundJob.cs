using Microsoft.Extensions.Options;
using RevloDB.Configuration;
using RevloDB.Services.Interfaces;

namespace RevloDB.Jobs
{
    public class CleanupBackgroundJob : BackgroundService
    {
        private readonly ILogger<CleanupBackgroundJob> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly CleanupJobOptions _options;

        public CleanupBackgroundJob(
            ILogger<CleanupBackgroundJob> logger,
            IServiceProvider serviceProvider,
            IOptions<CleanupJobOptions> options)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _options = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.Enabled)
            {
                _logger.LogInformation("Cleanup job is disabled via configuration");
                return;
            }

            _logger.LogInformation("Cleanup job started. Will run every {Hours} hours",
                _options.IntervalHours);

            await Task.Delay(TimeSpan.FromMinutes(_options.StartDelayMinutes), stoppingToken);
            _logger.LogInformation("Initial delay of {Minutes} minutes completed. Starting cleanup tasks.", _options.StartDelayMinutes);

            if (!stoppingToken.IsCancellationRequested)
            {
                await ExecuteCleanupTask(stoppingToken);
            }

            using var timer = new PeriodicTimer(TimeSpan.FromHours(_options.IntervalHours));

            while (!stoppingToken.IsCancellationRequested &&
                   await timer.WaitForNextTickAsync(stoppingToken))
            {
                await ExecuteCleanupTask(stoppingToken);
            }
        }

        private async Task ExecuteCleanupTask(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Starting scheduled cleanup task at {Time}", DateTime.UtcNow);

                using var scope = _serviceProvider.CreateScope();
                var cleanupService = scope.ServiceProvider.GetRequiredService<ICleanupService>();

                var result = await cleanupService.ExecuteCleanupAsync(cancellationToken);

                if (result.Success)
                {
                    if (_options.LogDetailedInfo)
                    {
                        _logger.LogInformation("Cleanup task completed successfully. " +
                            "Deleted: {Count} keys, Duration: {Duration}ms",
                            result.DeletedCount, result.Duration.TotalMilliseconds);
                    }
                    else
                    {
                        _logger.LogInformation("Cleanup task completed. Deleted {Count} keys", result.DeletedCount);
                    }
                }
                else
                {
                    _logger.LogError("Cleanup task failed: {Error}", result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during cleanup task execution");
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Cleanup job is stopping");
            await base.StopAsync(stoppingToken);
        }
    }
}