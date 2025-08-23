using Microsoft.EntityFrameworkCore;
using RevloDB.Data;
using RevloDB.Repositories.Interfaces;

namespace RevloDB.Repositories
{
    public class VersionRepository : IVersionRepository
    {
        private readonly RevloDbContext _context;

        public VersionRepository(RevloDbContext context)
        {
            _context = context;
        }

        public async Task<Entities.Version?> GetByIdAsync(int id)
        {
            return await _context.Versions
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.Id == id);
        }

        public async Task<IEnumerable<Entities.Version>> GetVersionsByKeyIdAsync(int keyId)
        {
            return await _context.Versions
                .AsNoTracking()
                .Where(v => v.KeyId == keyId)
                .OrderByDescending(v => v.VersionNumber)
                .ToListAsync();
        }

        public async Task<Entities.Version?> GetLatestVersionByKeyIdAsync(int keyId)
        {
            return await _context.Versions
                .AsNoTracking()
                .Where(v => v.KeyId == keyId)
                .OrderByDescending(v => v.VersionNumber)
                .FirstOrDefaultAsync();
        }
    }
}