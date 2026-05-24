using RevloDB.DTOs;

namespace RevloDB.Services.Interfaces
{
    public interface IMergeService
    {
        Task<MergeResultDto> MergeAsync(
            string targetBranchName,
            MergeRequestDto request,
            int namespaceId,
            int userId);
    }
}
