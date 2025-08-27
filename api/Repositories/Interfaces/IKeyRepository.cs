using RevloDB.Entities;

namespace RevloDB.Repositories.Interfaces
{
    public interface IKeyRepository
    {
        Task<Key> CreateKeyWithVersionAsync(string keyName, string value, int namespaceId);
        Task<Key?> GetByIdAsync(int id, int namespaceId);
        Task<Key?> GetByNameAsync(string keyName, int namespaceId);
        Task<bool> DeleteByNameAsync(string keyName, int namespaceId);
        Task<bool> RestoreByNameAsync(string keyName, int namespaceId);
        Task<IEnumerable<Key>> GetAllAsync(int namespaceId);
        Task<Key> AddNewVersionAsync(string keyName, string value, int namespaceId);
        Task<bool> ExistsAsync(string keyName, int namespaceId);
        Task<Key> RevertToVersionAsync(string keyName, int targetVersionNumber, int namespaceId);
    }
}