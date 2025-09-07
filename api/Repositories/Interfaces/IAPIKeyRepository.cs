using RevloDB.Entities;

namespace RevloDB.Repositories.Interfaces
{
    public interface IAPIKeyRepository
    {
        Task<ApiKey> CreateAsync(ApiKey apiKey);
        Task<ApiKey?> GetByIdAsync(int id);
        Task<IEnumerable<ApiKey>> GetByUserIdAsync(int userId);
        Task DeleteAsync(int id);
    }
}