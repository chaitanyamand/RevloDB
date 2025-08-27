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

        public async Task<Key> CreateKeyWithVersionAsync(string keyName, string value, int namespaceId)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var now = DateTime.UtcNow;
                var key = new Key
                {
                    KeyName = keyName,
                    CreatedAt = now,
                    IsDeleted = false,
                    NamespaceId = namespaceId,

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

        public async Task<Key?> GetByIdAsync(int id, int namespaceId)
        {
            return await _context.Keys
                .AsNoTracking()
                .Include(k => k.CurrentVersion)
                .Where(k => !k.IsDeleted && k.NamespaceId == namespaceId)
                .FirstOrDefaultAsync(k => k.Id == id);
        }

        public async Task<Key?> GetByNameAsync(string keyName, int namespaceId)
        {
            return await _context.Keys
                .AsNoTracking()
                .Include(k => k.CurrentVersion)
                .Where(k => !k.IsDeleted && namespaceId == k.NamespaceId)
                .FirstOrDefaultAsync(k => k.KeyName == keyName);
        }

        public async Task<bool> DeleteByNameAsync(string keyName, int namespaceId)
        {
            var key = await _context.Keys
                .Where(k => !k.IsDeleted && k.NamespaceId == namespaceId)
                .FirstOrDefaultAsync(k => k.KeyName == keyName);

            if (key == null) return false;

            key.IsDeleted = true;

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> RestoreByNameAsync(string keyName, int namespaceId)
        {
            var key = await _context.Keys
                .Where(k => k.IsDeleted && k.NamespaceId == namespaceId)
                .FirstOrDefaultAsync(k => k.KeyName == keyName);

            if (key == null) return false;

            key.IsDeleted = false;

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<IEnumerable<Key>> GetAllAsync(int namespaceId)
        {
            return await _context.Keys
                .AsNoTracking()
                .Include(k => k.CurrentVersion)
                .Where(k => !k.IsDeleted && k.NamespaceId == namespaceId)
                .ToListAsync();
        }

        public async Task<Key> AddNewVersionAsync(string keyName, string value, int namespaceId)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var key = await _context.Keys
                    .Where(k => !k.IsDeleted && k.NamespaceId == namespaceId)
                    .FirstOrDefaultAsync(k => k.KeyName == keyName);

                if (key == null)
                {
                    throw new KeyNotFoundException($"Key '{keyName}' not found");
                }

                var maxVersionNumber = await _context.Versions
                    .Where(v => v.KeyId == key.Id)
                    .Select(v => (int?)v.VersionNumber)
                    .MaxAsync() ?? 0;

                var nextVersionNumber = maxVersionNumber + 1;

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
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        public async Task<bool> ExistsAsync(string keyName, int namespaceId)
        {
            return await _context.Keys
                .AsNoTracking()
                .Where(k => !k.IsDeleted && k.NamespaceId == namespaceId)
                .AnyAsync(k => k.KeyName == keyName);
        }

        public async Task<Key> RevertToVersionAsync(string keyName, int targetVersionNumber, int namespaceId)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var key = await _context.Keys
                    .Include(k => k.CurrentVersion)
                    .Where(k => !k.IsDeleted && k.NamespaceId == namespaceId)
                    .FirstOrDefaultAsync(k => k.KeyName == keyName);

                if (key == null)
                {
                    throw new KeyNotFoundException($"Key '{keyName}' not found");
                }

                var targetVersion = await _context.Versions
                    .FirstOrDefaultAsync(v => v.KeyId == key.Id && v.VersionNumber == targetVersionNumber);

                if (targetVersion == null)
                {
                    throw new InvalidOperationException($"Version {targetVersionNumber} not found for key '{keyName}'");
                }

                if (key.CurrentVersion?.VersionNumber == targetVersionNumber)
                {
                    return key;
                }

                key.CurrentVersion = targetVersion;
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return key;
            }
            catch (KeyNotFoundException)
            {
                await transaction.RollbackAsync();
                throw;
            }
            catch (InvalidOperationException)
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
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}