using RevloDB.Entities;

namespace RevloDB.Repositories.Interfaces
{
    public interface INamespaceRepository
    {
        Task<Namespace?> GetByIdAsync(int id);
        Task<Namespace?> GetByNameAsync(string namespaceName);
        Task<Namespace> CreateAsync(string namespaceName, int createdByUserId);
        Task<Namespace> UpdateNameAsync(string newName, int id);
        Task DeleteAsync(int id);
    }
}