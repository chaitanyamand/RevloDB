using Microsoft.EntityFrameworkCore;
using RevloDB.Data;
using RevloDB.DTOs;
using RevloDB.Entities;
using RevloDB.Exceptions;
using RevloDB.Repositories.Interfaces;
using RevloDB.Services.Interfaces;
using RevloDB.Utility;
using System.Data;

namespace RevloDB.Services
{
    public class MergeService : IMergeService
    {
        private readonly IBranchRepository _branchRepository;
        private readonly ICommitRepository _commitRepository;
        private readonly IStateResolutionService _stateResolutionService;
        private readonly INamespaceRepository _namespaceRepository;
        private readonly RevloDbContext _context;

        public MergeService(
            IBranchRepository branchRepository,
            ICommitRepository commitRepository,
            IStateResolutionService stateResolutionService,
            INamespaceRepository namespaceRepository,
            RevloDbContext context)
        {
            _branchRepository = branchRepository;
            _commitRepository = commitRepository;
            _stateResolutionService = stateResolutionService;
            _namespaceRepository = namespaceRepository;
            _context = context;
        }

        public async Task<MergeResultDto> MergeAsync(
            string targetBranchName,
            MergeRequestDto request,
            int namespaceId,
            int userId)
        {
            var targetBranch = await GetBranchAsync(targetBranchName, namespaceId, "Target");
            var sourceBranch = await GetBranchAsync(request.SourceBranchName, namespaceId, "Source");

            ValidateBranchIntegrity(targetBranch, sourceBranch);

            int targetHeadId = targetBranch.HeadCommitId ?? 0;
            int sourceHeadId = sourceBranch.HeadCommitId!.Value;

            if (await IsMergeUpToDateAsync(targetHeadId, sourceHeadId))
            {
                return new MergeResultDto { Success = true, IsNoOp = true };
            }

            var baseState = await ResolveBaseStateAsync(targetHeadId, sourceHeadId);
            var sourceState = await ResolveBranchStateAsync(sourceHeadId);
            var targetState = await ResolveBranchStateAsync(targetHeadId);

            var (mergedState, conflicts) = ComputeThreeWayMerge(baseState, sourceState, targetState);

            if (conflicts.Any())
            {
                return new MergeResultDto { Success = false, IsNoOp = false, Conflicts = conflicts };
            }

            var (commitChanges, finalStateForUpdate) = GenerateMergeDeltas(mergedState, targetState);

            var commitHash = await GenerateMergeCommitHashAsync(
                targetHeadId,
                sourceHeadId,
                userId,
                request.Message ?? $"Merge branch '{request.SourceBranchName}' into '{targetBranchName}'",
                commitChanges);

            await ApplyMergeTransactionAsync(
                targetBranch,
                namespaceId,
                userId,
                targetHeadId,
                sourceHeadId,
                commitHash,
                request.Message ?? $"Merge branch '{request.SourceBranchName}' into '{targetBranchName}'",
                commitChanges,
                finalStateForUpdate);

            return new MergeResultDto
            {
                Success = true,
                IsNoOp = false,
                MergeCommitHash = commitHash
            };
        }

        private async Task<Branch> GetBranchAsync(string branchName, int namespaceId, string branchType)
        {
            var branch = await _branchRepository.GetByNameAsync(branchName, namespaceId);
            if (branch == null)
            {
                throw new KeyNotFoundException($"{branchType} branch '{branchName}' not found.");
            }
            return branch;
        }

        private void ValidateBranchIntegrity(Branch targetBranch, Branch sourceBranch)
        {
            if (targetBranch.Id == sourceBranch.Id)
            {
                throw new ArgumentException("Cannot merge a branch into itself.");
            }

            if (sourceBranch.HeadCommitId == null)
            {
                throw new ArgumentException("Source branch has no commits.");
            }
        }

        private async Task<bool> IsMergeUpToDateAsync(int targetHeadId, int sourceHeadId)
        {
            if (targetHeadId == 0)
            {
                return false;
            }

            var ancestorsOfTarget = await _commitRepository.GetAncestorChainToRootAsync(targetHeadId);
            return ancestorsOfTarget.Any(c => c.Id == sourceHeadId);
        }

        private async Task<Dictionary<string, string?>> ResolveBaseStateAsync(int targetHeadId, int sourceHeadId)
        {
            if (targetHeadId == 0)
            {
                return new Dictionary<string, string?>();
            }

            var candidates = await _commitRepository.FindLCACandidatesAsync(targetHeadId, sourceHeadId);

            if (candidates.Count == 0)
            {
                return new Dictionary<string, string?>();
            }

            if (candidates.Count == 1)
            {
                return await ResolveBranchStateAsync(candidates[0]);
            }

            return await RecursiveMergeInMemoryAsync(candidates[0], candidates[1], 0);
        }

        private async Task<Dictionary<string, string?>> ResolveBranchStateAsync(int commitId)
        {
            if (commitId == 0)
            {
                return new Dictionary<string, string?>();
            }

            var dict = await _stateResolutionService.GetStateAtCommitAsync(commitId);
            return dict.ToDictionary(kvp => kvp.Key, kvp => (string?)kvp.Value);
        }

        private (Dictionary<string, string?> mergedState, List<MergeConflictDto> conflicts) ComputeThreeWayMerge(
            Dictionary<string, string?> baseState,
            Dictionary<string, string?> sourceState,
            Dictionary<string, string?> targetState)
        {
            var allKeys = baseState.Keys.Union(sourceState.Keys).Union(targetState.Keys).Distinct().ToList();
            var mergedState = new Dictionary<string, string?>();
            var conflicts = new List<MergeConflictDto>();

            foreach (var key in allKeys)
            {
                baseState.TryGetValue(key, out var baseVal);
                sourceState.TryGetValue(key, out var sourceVal);
                targetState.TryGetValue(key, out var targetVal);

                bool sourceChanged = baseVal != sourceVal;
                bool targetChanged = baseVal != targetVal;

                if (sourceChanged && !targetChanged)
                {
                    mergedState[key] = sourceVal;
                }
                else if (targetChanged && !sourceChanged)
                {
                    mergedState[key] = targetVal;
                }
                else if (sourceChanged && targetChanged)
                {
                    if (sourceVal == targetVal)
                    {
                        mergedState[key] = sourceVal;
                    }
                    else
                    {
                        conflicts.Add(new MergeConflictDto
                        {
                            Key = key,
                            CurrentValue = targetVal,
                            IncomingValue = sourceVal
                        });
                    }
                }
                else
                {
                    mergedState[key] = baseVal;
                }
            }

            return (mergedState, conflicts);
        }

        private (List<CommitChange> commitChanges, Dictionary<string, string?> finalStateForUpdate) GenerateMergeDeltas(
            Dictionary<string, string?> mergedState,
            Dictionary<string, string?> targetState)
        {
            var commitChanges = new List<CommitChange>();
            var finalStateForUpdate = new Dictionary<string, string?>();

            foreach (var kvp in mergedState)
            {
                var key = kvp.Key;
                var mergedVal = kvp.Value;
                targetState.TryGetValue(key, out var targetVal);

                if (mergedVal != targetVal)
                {
                    commitChanges.Add(new CommitChange
                    {
                        KeyName = key,
                        Value = mergedVal,
                        Action = mergedVal == null ? ChangeAction.Deleted : (targetVal == null ? ChangeAction.Added : ChangeAction.Modified)
                    });
                    finalStateForUpdate[key] = mergedVal;
                }
            }

            return (commitChanges, finalStateForUpdate);
        }

        private async Task<string> GenerateMergeCommitHashAsync(
            int targetHeadId,
            int sourceHeadId,
            int userId,
            string commitMessage,
            List<CommitChange> commitChanges)
        {
            var targetGenRecord = targetHeadId == 0 ? null : await _commitRepository.GetByIdAsync(targetHeadId);
            var sourceGenRecord = await _commitRepository.GetByIdAsync(sourceHeadId);

            var changesForHash = commitChanges.Select(c => (c.KeyName, c.Value, c.Action)).ToList();

            return CommitHashUtil.ComputeCommitHash(
                targetGenRecord?.Hash,
                sourceGenRecord?.Hash,
                userId,
                DateTime.UtcNow,
                commitMessage,
                changesForHash
            );
        }

        private async Task ApplyMergeTransactionAsync(
            Branch targetBranch,
            int namespaceId,
            int userId,
            int targetHeadId,
            int sourceHeadId,
            string commitHash,
            string commitMessage,
            List<CommitChange> commitChanges,
            Dictionary<string, string?> finalStateForUpdate)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead);
            try
            {
                var currentTargetBranch = await ValidateConcurrencyAsync(targetBranch);

                var targetGenRecord = targetHeadId == 0 ? null : await _commitRepository.GetByIdAsync(targetHeadId);
                var sourceGenRecord = await _commitRepository.GetByIdAsync(sourceHeadId);

                int targetGen = targetGenRecord?.Generation ?? 0;
                int sourceGen = sourceGenRecord?.Generation ?? 0;
                int newGeneration = Math.Max(targetGen, sourceGen) + 1;

                var commit = new Commit
                {
                    Hash = commitHash,
                    Message = commitMessage,
                    Timestamp = DateTime.UtcNow,
                    AuthorUserId = userId,
                    NamespaceId = namespaceId,
                    Generation = newGeneration,
                    ParentCommitId = targetHeadId == 0 ? null : targetHeadId,
                    MergeParentCommitId = sourceHeadId
                };

                _context.Commits.Add(commit);
                await _context.SaveChangesAsync();

                await PersistCommitChangesAsync(commit.Id, commitChanges);
                currentTargetBranch.HeadCommitId = commit.Id;

                await ApplyBranchStateMutationsAsync(targetBranch.Id, commit.Id, finalStateForUpdate);
                await GenerateSnapshotIfNeededAsync(namespaceId, newGeneration, targetBranch.Id, commit.Id);

                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private async Task<Branch> ValidateConcurrencyAsync(Branch targetBranch)
        {
            var currentTargetBranch = await _context.Branches.FirstOrDefaultAsync(b => b.Id == targetBranch.Id);

            if (currentTargetBranch == null)
            {
                throw new KeyNotFoundException($"Target branch '{targetBranch.Name}' was deleted.");
            }

            if (currentTargetBranch.HeadCommitId != targetBranch.HeadCommitId)
            {
                throw new ConcurrentMergeException($"Target branch '{targetBranch.Name}' was modified concurrently. Please try again.");
            }

            return currentTargetBranch;
        }

        private async Task PersistCommitChangesAsync(int commitId, List<CommitChange> commitChanges)
        {
            if (!commitChanges.Any()) return;

            foreach (var cc in commitChanges)
            {
                cc.CommitId = commitId;
            }

            _context.CommitChanges.AddRange(commitChanges);
            await _context.SaveChangesAsync();
        }

        private async Task ApplyBranchStateMutationsAsync(
            int branchId,
            int commitId,
            Dictionary<string, string?> finalStateForUpdate)
        {
            foreach (var kvp in finalStateForUpdate)
            {
                var existingState = await _context.BranchStates
                    .FirstOrDefaultAsync(bs => bs.BranchId == branchId && bs.KeyName == kvp.Key);

                if (kvp.Value == null)
                {
                    if (existingState != null)
                    {
                        _context.BranchStates.Remove(existingState);
                    }
                }
                else
                {
                    if (existingState != null)
                    {
                        existingState.Value = kvp.Value;
                        existingState.LastModifiedCommitId = commitId;
                    }
                    else
                    {
                        _context.BranchStates.Add(new BranchState
                        {
                            BranchId = branchId,
                            KeyName = kvp.Key,
                            Value = kvp.Value,
                            LastModifiedCommitId = commitId
                        });
                    }
                }
            }

            await _context.SaveChangesAsync();
        }

        private async Task GenerateSnapshotIfNeededAsync(
            int namespaceId,
            int newGeneration,
            int branchId,
            int commitId)
        {
            var ns = await _namespaceRepository.GetByIdAsync(namespaceId);

            if (ns != null && SnapshotUtil.ShouldCreateSnapshot(newGeneration, ns.SnapshotInterval))
            {
                var branchStateList = await _context.BranchStates
                    .Where(bs => bs.BranchId == branchId)
                    .ToListAsync();

                var stateDict = branchStateList.ToDictionary(bs => bs.KeyName, bs => bs.Value);
                var snapshotJson = SnapshotUtil.SerializeState(stateDict);

                _context.CommitSnapshots.Add(new CommitSnapshot
                {
                    CommitId = commitId,
                    StateJson = snapshotJson
                });

                await _context.SaveChangesAsync();
            }
        }

        private async Task<Dictionary<string, string?>> RecursiveMergeInMemoryAsync(
            int commitId1,
            int commitId2,
            int depth)
        {
            if (depth >= 10)
            {
                return await ResolveBranchStateAsync(commitId1);
            }

            var candidates = await _commitRepository.FindLCACandidatesAsync(commitId1, commitId2);

            Dictionary<string, string?> baseState;

            if (candidates.Count == 0)
            {
                baseState = new Dictionary<string, string?>();
            }
            else if (candidates.Count == 1)
            {
                baseState = await ResolveBranchStateAsync(candidates[0]);
            }
            else
            {
                baseState = await RecursiveMergeInMemoryAsync(candidates[0], candidates[1], depth + 1);
            }

            var state1 = await ResolveBranchStateAsync(commitId1);
            var state2 = await ResolveBranchStateAsync(commitId2);

            return ResolveVirtualThreeWayMerge(baseState, state1, state2);
        }

        private Dictionary<string, string?> ResolveVirtualThreeWayMerge(
            Dictionary<string, string?> baseState,
            Dictionary<string, string?> sourceState,
            Dictionary<string, string?> targetState)
        {
            var (mergedState, conflicts) = ComputeThreeWayMerge(baseState, sourceState, targetState);

            foreach (var conflict in conflicts)
            {
                mergedState[conflict.Key] = conflict.IncomingValue;
            }

            return mergedState;
        }
    }
}
