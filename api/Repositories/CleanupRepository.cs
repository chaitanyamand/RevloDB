using Microsoft.EntityFrameworkCore;
using RevloDB.Data;
using RevloDB.Repositories.Interfaces;
using RevloDB.DTOs;

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

            _logger.LogDebug("Deleted {Count} marked keys", deletedCount);
            return deletedCount;
        }

        public async Task<int> GetMarkedUsersCountAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Users.CountAsync(u => u.IsDeleted, cancellationToken);
        }

        public async Task<int> DeleteMarkedUsersAsync(CancellationToken cancellationToken = default)
        {
            var deletedCount = await _context.Users
                .Where(u => u.IsDeleted)
                .ExecuteDeleteAsync(cancellationToken);

            _logger.LogDebug("Deleted {Count} marked users", deletedCount);
            return deletedCount;
        }

        public async Task<int> GetMarkedNamespacesCountAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Namespaces.CountAsync(n => n.IsDeleted, cancellationToken);
        }

        public async Task<int> DeleteMarkedNamespacesAsync(CancellationToken cancellationToken = default)
        {
            await _context.Keys
                .Where(k => _context.Namespaces.Any(n => n.Id == k.NamespaceId && n.IsDeleted))
                .ExecuteUpdateAsync(s => s.SetProperty(k => k.CurrentVersionId, k => null), cancellationToken);

            var deletedCount = await _context.Namespaces
                .Where(n => n.IsDeleted)
                .ExecuteDeleteAsync(cancellationToken);

            _logger.LogDebug("Deleted {Count} marked namespaces", deletedCount);
            return deletedCount;
        }

        public async Task<int> GetMarkedApiKeysCountAsync(CancellationToken cancellationToken = default)
        {
            return await _context.ApiKeys.CountAsync(a => a.IsDeleted, cancellationToken);
        }

        public async Task<int> DeleteMarkedApiKeysAsync(CancellationToken cancellationToken = default)
        {
            var deletedCount = await _context.ApiKeys
                .Where(a => a.IsDeleted)
                .ExecuteDeleteAsync(cancellationToken);

            _logger.LogDebug("Deleted {Count} marked API keys", deletedCount);
            return deletedCount;
        }

        public async Task<int> GetExpiredApiKeysCountAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            return await _context.ApiKeys
                .CountAsync(a => !a.IsDeleted && a.ExpiresAt.HasValue && a.ExpiresAt.Value <= now, cancellationToken);
        }

        public async Task<int> DeleteExpiredApiKeysAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var deletedCount = await _context.ApiKeys
                .Where(a => !a.IsDeleted && a.ExpiresAt.HasValue && a.ExpiresAt.Value <= now)
                .ExecuteDeleteAsync(cancellationToken);

            _logger.LogDebug("Deleted {Count} expired API keys", deletedCount);
            return deletedCount;
        }

        public async Task<CleanupResult> PerformFullCleanupAsync(bool includeExpiredApiKeys = true, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting full cleanup operation");

            var result = new CleanupResult();

            try
            {
                result.DeletedKeys = await DeleteMarkedKeysAsync(cancellationToken);
                result.DeletedNamespaces = await DeleteMarkedNamespacesAsync(cancellationToken);
                result.DeletedUsers = await DeleteMarkedUsersAsync(cancellationToken);
                result.DeletedApiKeys = await DeleteMarkedApiKeysAsync(cancellationToken);

                if (includeExpiredApiKeys)
                {
                    result.DeletedExpiredApiKeys = await DeleteExpiredApiKeysAsync(cancellationToken);
                }

                _logger.LogInformation("Full cleanup completed. Deleted: {Keys} keys, {Namespaces} namespaces, {Users} users, {ApiKeys} API keys, {ExpiredApiKeys} expired API keys",
                    result.DeletedKeys, result.DeletedNamespaces, result.DeletedUsers, result.DeletedApiKeys, result.DeletedExpiredApiKeys);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during full cleanup operation");
                throw;
            }

            return result;
        }

        public async Task<CleanupSummary> GetCleanupSummaryAsync(CancellationToken cancellationToken = default)
        {
            return new CleanupSummary
            {
                MarkedKeysCount = await GetMarkedKeysCountAsync(cancellationToken),
                MarkedUsersCount = await GetMarkedUsersCountAsync(cancellationToken),
                MarkedNamespacesCount = await GetMarkedNamespacesCountAsync(cancellationToken),
                MarkedApiKeysCount = await GetMarkedApiKeysCountAsync(cancellationToken),
                ExpiredApiKeysCount = await GetExpiredApiKeysCountAsync(cancellationToken)
            };
        }
    }
}