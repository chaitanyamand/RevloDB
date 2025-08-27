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

            _logger.LogDebug("Deleted {Count} keys with their versions", deletedCount);

            return deletedCount;
        }

        public async Task<int> GetMarkedUsersCountAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Users.CountAsync(u => u.IsDeleted, cancellationToken);
        }

        public async Task<int> DeleteMarkedUsersAsync(CancellationToken cancellationToken = default)
        {
            // Get users to be deleted for logging
            var usersToDelete = await _context.Users
                .Where(u => u.IsDeleted)
                .Select(u => new { u.Id, u.Username })
                .ToListAsync(cancellationToken);

            if (usersToDelete.Count == 0)
            {
                _logger.LogDebug("No inactive users found to delete");
                return 0;
            }

            var userIds = usersToDelete.Select(u => u.Id).ToList();
            var usersWithActiveNamespaces = await _context.Namespaces
                .Where(n => userIds.Contains(n.CreatedByUserId) && !n.IsDeleted)
                .Select(n => n.CreatedByUserId)
                .Distinct()
                .ToListAsync(cancellationToken);

            if (usersWithActiveNamespaces.Count > 0)
            {
                var conflictUsers = usersToDelete.Where(u => usersWithActiveNamespaces.Contains(u.Id)).ToList();
                _logger.LogWarning("Cannot delete {Count} users as they have active namespaces: {Users}",
                    conflictUsers.Count, string.Join(", ", conflictUsers.Select(u => u.Username)));

                usersToDelete = usersToDelete.Where(u => !usersWithActiveNamespaces.Contains(u.Id)).ToList();
            }

            if (usersToDelete.Count == 0)
            {
                _logger.LogDebug("No users eligible for deletion after dependency check");
                return 0;
            }

            var eligibleUserIds = usersToDelete.Select(u => u.Id).ToList();
            var deletedCount = await _context.Users
                .Where(u => eligibleUserIds.Contains(u.Id))
                .ExecuteDeleteAsync(cancellationToken);

            _logger.LogDebug("Deleted {Count} inactive users: {Users}",
                deletedCount, string.Join(", ", usersToDelete.Select(u => u.Username)));

            return deletedCount;
        }

        public async Task<int> GetMarkedNamespacesCountAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Namespaces.CountAsync(n => n.IsDeleted, cancellationToken);
        }

        public async Task<int> DeleteMarkedNamespacesAsync(CancellationToken cancellationToken = default)
        {
            var namespacesToDelete = await _context.Namespaces
                .Where(n => n.IsDeleted)
                .Select(n => new { n.Id, n.Name })
                .ToListAsync(cancellationToken);

            if (!namespacesToDelete.Any())
            {
                _logger.LogDebug("No inactive namespaces found to delete");
                return 0;
            }

            var namespaceIds = namespacesToDelete.Select(n => n.Id).ToList();

            await _context.Keys
                .Where(k => namespaceIds.Contains(k.NamespaceId))
                .ExecuteUpdateAsync(s => s.SetProperty(k => k.CurrentVersionId, k => null), cancellationToken);

            _logger.LogDebug("Cleared CurrentVersionId references for keys in {Count} namespaces", namespaceIds.Count);

            var deletedCount = await _context.Namespaces
                .Where(n => namespaceIds.Contains(n.Id))
                .ExecuteDeleteAsync(cancellationToken);

            _logger.LogDebug("Deleted {Count} inactive namespaces: {Namespaces}",
                deletedCount, string.Join(", ", namespacesToDelete.Select(n => n.Name)));

            return deletedCount;
        }

        public async Task<CleanupResult> PerformFullCleanupAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting full cleanup operation");

            var result = new CleanupResult();

            try
            {
                result.DeletedKeys = await DeleteMarkedKeysAsync(cancellationToken);
                result.DeletedNamespaces = await DeleteMarkedNamespacesAsync(cancellationToken);
                result.DeletedUsers = await DeleteMarkedUsersAsync(cancellationToken);

                _logger.LogInformation("Full cleanup completed successfully. Deleted: {Keys} keys, {Namespaces} namespaces, {Users} users",
                    result.DeletedKeys, result.DeletedNamespaces, result.DeletedUsers);
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
            var summary = new CleanupSummary
            {
                MarkedKeysCount = await GetMarkedKeysCountAsync(cancellationToken),
                MarkedUsersCount = await GetMarkedUsersCountAsync(cancellationToken),
                MarkedNamespacesCount = await GetMarkedNamespacesCountAsync(cancellationToken)
            };

            return summary;
        }
    }
}