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

        public async Task<int> GetMarkedKeysCountAsync()
        {
            return await _context.Keys.CountAsync(k => k.IsDeleted);
        }

        public async Task<int> DeleteMarkedKeysAsync()
        {
            await _context.Keys
                .Where(k => k.IsDeleted)
                .ExecuteUpdateAsync(s => s.SetProperty(k => k.CurrentVersionId, k => null));
            var deletedCount = await _context.Keys
                .Where(k => k.IsDeleted)
                .ExecuteDeleteAsync();

            _logger.LogDebug("Deleted {Count} keys with their versions", deletedCount);

            return deletedCount;
        }
    }
}