using Microsoft.EntityFrameworkCore;
using Npgsql;
using RevloDB.Data;
using RevloDB.Entities;
using RevloDB.Repositories.Interfaces;

namespace RevloDB.Repositories
{
    public class UserNamespaceRepository : IUserNamespaceRepository
    {
        private readonly RevloDbContext _context;

        public UserNamespaceRepository(RevloDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Namespace>> GetUserNamespacesAsync(int userId)
        {
            return await _context.UserNamespaces
                .AsNoTracking()
                .Include(un => un.Namespace)
                .Where(un => un.UserId == userId && !un.Namespace.IsDeleted)
                .Select(un => un.Namespace)
                .ToListAsync();
        }

        public async Task<NamespaceRole?> UserHasAccessToNamespaceAsync(int userId, int namespaceId)
        {
            var userNamespace = await _context.UserNamespaces
                .AsNoTracking()
                .Include(un => un.Namespace)
                .Where(un => un.UserId == userId && un.NamespaceId == namespaceId && !un.Namespace.IsDeleted)
                .FirstOrDefaultAsync();

            return userNamespace?.Role;
        }

        public async Task<UserNamespace?> GetUserNamespaceEntryAsync(int userId, int namespaceId)
        {
            return await _context.UserNamespaces
                .AsNoTracking()
                .Include(un => un.Namespace)
                .FirstOrDefaultAsync(un => un.UserId == userId && un.NamespaceId == namespaceId && !un.Namespace.IsDeleted);
        }

        public async Task<IEnumerable<UserNamespace>> GetUserNamespaceDetailsAsync(int userId)
        {
            return await _context.UserNamespaces
                .AsNoTracking()
                .Include(un => un.User)
                .Include(un => un.Namespace)
                .Where(un => un.UserId == userId && !un.Namespace.IsDeleted)
                .ToListAsync();
        }


        public async Task GrantUserAccessToNamespaceAsync(int userId, int namespaceId, NamespaceRole role = NamespaceRole.ReadOnly)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var userExists = await _context.Users
                    .AsNoTracking()
                    .Where(u => !u.IsDeleted)
                    .AnyAsync(u => u.Id == userId);

                if (!userExists)
                {
                    throw new KeyNotFoundException($"User with ID '{userId}' not found");
                }

                var namespaceExists = await _context.Namespaces
                    .AsNoTracking()
                    .Where(n => !n.IsDeleted)
                    .AnyAsync(n => n.Id == namespaceId);

                if (!namespaceExists)
                {
                    throw new KeyNotFoundException($"Namespace with ID '{namespaceId}' not found");
                }

                var existingAccess = await _context.UserNamespaces
                    .FirstOrDefaultAsync(un => un.UserId == userId && un.NamespaceId == namespaceId);

                if (existingAccess != null)
                {
                    await transaction.CommitAsync();
                    return;
                }

                var userNamespace = new UserNamespace
                {
                    UserId = userId,
                    NamespaceId = namespaceId,
                    GrantedAt = DateTime.UtcNow,
                    Role = role
                };

                _context.UserNamespaces.Add(userNamespace);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
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
                throw new InvalidOperationException("User already has access to this namespace.");
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task RevokeUserAccessFromNamespaceAsync(int userId, int namespaceId)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var userNamespace = await _context.UserNamespaces
                    .FirstOrDefaultAsync(un => un.UserId == userId && un.NamespaceId == namespaceId);

                if (userNamespace == null)
                {
                    throw new UnauthorizedAccessException($"User with ID '{userId}' does not have access to namespace with ID '{namespaceId}'");
                }

                _context.UserNamespaces.Remove(userNamespace);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch (UnauthorizedAccessException)
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