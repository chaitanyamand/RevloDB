namespace RevloDB.Services.Interfaces
{
    public interface IStateResolutionService
    {
        Task<Dictionary<string, string>> GetStateAtCommitAsync(int commitId);
        Task<Dictionary<string, string>> GetBranchHeadStateAsync(int branchId);
        Task<string?> GetKeyValueAsync(int branchId, string keyName);
        Task<Dictionary<string, string>> GetAllKeyValuesAsync(int branchId);
    }
}
