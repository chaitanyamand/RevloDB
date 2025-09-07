using Microsoft.EntityFrameworkCore;
using Npgsql;
using RevloDB.Data;
using RevloDB.Entities;
using RevloDB.Repositories.Interfaces;

namespace RevloDB.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly RevloDbContext _context;

        public UserRepository(RevloDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            return await _context.Users
                .AsNoTracking()
                .Where(u => !u.IsDeleted)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            return await _context.Users
                .AsNoTracking()
                .Where(u => !u.IsDeleted)
                .FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<User> CreateAsync(string username, string passwordHash, string passwordSalt)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var user = new User
                {
                    Username = username,
                    PasswordHash = passwordHash,
                    PasswordSalt = passwordSalt,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return user;
            }
            catch (DbUpdateException ex)
                when (ex.InnerException is PostgresException postgresEx && postgresEx.SqlState == "23505")
            {
                await transaction.RollbackAsync();
                throw new InvalidOperationException($"User with username '{username}' already exists.");
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task UpdatePasswordAsync(string passwordHash, string passwordSalt, int id)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var user = await _context.Users
                    .Where(u => !u.IsDeleted)
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                {
                    throw new KeyNotFoundException($"User with ID '{id}' not found");
                }

                user.PasswordHash = passwordHash;
                user.PasswordSalt = passwordSalt;

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


        public async Task DeleteAsync(int id)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var user = await _context.Users
                    .Where(u => !u.IsDeleted)
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                {
                    throw new KeyNotFoundException($"User with ID '{id}' not found");
                }

                user.IsDeleted = true;

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