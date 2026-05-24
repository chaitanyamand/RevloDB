using RevloDB.Entities;

namespace RevloDB.Repositories.Interfaces
{
    public interface IUnstagedChangeRepository
    {
        Task<List<UnstagedChange>> GetAllForBranchAsync(int branchId);
        Task UpsertAsync(UnstagedChange change);
        Task DeleteAllForBranchAsync(int branchId);
        Task<UnstagedChange?> GetByKeyAsync(int branchId, string keyName);
    }
}
