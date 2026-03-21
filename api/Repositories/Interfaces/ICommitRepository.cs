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
    }
}
