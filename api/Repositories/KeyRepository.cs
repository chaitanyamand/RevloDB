using Microsoft.EntityFrameworkCore;
using Npgsql;
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

        public async Task<Key> CreateKeyWithVersionAsync(string keyName, string value)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var now = DateTime.UtcNow;
                var key = new Key
                {
                    KeyName = keyName,
                    CreatedAt = now
                };

                _context.Keys.Add(key);
                await _context.SaveChangesAsync();

                var version = new Entities.Version
                {
                    KeyId = key.Id,
                    Value = value,
                    VersionNumber = 1,
                    Timestamp = now
                };

                _context.Versions.Add(version);
                await _context.SaveChangesAsync();

                key.CurrentVersion = version;
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return key;
            }
            catch (DbUpdateException ex)
                when (ex.InnerException is PostgresException postgresEx && postgresEx.SqlState == "23505")
            {
                await transaction.RollbackAsync();
                throw new InvalidOperationException($"Key '{keyName}' already exists.");
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<Key?> GetByIdAsync(int id)
        {
            try
            {
                return await _context.Keys
                    .Include(k => k.CurrentVersion)
                    .FirstOrDefaultAsync(k => k.Id == id);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve key with id '{id}': {ex.Message}", ex);
            }
        }

        public async Task<Key?> GetByNameAsync(string keyName)
        {
            try
            {
                return await _context.Keys
                    .Include(k => k.CurrentVersion)
                    .FirstOrDefaultAsync(k => k.KeyName == keyName);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve key '{keyName}': {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteByNameAsync(string keyName)
        {
            var key = await _context.Keys
                .Include(k => k.CurrentVersion)
                .FirstOrDefaultAsync(k => k.KeyName == keyName);

            if (key == null) return false;

            key.CurrentVersion = null;
            await _context.SaveChangesAsync();

            _context.Keys.Remove(key);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<IEnumerable<Key>> GetAllAsync()
        {
            try
            {
                return await _context.Keys
                    .Include(k => k.CurrentVersion)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve keys: {ex.Message}", ex);
            }
        }

        public async Task<Key> AddNewVersionAsync(string keyName, string value)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var key = await _context.Keys
                    .Include(k => k.CurrentVersion)
                    .FirstOrDefaultAsync(k => k.KeyName == keyName);

                if (key == null)
                {
                    throw new KeyNotFoundException($"Key '{keyName}' not found");
                }

                var nextVersionNumber = (key.CurrentVersion?.VersionNumber ?? 0) + 1;
                var newVersion = new Entities.Version
                {
                    KeyId = key.Id,
                    Value = value,
                    VersionNumber = nextVersionNumber,
                    Timestamp = DateTime.UtcNow
                };

                _context.Versions.Add(newVersion);
                await _context.SaveChangesAsync();

                key.CurrentVersion = newVersion;
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return key;
            }
            catch (KeyNotFoundException)
            {
                await transaction.RollbackAsync();
                throw;
            }
            catch (DbUpdateException ex)
                when (ex.InnerException is PostgresException postgresEx && postgresEx.SqlState == "23505")
            {
                await transaction.RollbackAsync();
                throw new InvalidOperationException("Concurrency conflict: The key was updated by another operation. Please try again.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new InvalidOperationException($"Failed to add new version to key '{keyName}': {ex.Message}", ex);
            }
        }

        public async Task<bool> ExistsAsync(string keyName)
        {
            try
            {
                return await _context.Keys.AnyAsync(k => k.KeyName == keyName);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to check if key '{keyName}' exists: {ex.Message}", ex);
            }
        }
    }
}