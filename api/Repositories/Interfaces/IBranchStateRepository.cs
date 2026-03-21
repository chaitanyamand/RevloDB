using RevloDB.Entities;

namespace RevloDB.Repositories.Interfaces
{
    public interface IBranchStateRepository
    {
        Task<BranchState?> GetAsync(int branchId, string keyName);
        Task<List<BranchState>> GetAllAsync(int branchId);
        Task UpsertAsync(BranchState state);
        Task DeleteAsync(int branchId, string keyName);
        Task BulkCopyFromBranchAsync(int sourceBranchId, int targetBranchId);
    }
}
