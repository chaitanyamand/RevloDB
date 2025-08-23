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
            try
            {
                return await _context.Versions.FindAsync(id);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve version with id '{id}'", ex);
            }
        }

        public async Task<IEnumerable<Entities.Version>> GetVersionsByKeyIdAsync(int keyId)
        {
            try
            {
                return await _context.Versions
                    .Where(v => v.KeyId == keyId)
                    .OrderByDescending(v => v.VersionNumber)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve versions for key id '{keyId}'", ex);
            }
        }

        public async Task<Entities.Version?> GetLatestVersionByKeyIdAsync(int keyId)
        {
            try
            {
                return await _context.Versions
                    .Where(v => v.KeyId == keyId)
                    .OrderByDescending(v => v.VersionNumber)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve latest version for key id '{keyId}'", ex);
            }
        }
    }
}