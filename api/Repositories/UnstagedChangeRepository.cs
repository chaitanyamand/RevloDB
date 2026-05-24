using Microsoft.EntityFrameworkCore;
using RevloDB.Data;
using RevloDB.Entities;
using RevloDB.Repositories.Interfaces;

namespace RevloDB.Repositories
{
    public class UnstagedChangeRepository : IUnstagedChangeRepository
    {
        private readonly RevloDbContext _context;

        public UnstagedChangeRepository(RevloDbContext context)
        {
            _context = context;
        }

        public async Task<List<UnstagedChange>> GetAllForBranchAsync(int branchId)
        {
            return await _context.UnstagedChanges
                .AsNoTracking()
                .Where(u => u.BranchId == branchId)
                .OrderBy(u => u.KeyName)
                .ToListAsync();
        }

        public async Task<UnstagedChange?> GetByKeyAsync(int branchId, string keyName)
        {
            return await _context.UnstagedChanges
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.BranchId == branchId && u.KeyName == keyName);
        }

        public async Task UpsertAsync(UnstagedChange change)
        {
            var existing = await _context.UnstagedChanges
                .FirstOrDefaultAsync(u => u.BranchId == change.BranchId && u.KeyName == change.KeyName);

            if (existing != null)
            {
                existing.Value = change.Value;
                existing.Action = change.Action;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                change.UpdatedAt = DateTime.UtcNow;
                _context.UnstagedChanges.Add(change);
            }

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAllForBranchAsync(int branchId)
        {
            var changes = await _context.UnstagedChanges
                .Where(u => u.BranchId == branchId)
                .ToListAsync();

            _context.UnstagedChanges.RemoveRange(changes);
            await _context.SaveChangesAsync();
        }
    }
}
