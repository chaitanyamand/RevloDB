using RevloDB.Entities;

namespace RevloDB.Repositories.Interfaces
{
    public interface IKeyRepository
    {
        Task<Key?> GetByIdAsync(int id);
        Task<Key?> GetByNameAsync(string keyName);
        Task<IEnumerable<Key>> GetAllAsync();
        Task<Key> CreateAsync(Key key);
        Task<Key> UpdateAsync(Key key);
        Task DeleteAsync(int id);
        Task<bool> ExistsAsync(string keyName);
    }
}