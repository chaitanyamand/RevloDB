using RevloDB.DTOs;

namespace RevloDB.Repositories.Interfaces
{
    public interface ICleanupRepository
    {
        // Key cleanup operations
        Task<int> GetMarkedKeysCountAsync(CancellationToken cancellationToken = default);
        Task<int> DeleteMarkedKeysAsync(CancellationToken cancellationToken = default);

        // User cleanup operations
        Task<int> GetMarkedUsersCountAsync(CancellationToken cancellationToken = default);
        Task<int> DeleteMarkedUsersAsync(CancellationToken cancellationToken = default);

        // Namespace cleanup operations
        Task<int> GetMarkedNamespacesCountAsync(CancellationToken cancellationToken = default);
        Task<int> DeleteMarkedNamespacesAsync(CancellationToken cancellationToken = default);

        // API Key cleanup operations
        Task<int> GetMarkedApiKeysCountAsync(CancellationToken cancellationToken = default);
        Task<int> DeleteMarkedApiKeysAsync(CancellationToken cancellationToken = default);

        // Expired API Key cleanup operations
        Task<int> GetExpiredApiKeysCountAsync(CancellationToken cancellationToken = default);
        Task<int> DeleteExpiredApiKeysAsync(CancellationToken cancellationToken = default);

        // Combined operations
        Task<CleanupResult> PerformFullCleanupAsync(bool includeExpiredApiKeys = true, CancellationToken cancellationToken = default);
        Task<CleanupSummary> GetCleanupSummaryAsync(CancellationToken cancellationToken = default);
    }
}