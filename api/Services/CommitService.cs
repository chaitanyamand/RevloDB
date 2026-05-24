using AutoMapper;
using RevloDB.Data;
using RevloDB.DTOs;
using RevloDB.Entities;
using RevloDB.Repositories.Interfaces;
using RevloDB.Services.Interfaces;
using RevloDB.Utility;

namespace RevloDB.Services
{
    public class CommitService : ICommitService
    {
        private readonly ICommitRepository _commitRepository;
        private readonly IBranchRepository _branchRepository;
        private readonly IUnstagedChangeRepository _unstagedChangeRepository;
        private readonly IBranchStateRepository _branchStateRepository;
        private readonly INamespaceRepository _namespaceRepository;
        private readonly RevloDbContext _context;
        private readonly IMapper _mapper;

        public CommitService(
            ICommitRepository commitRepository,
            IBranchRepository branchRepository,
            IUnstagedChangeRepository unstagedChangeRepository,
            IBranchStateRepository branchStateRepository,
            INamespaceRepository namespaceRepository,
            RevloDbContext context,
            IMapper mapper)
        {
            _commitRepository = commitRepository;
            _branchRepository = branchRepository;
            _unstagedChangeRepository = unstagedChangeRepository;
            _branchStateRepository = branchStateRepository;
            _namespaceRepository = namespaceRepository;
            _context = context;
            _mapper = mapper;
        }

        public async Task<CommitDto?> GetByHashAsync(string hash, string branchName, int namespaceId)
        {
            var branch = await _branchRepository.GetByNameAsync(branchName, namespaceId);
            if (branch == null)
                throw new KeyNotFoundException($"Branch '{branchName}' not found.");

            var commit = await _commitRepository.GetByHashAsync(hash, namespaceId);
            if (commit == null)
                return null;

            return _mapper.Map<CommitDto>(commit);
        }

        public async Task<List<CommitDto>> GetHistoryAsync(string branchName, int namespaceId, int limit)
        {
            var branch = await _branchRepository.GetByNameAsync(branchName, namespaceId);
            if (branch == null)
                throw new KeyNotFoundException($"Branch '{branchName}' not found.");

            if (branch.HeadCommitId == null)
                return new List<CommitDto>();

            var history = await _commitRepository.GetHistoryAsync(branch.HeadCommitId.Value, limit);
            return _mapper.Map<List<CommitDto>>(history);
        }

        public async Task<CommitDto> CreateCommitAsync(string branchName, CreateCommitDto dto, int namespaceId, int userId)
        {
            var branch = await _branchRepository.GetByNameAsync(branchName, namespaceId);
            if (branch == null)
                throw new KeyNotFoundException($"Branch '{branchName}' not found.");

            var unstagedChanges = await _unstagedChangeRepository.GetAllForBranchAsync(branch.Id);
            if (!unstagedChanges.Any())
                throw new InvalidOperationException("Nothing to commit: working tree clean.");

            var parentCommit = branch.HeadCommitId.HasValue
                ? await _commitRepository.GetByIdAsync(branch.HeadCommitId.Value)
                : null;

            var ns = await _namespaceRepository.GetByIdAsync(namespaceId);
            if (ns == null)
                throw new KeyNotFoundException("Namespace not found.");

            int newGeneration = (parentCommit?.Generation ?? 0) + 1;
            var timestamp = DateTime.UtcNow;

            var changesForHash = unstagedChanges.Select(u => (u.KeyName, u.Value, u.Action)).ToList();
            var hash = CommitHashUtil.ComputeCommitHash(
                parentCommit?.Hash,
                null,
                userId,
                timestamp,
                dto.Message,
                changesForHash
            );

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var commit = new Commit
                {
                    Hash = hash,
                    Message = dto.Message,
                    Timestamp = timestamp,
                    AuthorUserId = userId,
                    NamespaceId = namespaceId,
                    Generation = newGeneration,
                    ParentCommitId = parentCommit?.Id
                };

                await _commitRepository.CreateAsync(commit);

                var commitChanges = unstagedChanges.Select(u => new CommitChange
                {
                    CommitId = commit.Id,
                    KeyName = u.KeyName,
                    Value = u.Action == ChangeAction.Deleted ? null : u.Value,
                    Action = u.Action
                }).ToList();

                _context.CommitChanges.AddRange(commitChanges);

                await _branchRepository.UpdateHeadAsync(branch.Id, commit.Id);

                foreach (var unstaged in unstagedChanges)
                {
                    if (unstaged.Action == ChangeAction.Deleted)
                    {
                        await _branchStateRepository.DeleteAsync(branch.Id, unstaged.KeyName);
                    }
                    else
                    {
                        await _branchStateRepository.UpsertAsync(new BranchState
                        {
                            BranchId = branch.Id,
                            KeyName = unstaged.KeyName,
                            Value = unstaged.Value!,
                            LastModifiedCommitId = commit.Id
                        });
                    }
                }

                if (SnapshotUtil.ShouldCreateSnapshot(newGeneration, ns.SnapshotInterval))
                {
                    var branchStateList = await _branchStateRepository.GetAllAsync(branch.Id);
                    var stateDict = branchStateList.ToDictionary(bs => bs.KeyName, bs => bs.Value);
                    var snapshotJson = SnapshotUtil.SerializeState(stateDict);

                    _context.CommitSnapshots.Add(new CommitSnapshot
                    {
                        CommitId = commit.Id,
                        StateJson = snapshotJson
                    });
                }

                await _unstagedChangeRepository.DeleteAllForBranchAsync(branch.Id);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var createdCommit = await _commitRepository.GetByIdAsync(commit.Id);
                if (createdCommit != null)
                {
                    _context.Entry(createdCommit).Reference(c => c.AuthorUser).Load();
                    commit.AuthorUser = createdCommit.AuthorUser;
                }
                commit.Changes = commitChanges;

                return _mapper.Map<CommitDto>(commit);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
