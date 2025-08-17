using Microsoft.EntityFrameworkCore;
using RevloDB.Data;
using RevloDB.Entities;
using RevloDB.Repositories.Interfaces;

namespace RevloDB.Repositories
{
    public class KeyRepository : IKeyRepository
    {
        private readonly RevloDbContext _context;

        public KeyRepository(RevloDbContext context)
        {
            _context = context;
        }

        public async Task<Key?> GetByIdAsync(int id)
        {
            return await _context.Keys
                .Include(k => k.CurrentVersion)
                .FirstOrDefaultAsync(k => k.Id == id);
        }

        public async Task<Key?> GetByNameAsync(string keyName)
        {
            return await _context.Keys
                .Include(k => k.CurrentVersion)
                .FirstOrDefaultAsync(k => k.KeyName == keyName);
        }

        public async Task<IEnumerable<Key>> GetAllAsync()
        {
            return await _context.Keys
                .Include(k => k.CurrentVersion)
                .ToListAsync();
        }

        public async Task<Key> CreateAsync(Key key)
        {
            key.CreatedAt = DateTime.UtcNow;
            _context.Keys.Add(key);
            await _context.SaveChangesAsync();
            return key;
        }

        public async Task<Key> UpdateAsync(Key key)
        {
            _context.Keys.Update(key);
            await _context.SaveChangesAsync();
            return key;
        }

        public async Task DeleteAsync(int id)
        {
            var key = await _context.Keys.FindAsync(id);
            if (key != null)
            {
                _context.Keys.Remove(key);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(string keyName)
        {
            return await _context.Keys.AnyAsync(k => k.KeyName == keyName);
        }
    }
}