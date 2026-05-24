using RevloDB.Entities;

namespace RevloDB.Repositories.Interfaces
{
    public interface IBranchRepository
    {
        Task<Branch?> GetByNameAsync(string name, int namespaceId);
        Task<List<Branch>> GetAllAsync(int namespaceId);
        Task<Branch> CreateAsync(Branch branch);
        Task DeleteAsync(int id);
        Task UpdateHeadAsync(int id, int? commitId);
    }
}
