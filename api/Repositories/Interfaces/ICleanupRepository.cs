using RevloDB.DTOs;

namespace RevloDB.Repositories.Interfaces
{
    public interface ICleanupRepository
    {
        Task<int> GetMarkedKeysCountAsync(CancellationToken cancellationToken = default);
        Task<int> DeleteMarkedKeysAsync(CancellationToken cancellationToken = default);
        Task<int> GetMarkedUsersCountAsync(CancellationToken cancellationToken = default);
        Task<int> DeleteMarkedUsersAsync(CancellationToken cancellationToken = default);
        Task<int> GetMarkedNamespacesCountAsync(CancellationToken cancellationToken = default);
        Task<int> DeleteMarkedNamespacesAsync(CancellationToken cancellationToken = default);
        Task<CleanupResult> PerformFullCleanupAsync(CancellationToken cancellationToken = default);
        Task<CleanupSummary> GetCleanupSummaryAsync(CancellationToken cancellationToken = default);
    }
}