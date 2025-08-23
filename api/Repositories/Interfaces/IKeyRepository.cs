using RevloDB.Entities;
using Version = RevloDB.Entities.Version;

namespace RevloDB.Repositories.Interfaces
{
    public interface IKeyRepository
    {
        Task<Key?> GetByIdAsync(int id);
        Task<Key?> GetByNameAsync(string keyName);
        Task<IEnumerable<Key>> GetAllAsync();
        Task<bool> DeleteByNameAsync(string keyName);
        Task<bool> ExistsAsync(string keyName);
        Task<Key> CreateKeyWithVersionAsync(string keyName, string value);
        Task<Key> AddNewVersionAsync(string keyName, string value);
    }
}