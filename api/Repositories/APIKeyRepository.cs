using Microsoft.EntityFrameworkCore;
using Npgsql;
using RevloDB.Data;
using RevloDB.Entities;
using RevloDB.Repositories.Interfaces;

namespace RevloDB.Repositories
{
    public class APIKeyRepository : IAPIKeyRepository
    {
        private readonly RevloDbContext _context;

        public APIKeyRepository(RevloDbContext context)
        {
            _context = context;
        }

        public async Task<ApiKey> CreateAsync(ApiKey apiKey)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var userExists = await _context.Users
                    .AsNoTracking()
                    .Where(u => !u.IsDeleted)
                    .AnyAsync(u => u.Id == apiKey.UserId);

                if (!userExists)
                {
                    throw new KeyNotFoundException($"User with ID '{apiKey.UserId}' not found");
                }

                var namespaceExists = await _context.Namespaces
                    .AsNoTracking()
                    .Where(n => !n.IsDeleted)
                    .AnyAsync(n => n.Id == apiKey.NamespaceId);

                if (!namespaceExists)
                {
                    throw new KeyNotFoundException($"Namespace with ID '{apiKey.NamespaceId}' not found");
                }

                apiKey.CreatedAt = DateTime.UtcNow;
                apiKey.IsDeleted = false;

                _context.ApiKeys.Add(apiKey);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return apiKey;
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
                throw new InvalidOperationException($"API key '{apiKey.KeyValue}' already exists.");
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<ApiKey?> GetByIdAsync(int id)
        {
            return await _context.ApiKeys
                .AsNoTracking()
                .Include(a => a.User)
                .Include(a => a.Namespace)
                .Where(a => !a.IsDeleted && (a.ExpiresAt == null || a.ExpiresAt > DateTime.UtcNow))
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<IEnumerable<ApiKey>> GetByUserIdAsync(int userId)
        {
            return await _context.ApiKeys
                .AsNoTracking()
                .Include(a => a.Namespace)
                .Where(a => !a.IsDeleted && a.UserId == userId && (a.ExpiresAt == null || a.ExpiresAt > DateTime.UtcNow))
                .ToListAsync();
        }

        public async Task DeleteAsync(int id)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var apiKey = await _context.ApiKeys
                    .Where(a => !a.IsDeleted)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (apiKey == null)
                {
                    throw new KeyNotFoundException($"API Key with ID '{id}' not found");
                }

                apiKey.IsDeleted = true;
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch (KeyNotFoundException)
            {
                await transaction.RollbackAsync();
                throw;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}