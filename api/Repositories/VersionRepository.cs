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
            return await _context.Versions.FindAsync(id);
        }

        public async Task<IEnumerable<Entities.Version>> GetVersionsByKeyIdAsync(int keyId)
        {
            return await _context.Versions
                .Where(v => v.KeyId == keyId)
                .OrderByDescending(v => v.VersionNumber)
                .ToListAsync();
        }

        public async Task<Entities.Version?> GetLatestVersionByKeyIdAsync(int keyId)
        {
            return await _context.Versions
                .Where(v => v.KeyId == keyId)
                .OrderByDescending(v => v.VersionNumber)
                .FirstOrDefaultAsync();
        }

        public async Task<Entities.Version> CreateAsync(Entities.Version version)
        {
            version.Timestamp = DateTime.UtcNow;
            _context.Versions.Add(version);
            await _context.SaveChangesAsync();
            return version;
        }

        public async Task<int> GetNextVersionNumberAsync(int keyId)
        {
            var maxVersion = await _context.Versions
                .Where(v => v.KeyId == keyId)
                .MaxAsync(v => (int?)v.VersionNumber);

            return (maxVersion ?? 0) + 1;
        }
    }
}