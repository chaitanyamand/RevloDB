using Microsoft.EntityFrameworkCore;
using RevloDB.Data;
using RevloDB.Entities;
using RevloDB.Repositories.Interfaces;

namespace RevloDB.Repositories
{
    public class BranchRepository : IBranchRepository
    {
        private readonly RevloDbContext _context;

        public BranchRepository(RevloDbContext context)
        {
            _context = context;
        }

        public async Task<Branch?> GetByNameAsync(string name, int namespaceId)
        {
            return await _context.Branches
                .AsNoTracking()
                .Include(b => b.HeadCommit)
                .FirstOrDefaultAsync(b => b.Name == name && b.NamespaceId == namespaceId && !b.IsDeleted);
        }

        public async Task<List<Branch>> GetAllAsync(int namespaceId)
        {
            return await _context.Branches
                .AsNoTracking()
                .Include(b => b.HeadCommit)
                .Where(b => b.NamespaceId == namespaceId && !b.IsDeleted)
                .OrderBy(b => b.Name)
                .ToListAsync();
        }

        public async Task<Branch> CreateAsync(Branch branch)
        {
            _context.Branches.Add(branch);
            await _context.SaveChangesAsync();
            return branch;
        }

        public async Task DeleteAsync(int id)
        {
            var branch = await _context.Branches.FindAsync(id);
            if (branch == null)
                throw new KeyNotFoundException($"Branch with ID '{id}' not found.");

            branch.IsDeleted = true;
            await _context.SaveChangesAsync();
        }

        public async Task UpdateHeadAsync(int id, int? commitId)
        {
            var branch = await _context.Branches.FindAsync(id);
            if (branch == null)
                throw new KeyNotFoundException($"Branch with ID '{id}' not found.");

            branch.HeadCommitId = commitId;
            await _context.SaveChangesAsync();
        }
    }
}
