using AutoMapper;
using RevloDB.Constants;
using RevloDB.DTOs;
using RevloDB.Entities;
using RevloDB.Repositories.Interfaces;
using RevloDB.Services.Interfaces;

namespace RevloDB.Services
{
    public class BranchService : IBranchService
    {
        private readonly IBranchRepository _branchRepository;
        private readonly IBranchStateRepository _branchStateRepository;
        private readonly IMapper _mapper;

        public BranchService(
            IBranchRepository branchRepository,
            IBranchStateRepository branchStateRepository,
            IMapper mapper)
        {
            _branchRepository = branchRepository;
            _branchStateRepository = branchStateRepository;
            _mapper = mapper;
        }

        public async Task<BranchDto> CreateBranchAsync(CreateBranchDto dto, int namespaceId)
        {
            var sourceBranch = await _branchRepository.GetByNameAsync(dto.SourceBranchName, namespaceId);
            if (sourceBranch == null)
            {
                throw new KeyNotFoundException($"Source branch '{dto.SourceBranchName}' does not exist.");
            }

            var existingBranch = await _branchRepository.GetByNameAsync(dto.Name, namespaceId);
            if (existingBranch != null)
            {
                throw new InvalidOperationException($"Branch '{dto.Name}' already exists.");
            }

            var newBranch = new Branch
            {
                Name = dto.Name,
                NamespaceId = namespaceId,
                HeadCommitId = sourceBranch.HeadCommitId,
                CreatedAt = DateTime.UtcNow
            };

            await _branchRepository.CreateAsync(newBranch);

            await _branchStateRepository.BulkCopyFromBranchAsync(sourceBranch.Id, newBranch.Id);

            return _mapper.Map<BranchDto>(newBranch);
        }

        public async Task<List<BranchDto>> GetAllBranchesAsync(int namespaceId)
        {
            var branches = await _branchRepository.GetAllAsync(namespaceId);
            return _mapper.Map<List<BranchDto>>(branches);
        }

        public async Task DeleteBranchAsync(string branchName, int namespaceId)
        {
            if (branchName.Equals(BranchConstants.DefaultMainBranchName, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Cannot delete the 'main' branch.");
            }

            var branch = await _branchRepository.GetByNameAsync(branchName, namespaceId);
            if (branch == null)
            {
                throw new KeyNotFoundException($"Branch '{branchName}' not found.");
            }

            await _branchRepository.DeleteAsync(branch.Id);
        }
    }
}
