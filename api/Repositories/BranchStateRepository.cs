using Microsoft.EntityFrameworkCore;
using RevloDB.Data;
using RevloDB.Entities;
using RevloDB.Repositories.Interfaces;

namespace RevloDB.Repositories
{
    public class BranchStateRepository : IBranchStateRepository
    {
        private readonly RevloDbContext _context;

        public BranchStateRepository(RevloDbContext context)
        {
            _context = context;
        }

        public async Task<BranchState?> GetAsync(int branchId, string keyName)
        {
            return await _context.BranchStates
                .AsNoTracking()
                .FirstOrDefaultAsync(bs => bs.BranchId == branchId && bs.KeyName == keyName);
        }

        public async Task<List<BranchState>> GetAllAsync(int branchId)
        {
            return await _context.BranchStates
                .AsNoTracking()
                .Where(bs => bs.BranchId == branchId)
                .OrderBy(bs => bs.KeyName)
                .ToListAsync();
        }

        public async Task UpsertAsync(BranchState state)
        {
            var existing = await _context.BranchStates
                .FirstOrDefaultAsync(bs => bs.BranchId == state.BranchId && bs.KeyName == state.KeyName);

            if (existing != null)
            {
                existing.Value = state.Value;
                existing.LastModifiedCommitId = state.LastModifiedCommitId;
            }
            else
            {
                _context.BranchStates.Add(state);
            }

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int branchId, string keyName)
        {
            var state = await _context.BranchStates
                .FirstOrDefaultAsync(bs => bs.BranchId == branchId && bs.KeyName == keyName);

            if (state != null)
            {
                _context.BranchStates.Remove(state);
                await _context.SaveChangesAsync();
            }
        }

        public async Task BulkCopyFromBranchAsync(int sourceBranchId, int targetBranchId)
        {
            var sourceStates = await _context.BranchStates
                .AsNoTracking()
                .Where(bs => bs.BranchId == sourceBranchId)
                .ToListAsync();

            var newStates = sourceStates.Select(s => new BranchState
            {
                BranchId = targetBranchId,
                KeyName = s.KeyName,
                Value = s.Value,
                LastModifiedCommitId = s.LastModifiedCommitId
            }).ToList();

            _context.BranchStates.AddRange(newStates);
            await _context.SaveChangesAsync();
        }
    }
}
