using Microsoft.EntityFrameworkCore;
using RevloDB.Data;
using RevloDB.Repositories.Interfaces;

namespace RevloDB.Repositories
{
    public class CleanupRepository : ICleanupRepository
    {
        private readonly RevloDbContext _context;
        private readonly ILogger<CleanupRepository> _logger;

        public CleanupRepository(RevloDbContext context, ILogger<CleanupRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<int> GetMarkedKeysCountAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Keys.CountAsync(k => k.IsDeleted, cancellationToken);
        }

        public async Task<int> DeleteMarkedKeysAsync(CancellationToken cancellationToken = default)
        {
            await _context.Keys
                .Where(k => k.IsDeleted)
                .ExecuteUpdateAsync(s => s.SetProperty(k => k.CurrentVersionId, k => null), cancellationToken);
            var deletedCount = await _context.Keys
                .Where(k => k.IsDeleted)
                .ExecuteDeleteAsync(cancellationToken);

            _logger.LogDebug("Deleted {Count} keys with their versions", deletedCount);

            return deletedCount;
        }
    }
}