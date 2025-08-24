using RevloDB.Entities;

namespace RevloDB.Repositories.Interfaces
{
    public interface IKeyRepository
    {
        Task<Key> CreateKeyWithVersionAsync(string keyName, string value);
        Task<Key?> GetByIdAsync(int id);
        Task<Key?> GetByNameAsync(string keyName);
        Task<bool> DeleteByNameAsync(string keyName);
        Task<bool> RestoreByNameAsync(string keyName);
        Task<IEnumerable<Key>> GetAllAsync();
        Task<Key> AddNewVersionAsync(string keyName, string value);
        Task<bool> ExistsAsync(string keyName);
        Task<Key> RevertToVersionAsync(string keyName, int targetVersionNumber);
    }
}