using RevloDB.Entities;

namespace RevloDB.Repositories.Interfaces
{
    public interface INamespaceRepository
    {
        Task<Namespace?> GetByIdAsync(int id);
        Task<Namespace?> GetByNameAsync(string namespaceName, int userId);
        Task<Namespace> CreateAsync(string namespaceName, string? namespaceDescription, int createdByUserId);
        Task<Namespace> UpdateNamespaceAsync(string newName, string? newDescription, int id);
        Task DeleteAsync(int id);
    }
}