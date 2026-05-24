using Microsoft.EntityFrameworkCore;
using RevloDB.Data;
using RevloDB.Entities;
using RevloDB.Utility;
using RevloDB.Repositories.Interfaces;
using RevloDB.Services.Interfaces;

namespace RevloDB.Services
{
    public class StateResolutionService : IStateResolutionService
    {
        private readonly ICommitRepository _commitRepository;
        private readonly RevloDbContext _context;

        public StateResolutionService(ICommitRepository commitRepository, RevloDbContext context)
        {
            _commitRepository = commitRepository;
            _context = context;
        }

        public async Task<Dictionary<string, string>> GetStateAtCommitAsync(int commitId)
        {
            var targetCommit = await _commitRepository.GetByIdAsync(commitId);
            if (targetCommit == null)
                throw new KeyNotFoundException($"Commit with ID '{commitId}' not found.");

            var snapshotAncestor = await _commitRepository.GetNearestSnapshotAncestorAsync(commitId);

            Dictionary<string, string> state;
            List<Commit> chain;

            if (snapshotAncestor != null)
            {
                state = SnapshotUtil.DeserializeState(snapshotAncestor.Snapshot!.StateJson);

                if (snapshotAncestor.Id == commitId)
                    return state;

                chain = await _commitRepository.GetAncestorChainAsync(commitId, snapshotAncestor.Id);
            }
            else
            {
                state = new Dictionary<string, string>();
                chain = await _commitRepository.GetAncestorChainToRootAsync(commitId);
            }

            foreach (var commit in chain)
            {
                ApplyChanges(state, commit.Changes);
            }

            return state;
        }

        public async Task<Dictionary<string, string>> GetBranchHeadStateAsync(int branchId)
        {
            var branchExists = await _context.Branches
                .AsNoTracking()
                .AnyAsync(b => b.Id == branchId);

            if (!branchExists)
                throw new KeyNotFoundException($"Branch with ID '{branchId}' not found.");

            return await _context.BranchStates
                .AsNoTracking()
                .Where(bs => bs.BranchId == branchId)
                .ToDictionaryAsync(bs => bs.KeyName, bs => bs.Value);
        }

        public async Task<string?> GetKeyValueAsync(int branchId, string keyName)
        {
            var unstaged = await _context.UnstagedChanges
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.BranchId == branchId && u.KeyName == keyName);

            if (unstaged != null)
                return unstaged.Action == ChangeAction.Deleted ? null : unstaged.Value;

            var branchState = await _context.BranchStates
                .AsNoTracking()
                .FirstOrDefaultAsync(bs => bs.BranchId == branchId && bs.KeyName == keyName);

            return branchState?.Value;
        }

        public async Task<Dictionary<string, string>> GetAllKeyValuesAsync(int branchId)
        {
            var committed = await _context.BranchStates
                .AsNoTracking()
                .Where(bs => bs.BranchId == branchId)
                .ToDictionaryAsync(bs => bs.KeyName, bs => bs.Value);

            var unstaged = await _context.UnstagedChanges
                .AsNoTracking()
                .Where(u => u.BranchId == branchId)
                .ToListAsync();

            ApplyChanges(committed, unstaged);

            return committed;
        }

        private static void ApplyChanges<T>(Dictionary<string, string> state, ICollection<T> changes)
            where T : IChange
        {
            foreach (var change in changes)
                ApplyChange(state, change.KeyName, change.Value, change.Action);
        }

        private static void ApplyChange(Dictionary<string, string> state, string keyName, string? value, ChangeAction action)
        {
            switch (action)
            {
                case ChangeAction.Added:
                case ChangeAction.Modified:
                    state[keyName] = value ?? string.Empty;
                    break;
                case ChangeAction.Deleted:
                    state.Remove(keyName);
                    break;
            }
        }
    }
}
