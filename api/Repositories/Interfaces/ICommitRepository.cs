using RevloDB.Entities;

namespace RevloDB.Repositories.Interfaces
{
    public interface ICommitRepository
    {
        Task<Commit?> GetByIdAsync(int commitId);
        Task<Commit?> GetByIdWithSnapshotAsync(int commitId);
        Task<Commit?> GetNearestSnapshotAncestorAsync(int commitId);
        Task<List<Commit>> GetAncestorChainAsync(int fromCommitId, int toAncestorCommitId);
        Task<List<Commit>> GetAncestorChainToRootAsync(int commitId);
        Task<Commit?> GetByHashAsync(string hash, int namespaceId);
        Task<List<Commit>> GetHistoryAsync(int startCommitId, int limit);
        Task<Commit> CreateAsync(Commit commit);
        Task<List<int>> FindLCACandidatesAsync(int commitId1, int commitId2);
    }
}
