using Microsoft.EntityFrameworkCore;
using Npgsql;
using RevloDB.Data;
using RevloDB.Entities;
using RevloDB.Repositories.Interfaces;

namespace RevloDB.Repositories
{
    public class NamespaceRepository : INamespaceRepository
    {
        private readonly RevloDbContext _context;

        public NamespaceRepository(RevloDbContext context)
        {
            _context = context;
        }

        public async Task<Namespace?> GetByIdAsync(int id)
        {
            return await _context.Namespaces
                .AsNoTracking()
                .Where(n => !n.IsDeleted)
                .FirstOrDefaultAsync(n => n.Id == id);
        }

        public async Task<Namespace?> GetByNameAsync(string namespaceName, int userId)
        {
            return await _context.Namespaces
                .AsNoTracking()
                .Where(n => !n.IsDeleted)
                .Where(n => n.Name == namespaceName)
                .Where(n => n.UserNamespaces.Any(un => un.UserId == userId))
                .FirstOrDefaultAsync();
        }

        public async Task<Namespace> CreateAsync(string namespaceName, string? namespaceDescription, int createdByUserId)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            namespaceDescription ??= string.Empty;
            try
            {
                var ns = new Namespace
                {
                    Name = namespaceName,
                    Description = namespaceDescription,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false,
                    CreatedByUserId = createdByUserId
                };

                _context.Namespaces.Add(ns);

                await _context.SaveChangesAsync();

                _context.UserNamespaces.Add(new UserNamespace
                {
                    UserId = createdByUserId,
                    NamespaceId = ns.Id,
                    GrantedAt = DateTime.UtcNow,
                    Role = NamespaceRole.Admin
                });
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return ns;
            }
            catch (DbUpdateException ex)
                when (ex.InnerException is PostgresException postgresEx && postgresEx.SqlState == "23505")
            {
                await transaction.RollbackAsync();
                throw new InvalidOperationException($"Namespace '{namespaceName}' already exists for this user.");
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<Namespace> UpdateNamespaceAsync(string newName, string? newDescription, int id)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var ns = await _context.Namespaces
                    .Where(n => !n.IsDeleted)
                    .FirstOrDefaultAsync(n => n.Id == id);

                if (ns == null)
                {
                    throw new KeyNotFoundException($"Namespace with ID '{id}' not found");
                }

                ns.Name = newName;
                ns.Description = newDescription == null ? ns.Description : newDescription;
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return ns;
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
                throw new InvalidOperationException($"Namespace '{newName}' already exists for this user.");
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task DeleteAsync(int id)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var ns = await _context.Namespaces
                    .Where(n => !n.IsDeleted)
                    .FirstOrDefaultAsync(n => n.Id == id);

                if (ns == null)
                {
                    throw new KeyNotFoundException($"Namespace with ID '{id}' not found");
                }

                ns.IsDeleted = true;

                var apiKeys = await _context.ApiKeys
                    .Where(a => a.NamespaceId == id && !a.IsDeleted)
                    .ToListAsync();
                foreach (var apiKey in apiKeys)
                {
                    apiKey.IsDeleted = true;
                }

                var keys = await _context.Keys
                    .Where(k => k.NamespaceId == id && !k.IsDeleted)
                    .ToListAsync();
                foreach (var key in keys)
                {
                    key.IsDeleted = true;
                }

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