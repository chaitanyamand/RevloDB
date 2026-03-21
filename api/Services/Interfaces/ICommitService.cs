using RevloDB.DTOs;

namespace RevloDB.Services.Interfaces
{
    public interface ICommitService
    {
        Task<CommitDto> CreateCommitAsync(string branchName, CreateCommitDto dto, int namespaceId, int userId);
        Task<List<CommitDto>> GetHistoryAsync(string branchName, int namespaceId, int limit);
        Task<CommitDto?> GetByHashAsync(string hash, string branchName, int namespaceId);
    }
}
