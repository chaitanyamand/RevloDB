using System.Threading;
namespace RevloDB.Repositories.Interfaces
{
    public interface ICleanupRepository
    {
        Task<int> DeleteMarkedKeysAsync(CancellationToken cancellationToken = default);
        Task<int> GetMarkedKeysCountAsync(CancellationToken cancellationToken = default);
    }
}