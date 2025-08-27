using System.Diagnostics;
using RevloDB.Repositories.Interfaces;
using RevloDB.Services.Interfaces;

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

        public async Task<CleanupServiceResult> ExecuteCleanupAsync(CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var summary = await _cleanupRepository.GetCleanupSummaryAsync();
                _logger.LogInformation("Found {Total} entities marked for deletion", summary.TotalMarkedForDeletion);

                var result = await _cleanupRepository.PerformFullCleanupAsync();
                _logger.LogInformation("Cleanup completed. Total deleted: {Total}", result.TotalDeleted);
                stopwatch.Stop();

                return new CleanupServiceResult
                {
                    Success = true,
                    DeletedCount = result.TotalDeleted,
                    Duration = stopwatch.Elapsed
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error occurred during cleanup execution");

                return new CleanupServiceResult
                {
                    Success = false,
                    DeletedCount = 0,
                    Duration = stopwatch.Elapsed,
                    ErrorMessage = ex.Message
                };
            }
        }
    }
}