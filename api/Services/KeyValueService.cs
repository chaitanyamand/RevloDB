using RevloDB.DTOs;
using RevloDB.Entities;
using RevloDB.Repositories.Interfaces;
using RevloDB.Services.Interfaces;

namespace RevloDB.Services
{
    public class KeyValueService : IKeyValueService
    {
        private readonly IBranchRepository _branchRepository;
        private readonly IStateResolutionService _stateResolutionService;
        private readonly IUnstagedChangeRepository _unstagedChangeRepository;

        public KeyValueService(
            IBranchRepository branchRepository,
            IStateResolutionService stateResolutionService,
            IUnstagedChangeRepository unstagedChangeRepository)
        {
            _branchRepository = branchRepository;
            _stateResolutionService = stateResolutionService;
            _unstagedChangeRepository = unstagedChangeRepository;
        }

        private async Task<Branch> GetBranchByNameAsync(string branchName, int namespaceId)
        {
            var branch = await _branchRepository.GetByNameAsync(branchName, namespaceId);
            if (branch == null)
                throw new KeyNotFoundException($"Branch '{branchName}' not found.");
            return branch;
        }

        public async Task<KeyValueDto?> GetKeyAsync(string branchName, string keyName, int namespaceId)
        {
            var branch = await GetBranchByNameAsync(branchName, namespaceId);
            var value = await _stateResolutionService.GetKeyValueAsync(branch.Id, keyName);

            if (value == null)
                return null;

            return new KeyValueDto
            {
                KeyName = keyName,
                Value = value
            };
        }

        public async Task<KeyValueListDto> GetAllKeysAsync(string branchName, int namespaceId)
        {
            var branch = await GetBranchByNameAsync(branchName, namespaceId);
            var kvDict = await _stateResolutionService.GetAllKeyValuesAsync(branch.Id);

            var keysList = kvDict.Select(kv => new KeyValueDto
            {
                KeyName = kv.Key,
                Value = kv.Value
            }).OrderBy(k => k.KeyName).ToList();

            string? headHash = branch.HeadCommit?.Hash;

            return new KeyValueListDto
            {
                Keys = keysList,
                BranchName = branchName,
                HeadCommitHash = headHash
            };
        }

        public async Task SetKeyAsync(string branchName, string keyName, string value, int namespaceId)
        {
            var branch = await GetBranchByNameAsync(branchName, namespaceId);

            var change = new UnstagedChange
            {
                BranchId = branch.Id,
                KeyName = keyName,
                Value = value,
                Action = ChangeAction.Modified,
                UpdatedAt = DateTime.UtcNow
            };

            await _unstagedChangeRepository.UpsertAsync(change);
        }

        public async Task DeleteKeyAsync(string branchName, string keyName, int namespaceId)
        {
            var branch = await GetBranchByNameAsync(branchName, namespaceId);

            var change = new UnstagedChange
            {
                BranchId = branch.Id,
                KeyName = keyName,
                Value = null,
                Action = ChangeAction.Deleted,
                UpdatedAt = DateTime.UtcNow
            };

            await _unstagedChangeRepository.UpsertAsync(change);
        }

        public async Task<List<KeyValueDto>> GetUnstagedChangesAsync(string branchName, int namespaceId)
        {
            var branch = await GetBranchByNameAsync(branchName, namespaceId);
            var unstaged = await _unstagedChangeRepository.GetAllForBranchAsync(branch.Id);

            return unstaged.Select(u => new KeyValueDto
            {
                KeyName = u.KeyName,
                Value = u.Action == ChangeAction.Deleted ? null : u.Value
            }).OrderBy(k => k.KeyName).ToList();
        }
    }
}
