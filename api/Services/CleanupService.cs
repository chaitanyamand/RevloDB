using System.Diagnostics;
using RevloDB.Repositories.Interfaces;

namespace RevloDB.Services
{
    public class CleanupService : ICleanupService
    {
        private readonly ICleanupRepository _cleanupRepository;
        private readonly ILogger<CleanupService> _logger;

        public CleanupService(ICleanupRepository cleanupRepository, ILogger<CleanupService> logger)
        {
            _cleanupRepository = cleanupRepository;
            _logger = logger;
        }

        public async Task<CleanupResult> ExecuteCleanupAsync()
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var initialCount = await _cleanupRepository.GetMarkedKeysCountAsync();
                _logger.LogInformation("Starting cleanup. Found {Count} keys marked for deletion", initialCount);

                if (initialCount == 0)
                {
                    return new CleanupResult
                    {
                        Success = true,
                        DeletedKeysCount = 0,
                        Duration = stopwatch.Elapsed
                    };
                }

                var deletedCount = await _cleanupRepository.DeleteMarkedKeysAsync();

                stopwatch.Stop();

                _logger.LogInformation("Cleanup completed successfully. Deleted {Count} keys in {Duration}ms",
                    deletedCount, stopwatch.ElapsedMilliseconds);

                return new CleanupResult
                {
                    Success = true,
                    DeletedKeysCount = deletedCount,
                    Duration = stopwatch.Elapsed
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error occurred during cleanup execution");

                return new CleanupResult
                {
                    Success = false,
                    DeletedKeysCount = 0,
                    Duration = stopwatch.Elapsed,
                    ErrorMessage = ex.Message
                };
            }
        }
    }
}