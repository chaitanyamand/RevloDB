using RevloDB.DTOs;

namespace RevloDB.Services.Interfaces
{
    public interface IBranchService
    {
        Task<BranchDto> CreateBranchAsync(CreateBranchDto dto, int namespaceId);
        Task<List<BranchDto>> GetAllBranchesAsync(int namespaceId);
        Task DeleteBranchAsync(string branchName, int namespaceId);
    }
}
